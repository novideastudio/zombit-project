#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed partial class ZombitInterfaceWorkspace
{
    enum ResizeHandle { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
    enum PresetKind { Card, HealthBar, Inventory, CombatHud }

    bool useUnityCanvasPreview = true;
    bool snapEnabled = true;
    bool smartSnap = true;
    bool showGuides = true;
    bool showGridOptions;
    bool showGridLines;
    int gridSize = 8;
    ResizeHandle activeHandle;
    int dragUndoGroup = -1;
    bool snapGuideXActive;
    bool snapGuideYActive;

    Scene unityPreviewScene;
    GameObject unityPreviewInstance;
    GameObject unityPreviewCanvasWrapper;
    Camera unityPreviewCamera;
    RenderTexture unityPreviewTexture;
    Canvas unityPreviewCanvas;
    bool unityPreviewHierarchyDirty = true;
    bool unityPreviewLayoutDirty = true;

    void HandleUndoRedoCommands()
    {
        var e = Event.current;
        if (e == null || e.type != EventType.ValidateCommand && e.type != EventType.ExecuteCommand) return;
        if (e.commandName == "Undo")
        {
            if (e.type == EventType.ExecuteCommand) PerformWorkspaceUndo();
            e.Use();
        }
        else if (e.commandName == "Redo")
        {
            if (e.type == EventType.ExecuteCommand) PerformWorkspaceRedo();
            e.Use();
        }
    }

    void PerformWorkspaceUndo()
    {
        if (prefabRoot == null) return;
        Undo.PerformUndo();
        dirty = true;
        RefreshNodes();
        MarkPreviewHierarchyDirty();
        status = "Desfeito.";
        Repaint();
    }

    void PerformWorkspaceRedo()
    {
        if (prefabRoot == null) return;
        Undo.PerformRedo();
        dirty = true;
        RefreshNodes();
        MarkPreviewHierarchyDirty();
        status = "Refeito.";
        Repaint();
    }

    void OnWorkspaceUndoRedo()
    {
        if (prefabRoot == null) return;
        dirty = true;
        RefreshNodes();
        MarkPreviewHierarchyDirty();
        Repaint();
    }

    Vector2 GetWorkspaceResolution()
    {
        // A resolucao de referencia do CanvasScaler e o tamanho de projeto da tela.
        // Nao usar o rect atual do Canvas raiz: no Prefab Mode ele pode refletir o tamanho
        // da janela/monitor do Editor (ex.: ultrawide), e nao o layout de design do jogo.
        // Todas as telas compartilham a resolucao de design Full HD (UIResolution).
        return UIResolution.Reference;
    }

    void PopulateNodeRects(Vector2 viewSize, float fit)
    {
        nodeRects.Clear();
        foreach (var node in nodes)
        {
            if (node == null || node == rootRect || !node.gameObject.activeSelf) continue;
            Rect relative = CalculateRelativeRect(node);
            Rect gui = new Rect(
                viewSize.x * 0.5f + (relative.center.x - rootRect.rect.center.x) * fit - relative.width * fit * 0.5f,
                viewSize.y * 0.5f - (relative.center.y - rootRect.rect.center.y) * fit - relative.height * fit * 0.5f,
                relative.width * fit, relative.height * fit);
            if (gui.width >= 1 && gui.height >= 1) nodeRects[node] = gui;
        }
    }

    void DrawSelectionHandles()
    {
        if (selectedRect == null || !nodeRects.TryGetValue(selectedRect, out var r)) return;
        Handles.BeginGUI();
        Handles.color = new Color(1f, 0.67f, 0.12f, 1f);
        Handles.DrawAAPolyLine(2f,
            new Vector3(r.xMin, r.yMin), new Vector3(r.xMax, r.yMin),
            new Vector3(r.xMax, r.yMax), new Vector3(r.xMin, r.yMax), new Vector3(r.xMin, r.yMin));
        Handles.EndGUI();
        foreach (var handle in GetHandleRects(r))
        {
            EditorGUI.DrawRect(handle.rect, new Color(0.08f, 0.10f, 0.12f, 1f));
            GUI.Box(handle.rect, GUIContent.none, EditorStyles.whiteMiniLabel);
        }
    }

    struct HandleRect
    {
        public ResizeHandle handle;
        public Rect rect;
        public HandleRect(ResizeHandle h, Rect r) { handle = h; rect = r; }
    }

    IEnumerable<HandleRect> GetHandleRects(Rect r)
    {
        const float h = 8f;
        float cx = r.center.x, cy = r.center.y;
        yield return new HandleRect(ResizeHandle.TopLeft, new Rect(r.xMin - h * 0.5f, r.yMin - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.Top, new Rect(cx - h * 0.5f, r.yMin - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.TopRight, new Rect(r.xMax - h * 0.5f, r.yMin - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.Right, new Rect(r.xMax - h * 0.5f, cy - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.BottomRight, new Rect(r.xMax - h * 0.5f, r.yMax - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.Bottom, new Rect(cx - h * 0.5f, r.yMax - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.BottomLeft, new Rect(r.xMin - h * 0.5f, r.yMax - h * 0.5f, h, h));
        yield return new HandleRect(ResizeHandle.Left, new Rect(r.xMin - h * 0.5f, cy - h * 0.5f, h, h));
    }

    ResizeHandle GetResizeHandle(Rect r, Vector2 mouse)
    {
        if (selectedRect == null || !nodeRects.TryGetValue(selectedRect, out var selected) || selected != r) return ResizeHandle.None;
        foreach (var handle in GetHandleRects(r)) if (handle.rect.Contains(mouse)) return handle.handle;
        return ResizeHandle.None;
    }

    void BeginCanvasDrag(RectTransform rect, Vector2 mousePosition, ResizeHandle handle)
    {
        dragRect = rect;
        Undo.IncrementCurrentGroup();
        dragUndoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(handle == ResizeHandle.None ? "Mover elemento UI" : "Redimensionar elemento UI");
        dragStartMouse = mousePosition;
        dragStartPosition = rect.anchoredPosition;
        dragStartSize = rect.sizeDelta;
        activeHandle = handle;
    }

    void ResizeFromHandle(RectTransform rect, Vector2 startPosition, Vector2 startSize, Vector2 delta, ResizeHandle handle)
    {
        bool left = handle == ResizeHandle.Left || handle == ResizeHandle.TopLeft || handle == ResizeHandle.BottomLeft;
        bool right = handle == ResizeHandle.Right || handle == ResizeHandle.TopRight || handle == ResizeHandle.BottomRight;
        bool top = handle == ResizeHandle.Top || handle == ResizeHandle.TopLeft || handle == ResizeHandle.TopRight;
        bool bottom = handle == ResizeHandle.Bottom || handle == ResizeHandle.BottomLeft || handle == ResizeHandle.BottomRight;
        float widthDelta = left ? -delta.x : right ? delta.x : 0f;
        float heightDelta = top ? delta.y : bottom ? -delta.y : 0f;
        float shiftX = left || right ? delta.x * 0.5f : 0f;
        float shiftY = top || bottom ? delta.y * 0.5f : 0f;
        rect.sizeDelta = new Vector2(Mathf.Max(12f, startSize.x + widthDelta), Mathf.Max(12f, startSize.y + heightDelta));
        rect.anchoredPosition = startPosition + new Vector2(shiftX, shiftY);
    }

    void SnapSize(RectTransform rect)
    {
        if (!snapEnabled || gridSize <= 1) return;
        Vector2 size = rect.sizeDelta;
        size.x = Mathf.Max(12, Mathf.Round(size.x / gridSize) * gridSize);
        size.y = Mathf.Max(12, Mathf.Round(size.y / gridSize) * gridSize);
        rect.sizeDelta = size;
    }

    void SnapPosition(RectTransform rect, ref Vector2 target)
    {
        snapGuideXActive = false;
        snapGuideYActive = false;
        if (!snapEnabled) return;
        float threshold = Mathf.Max(4, gridSize * 1.25f);
        if (gridSize > 1)
        {
            target.x = Mathf.Round(target.x / gridSize) * gridSize;
            target.y = Mathf.Round(target.y / gridSize) * gridSize;
        }
        if (!smartSnap || rect == null) return;
        float bestX = threshold + 0.01f, bestY = threshold + 0.01f;
        float snapX = target.x, snapY = target.y;
        foreach (var other in nodes)
        {
            if (other == null || other == rect || other.parent != rect.parent || !other.gameObject.activeInHierarchy) continue;
            float dx = Mathf.Abs(other.anchoredPosition.x - target.x);
            if (dx < bestX) { bestX = dx; snapX = other.anchoredPosition.x; }
            float dy = Mathf.Abs(other.anchoredPosition.y - target.y);
            if (dy < bestY) { bestY = dy; snapY = other.anchoredPosition.y; }
        }
        if (bestX <= threshold) { target.x = snapX; snapGuideXActive = true; }
        if (bestY <= threshold) { target.y = snapY; snapGuideYActive = true; }
    }

    void DrawSnapGrid(Vector2 viewSize, float fit)
    {
        if (!showGridLines || gridSize < 2 || fit <= 0) return;
        float step = gridSize * fit;
        int stride = Mathf.Max(1, Mathf.CeilToInt(7f / Mathf.Max(1f, step)));
        step *= stride;
        Color c = new Color(.65f, .78f, .88f, .12f);
        float cx = viewSize.x * .5f, cy = viewSize.y * .5f;
        for (float x = cx % step; x < viewSize.x; x += step) EditorGUI.DrawRect(new Rect(x, 0, 1, viewSize.y), c);
        for (float y = cy % step; y < viewSize.y; y += step) EditorGUI.DrawRect(new Rect(0, y, viewSize.x, 1), c);
    }

    void DrawAlignmentGuides(Vector2 viewSize)
    {
        if (!showGuides || selectedRect == null || !nodeRects.TryGetValue(selectedRect, out var r)) return;
        Handles.BeginGUI();
        Handles.color = new Color(0.15f, 0.88f, 1f, 0.85f);
        if (snapGuideXActive) Handles.DrawAAPolyLine(1.5f, new Vector3(r.center.x, 0), new Vector3(r.center.x, viewSize.y));
        if (snapGuideYActive) Handles.DrawAAPolyLine(1.5f, new Vector3(0, r.center.y), new Vector3(viewSize.x, r.center.y));
        Handles.EndGUI();
    }

    bool IsEditorLocked(RectTransform node)
    {
        for (Transform t = node; t != null && t != rootRect; t = t.parent)
        {
            var meta = t.GetComponent<UIPrefabLayoutOverride>();
            if (meta != null && meta.editorLocked) return true;
        }
        return false;
    }

    void SetEditorLocked(RectTransform node, bool value)
    {
        if (node == null) return;
        var meta = node.GetComponent<UIPrefabLayoutOverride>();
        if (meta == null) meta = Undo.AddComponent<UIPrefabLayoutOverride>(node.gameObject);
        Undo.RecordObject(meta, value ? "Bloquear elemento UI" : "Desbloquear elemento UI");
        meta.editorLocked = value;
        EditorUtility.SetDirty(meta);
        dirty = true;
        status = value ? "Elemento bloqueado." : "Elemento desbloqueado.";
    }

    void ToggleSelectedVisibility()
    {
        if (selectedRect == null || selectedRect == rootRect || IsEditorLocked(selectedRect)) return;
        Undo.RecordObject(selectedRect.gameObject, "Alternar visibilidade UI");
        selectedRect.gameObject.SetActive(!selectedRect.gameObject.activeSelf);
        if (!string.IsNullOrEmpty(selectedPath) && IsRuntimeLayoutScreen(selectedPath))
        {
            var meta = selectedRect.GetComponent<UIPrefabLayoutOverride>();
            if (meta == null) meta = Undo.AddComponent<UIPrefabLayoutOverride>(selectedRect.gameObject);
            Undo.RecordObject(meta, "Aplicar visibilidade no runtime");
            meta.overrideActiveState = true;
        }
        dirty = true;
        MarkPreviewDirty();
        RefreshNodes();
    }

    void MoveLayer(RectTransform node, int direction)
    {
        if (node == null || node == rootRect || IsEditorLocked(node)) return;
        Undo.RecordObject(node, "Reordenar camada UI");
        int last = node.parent.childCount - 1;
        int index = node.GetSiblingIndex();
        if (direction == 1) node.SetSiblingIndex(Mathf.Min(last, index + 1));
        else if (direction == -1) node.SetSiblingIndex(Mathf.Max(0, index - 1));
        else if (direction == 2) node.SetAsLastSibling();
        else if (direction == -2) node.SetAsFirstSibling();
        if (!string.IsNullOrEmpty(selectedPath) && IsRuntimeLayoutScreen(selectedPath))
        {
            var meta = node.GetComponent<UIPrefabLayoutOverride>();
            if (meta == null) meta = Undo.AddComponent<UIPrefabLayoutOverride>(node.gameObject);
            Undo.RecordObject(meta, "Aplicar ordem de camada no runtime");
            meta.overrideSiblingOrder = true;
        }
        dirty = true;
        MarkPreviewHierarchyDirty();
        EditorApplication.delayCall += RefreshNodesAfterLayerChange;
        status = "Ordem das camadas atualizada.";
    }

    void RefreshNodesAfterLayerChange()
    {
        if (this == null || prefabRoot == null) return;
        RefreshNodes();
        Repaint();
    }

    void DuplicateCurrentScreen()
    {
        if (prefabRoot == null || string.IsNullOrEmpty(selectedPath)) return;
        if (dirty)
        {
            int decision = EditorUtility.DisplayDialogComplex("Duplicar tela", "Há alterações pendentes. Salvar antes de duplicar?", "Salvar e duplicar", "Cancelar", "Descartar e duplicar");
            if (decision == 1) return;
            string currentPath = selectedPath;
            if (decision == 0) SavePrefab();
            else
            {
                dirty = false;
                LoadPrefab(currentPath); // recarrega o asset para descartar de verdade as alterações locais.
            }
            if (dirty || prefabRoot == null) return;
        }
        string name = Path.GetFileNameWithoutExtension(selectedPath) + "_Copia";
        string path = AssetDatabase.GenerateUniqueAssetPath(ScreensPath + "/" + name + ".prefab");
        GameObject clone = Instantiate(prefabRoot);
        clone.name = Path.GetFileNameWithoutExtension(path);
        PrefabUtility.SaveAsPrefabAsset(clone, path, out bool success);
        DestroyImmediate(clone);
        if (!success) { status = "Não foi possível duplicar a tela."; return; }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        dirty = false;
        RefreshPrefabLists();
        LoadPrefab(path);
        status = "Tela duplicada: " + Path.GetFileName(path);
    }

    void DuplicateSelectedComponentAsset()
    {
        if (selectedComponentIndex < 0 || selectedComponentIndex >= componentPaths.Count) return;
        string source = componentPaths[selectedComponentIndex];
        string name = Path.GetFileNameWithoutExtension(source) + "_Copia";
        string destination = AssetDatabase.GenerateUniqueAssetPath(ComponentsPath + "/" + name + ".prefab");
        if (AssetDatabase.CopyAsset(source, destination))
        {
            AssetDatabase.Refresh(); RefreshComponentList();
            selectedComponentIndex = componentPaths.IndexOf(destination);
            status = "Componente duplicado: " + Path.GetFileName(destination);
        }
        else status = "Não foi possível duplicar o componente.";
    }

    void CreatePreset(PresetKind kind)
    {
        if (prefabRoot == null || rootRect == null) return;
        Transform parent = selectedRect != null && selectedRect != rootRect ? selectedRect : rootRect;
        string name = kind == PresetKind.Card ? "Card_Pronto" : kind == PresetKind.HealthBar ? "BarraVida_Pronto" : kind == PresetKind.Inventory ? "Inventario_4x3" : "HUD_Combate";
        GameObject root = NewUiObject(name, parent, kind == PresetKind.Card ? new Vector2(360, 190) : kind == PresetKind.HealthBar ? new Vector2(420, 52) : kind == PresetKind.Inventory ? new Vector2(440, 330) : new Vector2(880, 112));
        AddImage(root, UITheme.Current.panelColor, UITheme.Current.panelSprite);
        if (kind == PresetKind.Card)
        {
            AddText(root.transform, "Titulo", "TÍTULO DO CARTÃO", 25, UITheme.Current.goldColor, new Vector2(0, 54), new Vector2(320, 44), TextAlignmentOptions.Center);
            AddText(root.transform, "Descricao", "Descrição ou informação do elemento.", 18, Color.white, new Vector2(0, -28), new Vector2(310, 72), TextAlignmentOptions.Center);
            var strip = NewUiObject("Destaque", root.transform, new Vector2(300, 4)); strip.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80); AddImage(strip, UITheme.Current.goldColor, null);
        }
        else if (kind == PresetKind.HealthBar)
        {
            var fill = NewUiObject("Preenchimento", root.transform, new Vector2(390, 20));
            var rt = fill.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, .5f); rt.anchorMax = new Vector2(.72f, .5f); rt.pivot = new Vector2(0, .5f); rt.anchoredPosition = new Vector2(12, 0); rt.sizeDelta = new Vector2(0, 22);
            var image = AddImage(fill, new Color(0.16f, 0.78f, 0.29f), null); image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillAmount = .72f;
            AddText(root.transform, "Valor", "72 / 100", 20, Color.white, Vector2.zero, new Vector2(390, 44), TextAlignmentOptions.Center);
        }
        else if (kind == PresetKind.Inventory)
        {
            var grid = NewUiObject("Grade_Itens", root.transform, new Vector2(400, 280));
            var gridRt = grid.GetComponent<RectTransform>(); gridRt.anchorMin = Vector2.zero; gridRt.anchorMax = Vector2.one; gridRt.offsetMin = new Vector2(14, 14); gridRt.offsetMax = new Vector2(-14, -14);
            var layout = grid.AddComponent<GridLayoutGroup>(); layout.cellSize = new Vector2(82, 78); layout.spacing = new Vector2(12, 8); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = 4; layout.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < 12; i++) { var slot = NewUiObject("Slot_" + (i + 1).ToString("00"), grid.transform, new Vector2(82, 78)); AddImage(slot, new Color(.08f, .12f, .16f, .95f), UITheme.Current.buttonSprite); AddText(slot.transform, "Numero", (i + 1).ToString("00"), 13, new Color(1,1,1,.6f), new Vector2(0,-24), new Vector2(76,20), TextAlignmentOptions.BottomRight); }
        }
        else
        {
            AddText(root.transform, "Onda", "ONDA 01 / 05", 24, UITheme.Current.goldColor, new Vector2(-280, 0), new Vector2(260, 64), TextAlignmentOptions.Center);
            AddText(root.transform, "Gerador", "GERADOR 100%", 22, Color.white, new Vector2(0, 0), new Vector2(260, 64), TextAlignmentOptions.Center);
            AddText(root.transform, "Abates", "ABATES 000", 22, Color.white, new Vector2(280, 0), new Vector2(260, 64), TextAlignmentOptions.Center);
        }
        MarkRuntimeCreatable(root);
        selectedRect = root.GetComponent<RectTransform>();
        dirty = true; RefreshNodes(); MarkPreviewHierarchyDirty();
        status = "Modelo pronto criado: " + name;
    }

    GameObject NewUiObject(string name, Transform parent, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Criar modelo de UI");
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        return go;
    }

    Image AddImage(GameObject go, Color color, Sprite sprite)
    {
        var image = go.GetComponent<Image>(); if (image == null) image = go.AddComponent<Image>();
        image.color = color; image.sprite = sprite; image.raycastTarget = false;
        return image;
    }

    TMP_Text AddText(Transform parent, string name, string value, float size, Color color, Vector2 pos, Vector2 dimensions, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Criar texto de UI"); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(.5f,.5f); rt.pivot = new Vector2(.5f,.5f); rt.anchoredPosition = pos; rt.sizeDelta = dimensions;
        var text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.color = color; text.alignment = alignment; text.raycastTarget = false;
        if (UITheme.Current.font != null) text.font = UITheme.Current.font;
        return text;
    }

    void MarkPreviewDirty() { unityPreviewLayoutDirty = true; }
    void MarkPreviewHierarchyDirty() { unityPreviewHierarchyDirty = true; unityPreviewLayoutDirty = true; }

    // Componentes que ajustam o layout em runtime (ex.: PcTitleFit) nao recebem LateUpdate
    // dentro da PreviewScene do Editor. Executamos esses drivers explicitamente apos o Canvas
    // obter seu tamanho real, para a previa mostrar a mesma escala do jogo.
    void ApplyEditorPreviewLayoutDrivers()
    {
        if (unityPreviewInstance == null) return;
        Canvas.ForceUpdateCanvases();
        foreach (var fit in unityPreviewInstance.GetComponentsInChildren<PcTitleFit>(true))
            if (fit != null) fit.Apply();
        Canvas.ForceUpdateCanvases();
    }

    void DrawUnityCanvasPreview(Vector2 viewSize, float fit)
    {
        if (Event.current.type != EventType.Repaint) return;
        EnsureUnityPreview();
        if (unityPreviewInstance == null || unityPreviewCamera == null || unityPreviewTexture == null)
        {
            EditorGUI.HelpBox(new Rect(8, 8, viewSize.x - 16, 42), "Não foi possível criar a prévia real do Canvas. Use o modo de prévia simplificada.", MessageType.Warning);
            return;
        }
        if (!UnityPreviewHierarchyMatches(prefabRoot.transform, unityPreviewInstance.transform)) RebuildUnityPreviewInstance();
        else SyncUnityPreviewHierarchy(prefabRoot.transform, unityPreviewInstance.transform);
        ApplyEditorPreviewLayoutDrivers();
        if (unityPreviewCamera != null && unityPreviewTexture != null)
        {
            unityPreviewCamera.targetTexture = unityPreviewTexture;
            if (unityPreviewLayoutDirty)
            {
                Canvas.ForceUpdateCanvases();
                foreach (var rect in unityPreviewInstance.GetComponentsInChildren<RectTransform>(true).Reverse())
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Canvas.ForceUpdateCanvases();
                unityPreviewLayoutDirty = false;
            }
            // URP/HDRP use Render Requests; Built-in RP falls back to Camera.Render.
            var renderRequest = new RenderPipeline.StandardRequest
            {
                destination = unityPreviewTexture,
                isPreview = true
            };
            if (RenderPipeline.SupportsRenderRequest(unityPreviewCamera, renderRequest))
                RenderPipeline.SubmitRenderRequest(unityPreviewCamera, renderRequest);
            else
                unityPreviewCamera.Render();
            // EditorGUI.DrawPreviewTexture resolve a orientacao de amostragem da RenderTexture
            // para a API grafica atual. Nao inverter UV manualmente: no DX12 isso deixava o menu de cabeca para baixo.
            EditorGUI.DrawPreviewTexture(new Rect(0, 0, viewSize.x, viewSize.y), unityPreviewTexture,
                null, ScaleMode.StretchToFill, GetWorkspaceResolution().x / Mathf.Max(1f, GetWorkspaceResolution().y));
        }
    }

    void EnsureUnityPreview()
    {
        if (!unityPreviewScene.IsValid() || !unityPreviewScene.isLoaded)
            unityPreviewScene = EditorSceneManager.NewPreviewScene();
        if (unityPreviewHierarchyDirty || unityPreviewInstance == null)
            RebuildUnityPreviewInstance();
        if (unityPreviewCamera == null)
        {
            var cameraGo = new GameObject("ZomBit_UI_Preview_Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraGo, unityPreviewScene);
            unityPreviewCamera = cameraGo.GetComponent<Camera>();
            unityPreviewCamera.enabled = false;
            unityPreviewCamera.cameraType = CameraType.Preview;
            // Render only objects belonging to this isolated Preview Scene.
            unityPreviewCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(unityPreviewScene);
            unityPreviewCamera.orthographic = true;
            unityPreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            unityPreviewCamera.backgroundColor = new Color(.07f, .08f, .10f, 1f);
            unityPreviewCamera.nearClipPlane = .1f; unityPreviewCamera.farClipPlane = 1000f;
            unityPreviewCamera.transform.position = new Vector3(0, 0, -100f);
            unityPreviewCamera.transform.rotation = Quaternion.identity;
        }
        Vector2 res = GetWorkspaceResolution();
        if (res.x < 100 || res.y < 100) res = new Vector2(1920,1080);
        if (unityPreviewTexture == null || unityPreviewTexture.width != PreviewWidth(res) || unityPreviewTexture.height != PreviewHeight(res))
        {
            if (unityPreviewTexture != null) { unityPreviewTexture.Release(); DestroyImmediate(unityPreviewTexture); }
            unityPreviewTexture = new RenderTexture(PreviewWidth(res), PreviewHeight(res), 24, RenderTextureFormat.ARGB32)
            { name = "ZomBit_UI_Preview_RT", antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
            unityPreviewTexture.Create();
        }
        ConfigurePreviewCanvases();
    }

    int PreviewHeight(Vector2 res) => 900;
    // Preservar a proporcao de referencia, inclusive para telas ultrawide.
    int PreviewWidth(Vector2 res) => Mathf.Clamp(Mathf.RoundToInt(900f * res.x / Mathf.Max(1f, res.y)), 800, 4096);

    void RebuildUnityPreviewInstance()
    {
        DestroyUnityPreviewInstance();
        if (prefabRoot == null || !unityPreviewScene.IsValid()) return;

        // Toda tela e apresentada dentro de um Canvas de preview padrao. Os prefabs Screen_*
        // possuem um Canvas/Scaler na raiz apenas para Prefab Mode; em runtime o controlador
        // remove esses componentes e estica a raiz para preencher o MenuCanvas. Reproduzimos
        // esse mesmo processo aqui para que o fundo (MenuArt/MenuBackdrop) receba dimensoes reais.
        var canvasGo = new GameObject("ZomBit_UI_Preview_Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SceneManager.MoveGameObjectToScene(canvasGo, unityPreviewScene);
        unityPreviewCanvasWrapper = canvasGo;
        unityPreviewCanvas = canvasGo.GetComponent<Canvas>();
        unityPreviewCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        unityPreviewCanvas.pixelPerfect = false;
        var previewScaler = canvasGo.GetComponent<CanvasScaler>();
        UIResolution.Apply(previewScaler);

        unityPreviewInstance = Instantiate(prefabRoot);
        unityPreviewInstance.name = prefabRoot.name;
        SceneManager.MoveGameObjectToScene(unityPreviewInstance, unityPreviewScene);
        // Prefabs Screen_* ficam inativos por padrao para o runtime ativa-los sob demanda.
        // Na cena isolada do Editor, porem, precisamos exibir a copia para renderizar a UI.
        // Isso altera somente a instancia temporaria, nunca o prefab salvo.
        if (!unityPreviewInstance.activeSelf) unityPreviewInstance.SetActive(true);

        // Retira apenas os componentes de Canvas da raiz clonada. Nenhum componente do prefab
        // original e alterado; Canvases realmente aninhados continuam preservados.
        var rootCanvas = unityPreviewInstance.GetComponent<Canvas>();
        if (rootCanvas != null) DestroyImmediate(rootCanvas);
        var rootScaler = unityPreviewInstance.GetComponent<CanvasScaler>();
        if (rootScaler != null) DestroyImmediate(rootScaler);
        var rootRaycaster = unityPreviewInstance.GetComponent<GraphicRaycaster>();
        if (rootRaycaster != null) DestroyImmediate(rootRaycaster);

        var previewRootRect = unityPreviewInstance.GetComponent<RectTransform>();
        if (previewRootRect != null)
        {
            previewRootRect.SetParent(canvasGo.transform, false);
            previewRootRect.anchorMin = Vector2.zero;
            previewRootRect.anchorMax = Vector2.one;
            previewRootRect.pivot = new Vector2(.5f, .5f);
            previewRootRect.offsetMin = Vector2.zero;
            previewRootRect.offsetMax = Vector2.zero;
            previewRootRect.anchoredPosition3D = Vector3.zero;
            previewRootRect.localRotation = Quaternion.identity;
            previewRootRect.localScale = Vector3.one;
        }
        else
        {
            unityPreviewInstance.transform.SetParent(canvasGo.transform, false);
        }

        unityPreviewHierarchyDirty = false;
        ConfigurePreviewCanvases();
        ApplyEditorPreviewLayoutDrivers();
    }

    void ConfigurePreviewCanvases()
    {
        if (unityPreviewInstance == null || unityPreviewCamera == null) return;
        if (unityPreviewCanvasWrapper != null)
        {
            var wrapperCanvas = unityPreviewCanvasWrapper.GetComponent<Canvas>();
            if (wrapperCanvas != null)
            {
                wrapperCanvas.enabled = true;
                wrapperCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                wrapperCanvas.worldCamera = unityPreviewCamera;
                wrapperCanvas.planeDistance = 100f;
                wrapperCanvas.pixelPerfect = false;
            }
            var wrapperScaler = unityPreviewCanvasWrapper.GetComponent<CanvasScaler>();
            if (wrapperScaler != null)
            {
                UIResolution.Apply(wrapperScaler);
            }
        }
        // Somente Canvases internos da tela; o Canvas externo foi criado acima.
        foreach (var canvas in unityPreviewInstance.GetComponentsInChildren<Canvas>(true))
        {
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = unityPreviewCamera;
            canvas.planeDistance = 99f;
            canvas.pixelPerfect = false;
        }
        Vector2 res = GetWorkspaceResolution();
        unityPreviewCamera.orthographicSize = res.y * .5f;
        unityPreviewCamera.aspect = res.x / Mathf.Max(1, res.y);
        unityPreviewCamera.targetTexture = unityPreviewTexture;
    }

    bool UnityPreviewHierarchyMatches(Transform source, Transform target)
    {
        if (source == null || target == null || source.name != target.name || source.childCount != target.childCount) return false;
        for (int i = 0; i < source.childCount; i++)
            if (!UnityPreviewHierarchyMatches(source.GetChild(i), target.GetChild(i))) return false;
        return true;
    }

    void SyncUnityPreviewHierarchy(Transform source, Transform target)
    {
        if (source == null || target == null) return;
        bool isScreenRoot = source == (prefabRoot != null ? prefabRoot.transform : null);
        // A raiz Screen_* e deliberadamente inativa no prefab; mantenha a copia de preview ativa.
        // Os estados dos filhos continuam sendo sincronizados normalmente.
        target.gameObject.SetActive(isScreenRoot ? true : source.gameObject.activeSelf);
        var sr = source as RectTransform; var tr = target as RectTransform;
        if (sr != null && tr != null)
        {
            if (isScreenRoot)
            {
                // O Screen_* e uma raiz especial: o jogo a normaliza para preencher o MenuCanvas.
                // Nunca copie para a previa o sentinel do prefab (scale/size zero, anchors 0,0).
                if (tr.anchorMin != Vector2.zero) tr.anchorMin = Vector2.zero;
                if (tr.anchorMax != Vector2.one) tr.anchorMax = Vector2.one;
                if (tr.pivot != new Vector2(.5f, .5f)) tr.pivot = new Vector2(.5f, .5f);
                if (tr.offsetMin != Vector2.zero) tr.offsetMin = Vector2.zero;
                if (tr.offsetMax != Vector2.zero) tr.offsetMax = Vector2.zero;
                if (tr.anchoredPosition3D != Vector3.zero) tr.anchoredPosition3D = Vector3.zero;
                if (tr.localScale != Vector3.one) tr.localScale = Vector3.one;
                if (tr.localRotation != Quaternion.identity) tr.localRotation = Quaternion.identity;
            }
            else
            {
                if (tr.anchorMin != sr.anchorMin) tr.anchorMin = sr.anchorMin;
                if (tr.anchorMax != sr.anchorMax) tr.anchorMax = sr.anchorMax;
                if (tr.anchoredPosition3D != sr.anchoredPosition3D) tr.anchoredPosition3D = sr.anchoredPosition3D;
                if (tr.sizeDelta != sr.sizeDelta) tr.sizeDelta = sr.sizeDelta;
                if (tr.pivot != sr.pivot) tr.pivot = sr.pivot;
                if (tr.localScale != sr.localScale) tr.localScale = sr.localScale;
                if (tr.localRotation != sr.localRotation) tr.localRotation = sr.localRotation;
            }
        }
        var si = source.GetComponent<Image>(); var ti = target.GetComponent<Image>();
        if (si != null && ti != null)
        {
            if (ti.sprite != si.sprite) ti.sprite = si.sprite;
            if (ti.color != si.color) ti.color = si.color;
            if (ti.type != si.type) ti.type = si.type;
            if (ti.preserveAspect != si.preserveAspect) ti.preserveAspect = si.preserveAspect;
            ti.fillCenter = si.fillCenter; ti.fillMethod = si.fillMethod; ti.fillOrigin = si.fillOrigin; ti.fillAmount = si.fillAmount;
            ti.pixelsPerUnitMultiplier = si.pixelsPerUnitMultiplier; ti.enabled = si.enabled;
        }
        var st = source.GetComponent<TMP_Text>(); var tt = target.GetComponent<TMP_Text>();
        if (st != null && tt != null)
        {
            if (tt.text != st.text) tt.text = st.text;
            if (tt.font != st.font) tt.font = st.font;
            if (!Mathf.Approximately(tt.fontSize, st.fontSize)) tt.fontSize = st.fontSize;
            if (tt.color != st.color) tt.color = st.color;
            if (tt.fontStyle != st.fontStyle) tt.fontStyle = st.fontStyle;
            if (tt.alignment != st.alignment) tt.alignment = st.alignment;
            tt.enableAutoSizing = st.enableAutoSizing; tt.fontSizeMin = st.fontSizeMin; tt.fontSizeMax = st.fontSizeMax;
            tt.textWrappingMode = st.textWrappingMode; tt.overflowMode = st.overflowMode; tt.margin = st.margin;
            tt.characterSpacing = st.characterSpacing; tt.wordSpacing = st.wordSpacing; tt.lineSpacing = st.lineSpacing;
            tt.paragraphSpacing = st.paragraphSpacing; tt.richText = st.richText; tt.extraPadding = st.extraPadding;
            tt.raycastTarget = st.raycastTarget; tt.enabled = st.enabled;
        }
        var sb = source.GetComponent<Button>(); var tb = target.GetComponent<Button>();
        if (sb != null && tb != null)
        {
            tb.colors = sb.colors; tb.transition = sb.transition; tb.interactable = sb.interactable;
            tb.navigation = sb.navigation;
            tb.targetGraphic = target.GetComponent<Graphic>();
        }
        for (int i = 0; i < source.childCount; i++) SyncUnityPreviewHierarchy(source.GetChild(i), target.GetChild(i));
    }

    void DestroyUnityPreviewInstance()
    {
        if (unityPreviewInstance != null) DestroyImmediate(unityPreviewInstance);
        unityPreviewInstance = null; unityPreviewCanvas = null;
        if (unityPreviewCanvasWrapper != null) DestroyImmediate(unityPreviewCanvasWrapper);
        unityPreviewCanvasWrapper = null;
    }

    void DestroyUnityPreview()
    {
        DestroyUnityPreviewInstance();
        if (unityPreviewCamera != null) DestroyImmediate(unityPreviewCamera.gameObject);
        unityPreviewCamera = null;
        if (unityPreviewTexture != null) { unityPreviewTexture.Release(); DestroyImmediate(unityPreviewTexture); }
        unityPreviewTexture = null;
        if (unityPreviewScene.IsValid() && unityPreviewScene.isLoaded) EditorSceneManager.ClosePreviewScene(unityPreviewScene);
        unityPreviewScene = default(Scene);
    }
}
#endif
