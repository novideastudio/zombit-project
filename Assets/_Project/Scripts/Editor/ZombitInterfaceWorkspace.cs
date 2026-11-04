#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor visual do ZomBit. Permite abrir prefabs de UI fora de Play Mode,
/// visualizar a hierarquia num canvas, mover/redimensionar RectTransforms,
/// editar textos/imagens/cores, aplicar tema global e criar componentes reutilizaveis.
/// O editor trabalha diretamente em Prefab Contents e nao altera a logica de gameplay.
/// </summary>
public sealed partial class ZombitInterfaceWorkspace : EditorWindow
{
    const string ScreensPath = "Assets/_Project/Resources/UI/Screens";
    const string ComponentsPath = "Assets/_Project/Resources/UI/Components";
    const string OverlaysPath = "Assets/_Project/Resources/UI/Overlays";
    const string ThemePath = "Assets/_Project/Resources/Config/UITheme.asset";
    const string GuidePath = "Assets/_Project/Documentacao/EDITOR_VISUAL_GUIA_PT_BR.md";

    enum WorkspaceTab { Canvas, Components, Theme }
    WorkspaceTab tab;

    readonly List<string> prefabPaths = new List<string>();
    readonly List<RectTransform> nodes = new List<RectTransform>();
    readonly Dictionary<RectTransform, Rect> nodeRects = new Dictionary<RectTransform, Rect>();
    readonly List<string> componentPaths = new List<string>();

    string selectedPath;
    GameObject prefabRoot;
    RectTransform rootRect;
    RectTransform selectedRect;
    Vector2 treeScroll;
    Vector2 componentScroll;
    Vector2 propertyScroll;
    Vector2 canvasScroll;
    Vector2 dragStartMouse;
    Vector2 dragStartPosition;
    Vector2 dragStartSize;
    RectTransform dragRect;
    bool dirty;
    float canvasScale = 1f;
    float currentFit = 1f;
    Rect previewArtboard;
    string status = "Abra uma tela para comecar a editar.";
    int selectedComponentIndex;
    string newComponentName = "Componente";

    [MenuItem("ZomBit/Interface/Editor Visual (Canvas)", priority = 0)]
    static void Open() => GetWindow<ZombitInterfaceWorkspace>("ZomBit - Editor Visual");

    void OnEnable()
    {
        minSize = new Vector2(1250, 720);
        Undo.undoRedoPerformed += OnWorkspaceUndoRedo;
        RefreshPrefabLists();
        if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath)) LoadPrefab(selectedPath);
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= OnWorkspaceUndoRedo;
        if (dirty && prefabRoot != null) SavePrefab();
        UnloadPrefab();
        DestroyUnityPreview();
    }

    void OnGUI()
    {
        HandleUndoRedoCommands();
        DrawToolbar();
        if (tab == WorkspaceTab.Theme) { DrawThemeTab(); return; }
        if (tab == WorkspaceTab.Components) { DrawComponentsTab(); return; }

        if (prefabRoot == null)
        {
            DrawEmptyState();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        DrawHierarchyPanel();
        DrawCanvasPanel();
        DrawInspectorPanel();
        EditorGUILayout.EndHorizontal();
        DrawStatusBar();
        HandleCanvasInput();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("ZOMBIT SURVIVAL", EditorStyles.boldLabel, GUILayout.Width(130));
        GUILayout.Label("Tela:", GUILayout.Width(34));
        int current = Mathf.Max(0, prefabPaths.IndexOf(selectedPath));
        string[] names = prefabPaths.Select(p => Path.GetFileNameWithoutExtension(p)).ToArray();
        int next = names.Length == 0 ? -1 : EditorGUILayout.Popup(current, names, EditorStyles.toolbarPopup, GUILayout.Width(190));
        if (next >= 0 && next < prefabPaths.Count && prefabPaths[next] != selectedPath)
        {
            if (ConfirmDiscardIfNeeded()) LoadPrefab(prefabPaths[next]);
        }
        GUILayout.Space(6);
        if (GUILayout.Button("Abrir", EditorStyles.toolbarButton, GUILayout.Width(48))) RefreshPrefabLists();
        if (GUILayout.Button("Salvar", EditorStyles.toolbarButton, GUILayout.Width(50))) SavePrefab();
        using (new EditorGUI.DisabledScope(prefabRoot == null))
        {
            if (GUILayout.Button("Duplicar tela", EditorStyles.toolbarButton, GUILayout.Width(76))) DuplicateCurrentScreen();
        }
        if (GUILayout.Button("↶", EditorStyles.toolbarButton, GUILayout.Width(25))) PerformWorkspaceUndo();
        if (GUILayout.Button("↷", EditorStyles.toolbarButton, GUILayout.Width(25))) PerformWorkspaceRedo();
        if (GUILayout.Button("Prefab Mode", EditorStyles.toolbarButton, GUILayout.Width(82))) OpenInPrefabMode();
        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || dirty))
        {
            if (GUILayout.Button("Capturar tela", EditorStyles.toolbarButton, GUILayout.Width(84)))
            {
                UIBaker.CaptureOpenInterfacesSafe();
                RefreshPrefabLists();
            }
        }
        GUILayout.FlexibleSpace();
        DrawTabButton("Canvas", WorkspaceTab.Canvas);
        DrawTabButton("Componentes", WorkspaceTab.Components);
        DrawTabButton("Tema global", WorkspaceTab.Theme);
        GUILayout.Space(6);
        if (GUILayout.Button("QA", EditorStyles.toolbarButton, GUILayout.Width(34))) ZombitUIProjectAudit.Open();
        if (GUILayout.Button("Guia", EditorStyles.toolbarButton, GUILayout.Width(42))) OpenGuide();
        EditorGUILayout.EndHorizontal();
    }

    void DrawTabButton(string label, WorkspaceTab value)
    {
        bool active = tab == value;
        var old = GUI.backgroundColor;
        if (active) GUI.backgroundColor = new Color(0.95f, 0.62f, 0.18f);
        if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(value == WorkspaceTab.Theme ? 88 : 82))) tab = value;
        GUI.backgroundColor = old;
    }

    void DrawEmptyState()
    {
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(560));
        GUILayout.Label("EDITOR VISUAL", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox("Escolha uma tela na barra superior. O editor abre o prefab fora do Play Mode, permitindo montar a interface sem iniciar uma campanha.", MessageType.Info);
        if (GUILayout.Button("Atualizar lista de telas", GUILayout.Height(32))) RefreshPrefabLists();
        EditorGUILayout.LabelField("Telas: " + prefabPaths.Count);
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
    }

    void DrawHierarchyPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(215));
        GUILayout.Label("HIERARQUIA", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Selecione um objeto para editar. Os nomes usados pelo codigo devem ser preservados.", MessageType.None);
        treeScroll = EditorGUILayout.BeginScrollView(treeScroll, EditorStyles.helpBox);
        foreach (var node in nodes)
        {
            if (node == null || node == rootRect) continue;
            int depth = GetDepth(node) - 1;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(Mathf.Clamp(depth, 0, 8) * 12);
            string marker = node.GetComponent<Button>() != null ? "BOTAO" : node.GetComponent<Image>() != null ? "IMG" : node.GetComponent<TMP_Text>() != null ? "TXT" : "UI";
            bool isSelected = selectedRect == node;
            bool isLocked = IsEditorLocked(node);
            var old = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = new Color(0.95f, 0.68f, 0.24f, 0.75f);
            if (GUILayout.Toggle(isSelected, (isLocked ? "🔒 " : "") + node.name + "  ·  " + marker, EditorStyles.miniButton)) SelectNode(node);
            GUI.backgroundColor = old;
            using (new EditorGUI.DisabledScope(node == rootRect))
            {
                if (GUILayout.Button("↑", EditorStyles.miniButton, GUILayout.Width(20))) MoveLayer(node, 1);
                if (GUILayout.Button("↓", EditorStyles.miniButton, GUILayout.Width(20))) MoveLayer(node, -1);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        GUILayout.Label("ADICIONAR ELEMENTO", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Painel", GUILayout.Height(25))) CreateElement(ElementKind.Panel);
        if (GUILayout.Button("+ Botao pronto", GUILayout.Height(25))) CreateElement(ElementKind.Button);
        if (GUILayout.Button("+ Texto", GUILayout.Height(25))) CreateElement(ElementKind.Text);
        if (GUILayout.Button("+ Imagem / Icone", GUILayout.Height(25))) CreateElement(ElementKind.Image);
        EditorGUILayout.Space(5);
        GUILayout.Label("MODELOS PRONTOS", EditorStyles.boldLabel);
        if (GUILayout.Button("+ Cartão", GUILayout.Height(23))) CreatePreset(PresetKind.Card);
        if (GUILayout.Button("+ Barra de vida", GUILayout.Height(23))) CreatePreset(PresetKind.HealthBar);
        if (GUILayout.Button("+ Inventário (4×3)", GUILayout.Height(23))) CreatePreset(PresetKind.Inventory);
        if (GUILayout.Button("+ HUD de combate", GUILayout.Height(23))) CreatePreset(PresetKind.CombatHud);
        EditorGUILayout.EndVertical();
    }

    void DrawCanvasPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("CANVAS DE PRE-VISUALIZACAO", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.Label("Zoom", GUILayout.Width(35));
        canvasScale = EditorGUILayout.Slider(canvasScale, 0.35f, 1.5f, GUILayout.Width(105));
        if (GUILayout.Button("Ajustar", GUILayout.Width(48))) canvasScale = 1f;
        useUnityCanvasPreview = GUILayout.Toggle(useUnityCanvasPreview, "Prévia real", EditorStyles.toolbarButton, GUILayout.Width(78));
        snapEnabled = GUILayout.Toggle(snapEnabled, "Encaixe", EditorStyles.toolbarButton, GUILayout.Width(58));
        showGuides = GUILayout.Toggle(showGuides, "Guias", EditorStyles.toolbarButton, GUILayout.Width(48));
        if (GUILayout.Button("Grade ▾", EditorStyles.toolbarButton, GUILayout.Width(58))) showGridOptions = !showGridOptions;
        EditorGUILayout.EndHorizontal();
        if (showGridOptions)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Passo da grade", GUILayout.Width(92));
            gridSize = EditorGUILayout.IntSlider(gridSize, 2, 64, GUILayout.Width(220));
            showGridLines = GUILayout.Toggle(showGridLines, "Mostrar grade", GUILayout.Width(105));
            smartSnap = GUILayout.Toggle(smartSnap, "Alinhar com outros elementos", GUILayout.Width(210));
            GUILayout.Label("Camadas: use os botões no Inspector", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        Rect area = GUILayoutUtility.GetRect(320, 240, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(area, new Color(0.10f, 0.11f, 0.13f));
        DrawCheckerGrid(area);
        if (rootRect != null)
        {
            Vector2 workspaceResolution = GetWorkspaceResolution();
            float rw = Mathf.Max(1, workspaceResolution.x);
            float rh = Mathf.Max(1, workspaceResolution.y);
            float fit = Mathf.Min((area.width - 32) / rw, (area.height - 32) / rh) * canvasScale;
            fit = Mathf.Max(0.01f, fit);
            Rect artboard = new Rect(area.center.x - rw * fit * 0.5f, area.center.y - rh * fit * 0.5f, rw * fit, rh * fit);
            previewArtboard = artboard;
            currentFit = fit;
            EditorGUI.DrawRect(artboard, new Color(0.035f, 0.045f, 0.055f));
            GUI.BeginGroup(artboard);
            if (useUnityCanvasPreview)
            {
                DrawUnityCanvasPreview(artboard.size, fit);
                DrawSnapGrid(artboard.size, fit);
                PopulateNodeRects(artboard.size, fit);
                DrawSelectionHandles();
                if (showGuides) DrawAlignmentGuides(artboard.size);
            }
            else
            {
                DrawNodePreview(rootRect, new Rect(0, 0, artboard.width, artboard.height));
                DrawPrefabPreview(artboard.size, fit);
                DrawSnapGrid(artboard.size, fit);
                if (showGuides) DrawAlignmentGuides(artboard.size);
            }
            GUI.EndGroup();
            GUI.Box(artboard, GUIContent.none);
            EditorGUI.LabelField(new Rect(area.x + 8, area.y + 5, 300, 18),
                Mathf.RoundToInt(rw) + " × " + Mathf.RoundToInt(rh) + " px", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawPrefabPreview(Vector2 viewSize, float fit)
    {
        nodeRects.Clear();
        foreach (var node in nodes)
        {
            if (node == null || node == rootRect || !node.gameObject.activeSelf) continue;
            Rect relative = CalculateRelativeRect(node);
            Rect gui = new Rect(viewSize.x * 0.5f + (relative.center.x - rootRect.rect.center.x) * fit - relative.width * fit * 0.5f,
                                viewSize.y * 0.5f - (relative.center.y - rootRect.rect.center.y) * fit - relative.height * fit * 0.5f,
                                relative.width * fit, relative.height * fit);
            if (gui.width < 1 || gui.height < 1) continue;
            nodeRects[node] = gui;
            DrawNodePreview(node, gui);
        }
        DrawSelectionHandles();
    }

    Rect CalculateRelativeRect(RectTransform node)
    {
        try
        {
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(rootRect, node);
            return new Rect(b.center.x - b.size.x * 0.5f, b.center.y - b.size.y * 0.5f, b.size.x, b.size.y);
        }
        catch
        {
            Vector2 p = node.anchoredPosition;
            return new Rect(p.x - node.rect.width * 0.5f, p.y - node.rect.height * 0.5f, node.rect.width, node.rect.height);
        }
    }

    void DrawNodePreview(RectTransform node, Rect rect)
    {
        Image image = node.GetComponent<Image>();
        if (image != null)
        {
            Color old = GUI.color;
            GUI.color = image.color;
            Texture2D texture = image.sprite != null ? AssetPreview.GetAssetPreview(image.sprite) : null;
            if (texture == null && image.sprite != null)
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GetAssetPath(image.sprite));
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            else if (image.color.a > 0.01f) EditorGUI.DrawRect(rect, image.color);
            GUI.color = old;
        }
        TMP_Text text = node.GetComponent<TMP_Text>();
        if (text != null && !string.IsNullOrEmpty(text.text))
        {
            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = ToTextAnchor(text.alignment),
                fontSize = Mathf.Clamp(Mathf.RoundToInt(text.fontSize * Mathf.Max(0.2f, rect.height / Mathf.Max(1, node.rect.height))), 7, 42),
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = text.color;
            if ((text.fontStyle & FontStyles.Bold) != 0) style.fontStyle = FontStyle.Bold;
            GUI.Label(rect, text.text, style);
        }
        if (node.GetComponent<Button>() != null && image == null)
            GUI.Box(rect, GUIContent.none);
    }

    static TextAnchor ToTextAnchor(TextAlignmentOptions a)
    {
        if (a == TextAlignmentOptions.TopLeft) return TextAnchor.UpperLeft;
        if (a == TextAlignmentOptions.Top) return TextAnchor.UpperCenter;
        if (a == TextAlignmentOptions.TopRight) return TextAnchor.UpperRight;
        if (a == TextAlignmentOptions.Left || a == TextAlignmentOptions.MidlineLeft) return TextAnchor.MiddleLeft;
        if (a == TextAlignmentOptions.Right || a == TextAlignmentOptions.MidlineRight) return TextAnchor.MiddleRight;
        if (a == TextAlignmentOptions.BottomLeft) return TextAnchor.LowerLeft;
        if (a == TextAlignmentOptions.BottomRight) return TextAnchor.LowerRight;
        if (a == TextAlignmentOptions.Bottom) return TextAnchor.LowerCenter;
        return TextAnchor.MiddleCenter;
    }

    void DrawCheckerGrid(Rect area)
    {
        Color c = new Color(1, 1, 1, 0.035f);
        for (float x = area.x; x < area.xMax; x += 24) EditorGUI.DrawRect(new Rect(x, area.y, 1, area.height), c);
        for (float y = area.y; y < area.yMax; y += 24) EditorGUI.DrawRect(new Rect(area.x, y, area.width, 1), c);
    }

    void HandleCanvasInput()
    {
        Event e = Event.current;
        if (e == null || rootRect == null || tab != WorkspaceTab.Canvas) return;
        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            if (!previewArtboard.Contains(e.mousePosition)) return;
            Sprite draggedSprite = DragAndDrop.objectReferences.OfType<Sprite>().FirstOrDefault();
            GameObject draggedComponent = DragAndDrop.objectReferences.OfType<GameObject>()
                .FirstOrDefault(go => AssetDatabase.GetAssetPath(go).StartsWith(ComponentsPath, StringComparison.OrdinalIgnoreCase));
            if (draggedSprite == null && draggedComponent == null) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Vector2 drop = e.mousePosition - previewArtboard.position;
                Vector2 rootLocal = new Vector2(
                    (drop.x - previewArtboard.width * 0.5f) / Mathf.Max(0.01f, currentFit) + rootRect.rect.center.x,
                    (previewArtboard.height * 0.5f - drop.y) / Mathf.Max(0.01f, currentFit) + rootRect.rect.center.y);
                if (draggedSprite != null) CreateImageAt(draggedSprite, rootLocal);
                else CreatePrefabAt(draggedComponent, rootLocal);
                MarkPreviewHierarchyDirty();
            }
            e.Use();
            return;
        }
        if (dragRect == null && !previewArtboard.Contains(e.mousePosition)) return;
        Vector2 canvasMouse = e.mousePosition - previewArtboard.position;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (selectedRect != null && nodeRects.TryGetValue(selectedRect, out var selectedBounds))
            {
                ResizeHandle selectedHandle = GetResizeHandle(selectedBounds, canvasMouse);
                if (selectedHandle != ResizeHandle.None)
                {
                    if (IsEditorLocked(selectedRect))
                    {
                        status = "Elemento bloqueado. Desbloqueie pelo Inspector para editar.";
                        e.Use(); Repaint(); return;
                    }
                    BeginCanvasDrag(selectedRect, canvasMouse, selectedHandle);
                    e.Use(); Repaint(); return;
                }
            }
            foreach (var pair in nodeRects.OrderByDescending(p => GetDepth(p.Key)))
            {
                if (!pair.Value.Contains(canvasMouse)) continue;
                SelectNode(pair.Key);
                if (IsEditorLocked(pair.Key))
                {
                    status = "Elemento bloqueado. Desbloqueie pelo Inspector para editar.";
                    e.Use();
                    Repaint();
                    return;
                }
                BeginCanvasDrag(pair.Key, canvasMouse, ResizeHandle.None);
                e.Use();
                Repaint();
                break;
            }
        }
        else if (e.type == EventType.MouseDrag && e.button == 0 && dragRect != null)
        {
            Vector2 screenDelta = canvasMouse - dragStartMouse;
            float fit = CurrentCanvasFit();
            if (fit <= 0) fit = 1;
            Vector3 rootLocalDelta = new Vector3(screenDelta.x / fit, -screenDelta.y / fit, 0);
            Transform parent = dragRect.parent;
            Vector3 parentDelta3 = parent != null ? parent.InverseTransformVector(rootRect.TransformVector(rootLocalDelta)) : rootLocalDelta;
            Vector2 parentDelta = new Vector2(parentDelta3.x, parentDelta3.y);
            Undo.RecordObject(dragRect, activeHandle == ResizeHandle.None ? "Mover elemento UI" : "Redimensionar elemento UI");
            if (activeHandle == ResizeHandle.None)
            {
                Vector2 target = dragStartPosition + parentDelta;
                SnapPosition(dragRect, ref target);
                dragRect.anchoredPosition = target;
                status = "Movendo " + dragRect.name;
            }
            else
            {
                ResizeFromHandle(dragRect, dragStartPosition, dragStartSize, parentDelta, activeHandle);
                SnapSize(dragRect);
                status = "Redimensionando " + dragRect.name;
            }
            dirty = true;
            MarkPreviewDirty();
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (dragUndoGroup >= 0) Undo.CollapseUndoOperations(dragUndoGroup);
            dragUndoGroup = -1;
            dragRect = null;
            activeHandle = ResizeHandle.None;
        }
    }

    float CurrentCanvasFit() => Mathf.Max(0.01f, currentFit);

    void DrawInspectorPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(285));
        GUILayout.Label("PROPRIEDADES", EditorStyles.boldLabel);
        if (selectedRect == null)
        {
            EditorGUILayout.HelpBox("Selecione um elemento no canvas ou na hierarquia.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }
        propertyScroll = EditorGUILayout.BeginScrollView(propertyScroll, EditorStyles.helpBox);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.LabelField(selectedRect.name, EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        bool locked = IsEditorLocked(selectedRect);
        if (GUILayout.Button(locked ? "Desbloquear" : "Bloquear", GUILayout.Height(24))) SetEditorLocked(selectedRect, !locked);
        if (GUILayout.Button("Ocultar / Mostrar", GUILayout.Height(24))) ToggleSelectedVisibility();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("↑ Frente")) MoveLayer(selectedRect, 1);
        if (GUILayout.Button("↓ Trás")) MoveLayer(selectedRect, -1);
        if (GUILayout.Button("Topo")) MoveLayer(selectedRect, 2);
        if (GUILayout.Button("Fundo")) MoveLayer(selectedRect, -2);
        EditorGUILayout.EndHorizontal();
        var rect = selectedRect;
        EditorGUI.BeginDisabledGroup(locked);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Transformação", EditorStyles.miniBoldLabel);
        Vector2 pos = EditorGUILayout.Vector2Field("Posição", rect.anchoredPosition);
        Vector2 size = EditorGUILayout.Vector2Field("Tamanho", rect.sizeDelta);
        Vector2 amin = EditorGUILayout.Vector2Field("Âncora mínima", rect.anchorMin);
        Vector2 amax = EditorGUILayout.Vector2Field("Âncora máxima", rect.anchorMax);
        Vector2 pivot = EditorGUILayout.Vector2Field("Pivô", rect.pivot);
        Vector3 scale = EditorGUILayout.Vector3Field("Escala", rect.localScale);
        float rotation = EditorGUILayout.FloatField("Rotação Z", rect.localEulerAngles.z);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(rect, "Editar propriedades UI");
            rect.anchoredPosition = pos; rect.sizeDelta = size; rect.anchorMin = amin; rect.anchorMax = amax;
            rect.pivot = pivot; rect.localScale = scale; rect.localEulerAngles = new Vector3(0, 0, rotation);
            dirty = true; MarkPreviewDirty();
        }

        var image = selectedRect.GetComponent<Image>();
        if (image != null)
        {
            EditorGUILayout.Space(8); EditorGUILayout.LabelField("Imagem", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", image.sprite, typeof(Sprite), false);
            var tint = EditorGUILayout.ColorField("Cor / tonalidade", image.color);
            var type = (Image.Type)EditorGUILayout.EnumPopup("Tipo", image.type);
            bool preserve = EditorGUILayout.Toggle("Preservar proporção", image.preserveAspect);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(image, "Editar imagem UI"); image.sprite = sprite; image.color = tint; image.type = type; image.preserveAspect = preserve; dirty = true; MarkPreviewDirty(); }
        }

        var tmp = selectedRect.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            EditorGUILayout.Space(8); EditorGUILayout.LabelField("Texto", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string value = EditorGUILayout.TextArea(tmp.text, GUILayout.MinHeight(48));
            var font = (TMP_FontAsset)EditorGUILayout.ObjectField("Fonte", tmp.font, typeof(TMP_FontAsset), false);
            float fontSize = EditorGUILayout.FloatField("Tamanho", tmp.fontSize);
            Color color = EditorGUILayout.ColorField("Cor", tmp.color);
            var fontStyle = (FontStyles)EditorGUILayout.EnumPopup("Estilo", tmp.fontStyle);
            var alignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alinhamento", tmp.alignment);
            bool wrap = EditorGUILayout.Toggle("Quebra de linha", tmp.textWrappingMode != TextWrappingModes.NoWrap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(tmp, "Editar texto UI"); tmp.text = value; tmp.font = font; tmp.fontSize = fontSize;
                tmp.color = color; tmp.fontStyle = fontStyle; tmp.alignment = alignment;
                tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap; dirty = true; MarkPreviewDirty();
            }
        }

        var button = selectedRect.GetComponent<Button>();
        if (button != null)
        {
            EditorGUILayout.Space(8); EditorGUILayout.LabelField("Botão", EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            var colors = button.colors;
            colors.normalColor = EditorGUILayout.ColorField("Normal", colors.normalColor);
            colors.highlightedColor = EditorGUILayout.ColorField("Realce", colors.highlightedColor);
            colors.pressedColor = EditorGUILayout.ColorField("Pressionado", colors.pressedColor);
            colors.disabledColor = EditorGUILayout.ColorField("Desativado", colors.disabledColor);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(button, "Editar botão UI"); button.colors = colors; dirty = true; MarkPreviewDirty(); }
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(locked))
        {
            if (GUILayout.Button("Duplicar", GUILayout.Height(25))) DuplicateSelected();
            if (GUILayout.Button("Excluir", GUILayout.Height(25))) DeleteSelected();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void DrawComponentsTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(300));
        GUILayout.Label("BIBLIOTECA DE COMPONENTES", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Salve paineis, botoes, cards e blocos como prefabs reutilizaveis entre telas.", MessageType.Info);
        componentScroll = EditorGUILayout.BeginScrollView(componentScroll, EditorStyles.helpBox);
        RefreshComponentList();
        for (int i = 0; i < componentPaths.Count; i++)
        {
            string label = Path.GetFileNameWithoutExtension(componentPaths[i]);
            if (GUILayout.Toggle(selectedComponentIndex == i, label, EditorStyles.miniButton)) selectedComponentIndex = i;
        }
        EditorGUILayout.EndScrollView();
        if (GUILayout.Button("Inserir componente na tela", GUILayout.Height(32))) InsertSelectedComponent();
        using (new EditorGUI.DisabledScope(componentPaths.Count == 0 || selectedComponentIndex < 0 || selectedComponentIndex >= componentPaths.Count))
        {
            if (GUILayout.Button("Duplicar componente", GUILayout.Height(27))) DuplicateSelectedComponentAsset();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        GUILayout.Label("CRIAR COMPONENTE REUTILIZÁVEL", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Na aba Canvas, selecione um objeto (e seus filhos). Volte aqui para salvar esse objeto como prefab na biblioteca.", MessageType.None);
        newComponentName = EditorGUILayout.TextField("Nome do componente", newComponentName);
        using (new EditorGUI.DisabledScope(selectedRect == null || selectedRect == rootRect))
        {
            if (GUILayout.Button("Salvar seleção na biblioteca", GUILayout.Height(36))) SaveSelectedAsComponent();
        }
        EditorGUILayout.Space(8);
        if (GUILayout.Button("Abrir pasta da biblioteca", GUILayout.Height(28))) RevealFolder(ComponentsPath);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    void DrawThemeTab()
    {
        GUILayout.Label("TEMA GLOBAL DO JOGO", EditorStyles.boldLabel);
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme == null)
        {
            EditorGUILayout.HelpBox("UITheme.asset não foi encontrado.", MessageType.Warning);
            return;
        }
        var so = new SerializedObject(theme);
        so.Update();
        EditorGUILayout.HelpBox("Alterações aqui definem a identidade visual global. Telas que usam o tema herdam cores, fontes, sprites e escala.", MessageType.Info);
        DrawProperty(so, "useEditorUI");
        DrawProperty(so, "polishPrefabButtons");
        EditorGUILayout.Space(6); EditorGUILayout.LabelField("Fontes", EditorStyles.boldLabel);
        DrawProperty(so, "font"); DrawProperty(so, "displayFont"); DrawProperty(so, "minFontSize"); DrawProperty(so, "fontScale"); DrawProperty(so, "titleScale");
        EditorGUILayout.Space(6); EditorGUILayout.LabelField("Paleta", EditorStyles.boldLabel);
        DrawProperty(so, "backgroundColor"); DrawProperty(so, "panelColor"); DrawProperty(so, "accentColor");
        DrawProperty(so, "goldColor"); DrawProperty(so, "grayColor"); DrawProperty(so, "buttonColor"); DrawProperty(so, "cyanColor");
        EditorGUILayout.Space(6); EditorGUILayout.LabelField("Sprites globais", EditorStyles.boldLabel);
        DrawProperty(so, "menuBackground"); DrawProperty(so, "panelSprite"); DrawProperty(so, "panelTint"); DrawProperty(so, "buttonSprite"); DrawProperty(so, "buttonTint");
        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets(); UITheme.Reload();
            status = "Tema global atualizado.";
        }
    }

    static void DrawProperty(SerializedObject so, string name)
    {
        var p = so.FindProperty(name);
        if (p != null) EditorGUILayout.PropertyField(p, true);
    }

    enum ElementKind { Panel, Button, Text, Image }
    void CreateElement(ElementKind kind)
    {
        if (prefabRoot == null) return;
        Transform parent = selectedRect != null ? selectedRect : rootRect;
        if (parent == null) parent = prefabRoot.transform;
        string name = kind == ElementKind.Panel ? "Painel_Novo" : kind == ElementKind.Button ? "Botao_Novo" : kind == ElementKind.Text ? "Texto_Novo" : "Imagem_Nova";
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Adicionar elemento UI");
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = kind == ElementKind.Text ? new Vector2(320, 54) : kind == ElementKind.Button ? new Vector2(280, 76) : new Vector2(300, 160);

        if (kind == ElementKind.Panel || kind == ElementKind.Button || kind == ElementKind.Image)
        {
            var img = go.AddComponent<Image>();
            img.color = kind == ElementKind.Button ? UITheme.Current.buttonColor : UITheme.Current.panelColor;
            if (kind == ElementKind.Button) img.sprite = UITheme.Current.buttonSprite;
            if (kind == ElementKind.Image) img.color = Color.white;
        }
        if (kind == ElementKind.Button)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.transition = Selectable.Transition.ColorTint;
            var buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = UITheme.Current.goldColor;
            buttonColors.pressedColor = new Color(0.72f, 0.48f, 0.18f, 1f);
            buttonColors.selectedColor = UITheme.Current.goldColor;
            buttonColors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = buttonColors;
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero; labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10, 6); labelRt.offsetMax = new Vector2(-10, -6);
            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "NOVO BOTÃO"; label.fontSize = 24; label.alignment = TextAlignmentOptions.Center;
            if (UITheme.Current.font != null) label.font = UITheme.Current.font;
            label.color = Color.white;
        }
        if (kind == ElementKind.Text)
        {
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = "Novo texto"; text.fontSize = 28; text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            if (UITheme.Current.font != null) text.font = UITheme.Current.font;
        }
        MarkRuntimeCreatable(go);
        selectedRect = rt; dirty = true; RefreshNodes(); MarkPreviewHierarchyDirty(); status = "Elemento criado: " + name;
    }

    void CreatePrefabAt(GameObject prefab, Vector2 rootLocalPosition)
    {
        if (prefab == null || rootRect == null) return;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rootRect);
        if (instance == null) return;
        instance.name = prefab.name;
        var rt = instance.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = rootLocalPosition - rootRect.rect.center;
        MarkRuntimeCreatable(instance);
        selectedRect = rt;
        dirty = true;
        RefreshNodes();
        MarkPreviewHierarchyDirty();
        status = "Componente inserido: " + prefab.name;
        Repaint();
    }

    void CreateImageAt(Sprite sprite, Vector2 rootLocalPosition)
    {
        if (sprite == null || rootRect == null) return;
        var go = new GameObject("Imagem_" + sprite.name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Adicionar sprite ao canvas");
        go.transform.SetParent(rootRect, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = rootLocalPosition - rootRect.rect.center;
        rt.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        MarkRuntimeCreatable(go);
        selectedRect = rt;
        dirty = true;
        RefreshNodes();
        MarkPreviewHierarchyDirty();
        status = "Sprite adicionado: " + sprite.name;
        Repaint();
    }

    void DuplicateSelected()
    {
        if (selectedRect == null || selectedRect == rootRect || IsEditorLocked(selectedRect)) return;
        var copy = Instantiate(selectedRect.gameObject, selectedRect.parent);
        copy.name = selectedRect.name + "_Copia";
        Undo.RegisterCreatedObjectUndo(copy, "Duplicar elemento UI");
        var rt = copy.GetComponent<RectTransform>();
        rt.anchoredPosition += new Vector2(24, -24);
        MarkRuntimeCreatable(copy);
        selectedRect = rt; dirty = true; RefreshNodes(); MarkPreviewHierarchyDirty();
    }

    void DeleteSelected()
    {
        if (selectedRect == null || selectedRect == rootRect || IsEditorLocked(selectedRect)) return;
        if (!EditorUtility.DisplayDialog("Excluir elemento", "Excluir '" + selectedRect.name + "' e seus filhos?", "Excluir", "Cancelar")) return;
        var doomed = selectedRect.gameObject; selectedRect = null;
        Undo.DestroyObjectImmediate(doomed); dirty = true; RefreshNodes(); MarkPreviewHierarchyDirty();
    }

    static bool IsRuntimeLayoutScreen(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        string name = Path.GetFileNameWithoutExtension(path);
        if (!name.StartsWith("Screen_", StringComparison.OrdinalIgnoreCase)) return false;
        string key = name.Substring("Screen_".Length);
        // Estas telas sao instanciadas diretamente como prefabs completos; as demais sao overlays visuais.
        // Main e select continuam sendo geradas pelo controlador para preservar callbacks e estado dinamico.
        return !new[] { "login", "nickname", "options", "credits", "slots" }
            .Contains(key, StringComparer.OrdinalIgnoreCase);
    }

    void MarkRuntimeCreatable(GameObject go)
    {
        if (go == null || !IsRuntimeLayoutScreen(selectedPath)) return;
        var flag = go.GetComponent<UIPrefabLayoutOverride>();
        if (flag == null) flag = go.AddComponent<UIPrefabLayoutOverride>();
        flag.runtimeCreateIfMissing = true;
    }

    void SaveSelectedAsComponent()
    {
        if (selectedRect == null || selectedRect == rootRect) return;
        Directory.CreateDirectory(ComponentsPath);
        string safe = Sanitize(string.IsNullOrWhiteSpace(newComponentName) ? selectedRect.name : newComponentName);
        string path = AssetDatabase.GenerateUniqueAssetPath(ComponentsPath + "/" + safe + ".prefab");
        var saved = PrefabUtility.SaveAsPrefabAsset(selectedRect.gameObject, path, out bool success);
        if (success && saved != null)
        {
            AssetDatabase.SaveAssets(); RefreshComponentList();
            status = "Componente salvo: " + path;
            EditorGUIUtility.PingObject(saved);
        }
        else status = "Não foi possível salvar o componente.";
    }

    void InsertSelectedComponent()
    {
        if (componentPaths.Count == 0 || selectedComponentIndex < 0 || selectedComponentIndex >= componentPaths.Count || rootRect == null) return;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(componentPaths[selectedComponentIndex]);
        if (asset == null) return;
        Transform parent = selectedRect != null ? selectedRect : rootRect;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        if (instance == null) return;
        var rt = instance.GetComponent<RectTransform>();
        if (rt != null) { rt.anchoredPosition = Vector2.zero; rt.localScale = Vector3.one; }
        MarkRuntimeCreatable(instance);
        selectedRect = rt; dirty = true; RefreshNodes(); MarkPreviewHierarchyDirty(); status = "Componente inserido: " + asset.name;
    }

    void SelectNode(RectTransform rect)
    {
        selectedRect = rect;
        Repaint();
    }

    void RefreshNodes()
    {
        nodes.Clear();
        if (rootRect == null) return;
        CollectNodes(rootRect);
    }

    void CollectNodes(RectTransform parent)
    {
        if (parent == null) return;
        nodes.Add(parent);
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i) is RectTransform child) CollectNodes(child);
    }

    int GetDepth(Transform t)
    {
        int d = 0;
        while (t != null && t != rootRect) { d++; t = t.parent; }
        return d;
    }

    void RefreshPrefabLists()
    {
        prefabPaths.Clear();
        if (Directory.Exists(ScreensPath))
        {
            foreach (var path in Directory.GetFiles(ScreensPath, "Screen_*.prefab", SearchOption.TopDirectoryOnly).OrderBy(p => p))
                prefabPaths.Add(path.Replace('\\', '/'));
        }
        if (Directory.Exists(OverlaysPath))
        {
            foreach (var path in Directory.GetFiles(OverlaysPath, "Overlay_*.prefab", SearchOption.TopDirectoryOnly).OrderBy(p => p))
                prefabPaths.Add(path.Replace('\\', '/'));
        }
        RefreshComponentList();
        if (prefabPaths.Count > 0 && string.IsNullOrEmpty(selectedPath))
        {
            string preferred = prefabPaths.FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == "Screen_main");
            LoadPrefab(preferred ?? prefabPaths[0]);
        }
    }

    void RefreshComponentList()
    {
        componentPaths.Clear();
        if (!Directory.Exists(ComponentsPath)) return;
        componentPaths.AddRange(Directory.GetFiles(ComponentsPath, "*.prefab", SearchOption.TopDirectoryOnly).Select(p => p.Replace('\\', '/')).OrderBy(p => p));
        selectedComponentIndex = Mathf.Clamp(selectedComponentIndex, 0, Mathf.Max(0, componentPaths.Count - 1));
    }

    void LoadPrefab(string path)
    {
        if (dirty && !ConfirmDiscardIfNeeded()) return;
        UnloadPrefab();
        selectedPath = path;
        try
        {
            prefabRoot = PrefabUtility.LoadPrefabContents(path);
            rootRect = prefabRoot != null ? prefabRoot.GetComponent<RectTransform>() : null;
            if (rootRect == null && prefabRoot != null) rootRect = prefabRoot.GetComponentInChildren<RectTransform>();
            selectedRect = null;
            dirty = false;
            RefreshNodes();
            MarkPreviewHierarchyDirty();
            status = "Aberto: " + Path.GetFileName(path);
        }
        catch (Exception ex)
        {
            prefabRoot = null; rootRect = null; status = "Erro ao abrir prefab: " + ex.Message;
            Debug.LogException(ex);
        }
    }

    void SavePrefab()
    {
        if (prefabRoot == null || string.IsNullOrEmpty(selectedPath)) return;
        try
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, selectedPath, out bool success);
            if (!success) { status = "Falha ao salvar o prefab."; return; }
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); dirty = false;
            status = "Salvo: " + Path.GetFileName(selectedPath);
        }
        catch (Exception ex) { status = "Erro ao salvar: " + ex.Message; Debug.LogException(ex); }
    }

    void UnloadPrefab()
    {
        if (prefabRoot != null)
        {
            if (dirty && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
                Debug.LogWarning("[ZomBit Visual Editor] Alterações não salvas descartadas em " + selectedPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
        prefabRoot = null; rootRect = null; selectedRect = null; nodes.Clear(); nodeRects.Clear(); dirty = false; MarkPreviewHierarchyDirty();
    }

    bool ConfirmDiscardIfNeeded()
    {
        if (!dirty) return true;
        int result = EditorUtility.DisplayDialogComplex("Alterações não salvas", "Deseja salvar as alterações antes de trocar de tela?", "Salvar", "Cancelar", "Descartar");
        if (result == 0) { SavePrefab(); return !dirty; }
        if (result == 1) return false;
        dirty = false; return true;
    }

    void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(dirty ? "● Alterações não salvas" : "● Salvo", GUILayout.Width(145));
        GUILayout.Label(status);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Salvar", EditorStyles.toolbarButton, GUILayout.Width(55))) SavePrefab();
        EditorGUILayout.EndHorizontal();
    }

    void OpenInPrefabMode()
    {
        if (string.IsNullOrEmpty(selectedPath)) return;
        if (!ConfirmDiscardIfNeeded()) return;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
        if (asset != null) AssetDatabase.OpenAsset(asset);
    }

    void OpenGuide()
    {
        var text = AssetDatabase.LoadAssetAtPath<TextAsset>(GuidePath);
        if (text != null) AssetDatabase.OpenAsset(text);
        else EditorUtility.DisplayDialog("Guia", "Guia não encontrado. Veja Documentacao/EDITOR_VISUAL_GUIA_PT_BR.md", "OK");
    }

    static void RevealFolder(string path)
    {
        Directory.CreateDirectory(path);
        var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
        EditorUtility.RevealInFinder(Path.GetFullPath(path));
    }

    static string Sanitize(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value.Trim().Replace(' ', '_');
    }
}
#endif
