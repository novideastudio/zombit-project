#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auditoria de integracao do Editor Visual. Somente leitura: nao reescreve nem remove prefabs.
/// Verifica a disponibilidade de telas, dependencias, componentes e pontos de aplicacao em runtime.
/// </summary>
public sealed class ZombitUIProjectAudit : EditorWindow
{
    const string ScreensPath = "Assets/_Project/Resources/UI/Screens";
    const string ComponentsPath = "Assets/_Project/Resources/UI/Components";
    const string OverlaysPath = "Assets/_Project/Resources/UI/Overlays";
    const string ThemePath = "Assets/_Project/Resources/Config/UITheme.asset";
    const string DocsPath = "Assets/_Project/Documentacao";

    static readonly string[] DirectScreens = { "login", "nickname", "options", "credits", "slots" };
    static readonly string[] DynamicScreens =
    {
        "main", "select", "home", "map", "mapInventory", "mapSkills", "shop", "gameModes",
        "lastStandSetup", "bossRushSetup", "refugeDefenseSetup", "combatHud", "trainingRange",
        "refugeDefenseHud", "lastStandHud", "bossRushHud"
    };
    static readonly string[] RequiredComponents = { "Card_Survivor", "Cell_Equip", "Node_Location", "Row_slot", "Tile_Hub" };
    static readonly string[] RequiredOverlays = { "Overlay_confirm", "Overlay_pause" };

    readonly List<string> reportLines = new List<string>();
    Vector2 scroll;
    string summary = "Execute a auditoria para verificar a integracao da UI.";
    MessageType summaryType = MessageType.Info;
    int errors, warnings, passed;

    [MenuItem("ZomBit/Interface/Auditoria e Pré-build", priority = 2)]
    public static void Open()
    {
        var w = GetWindow<ZombitUIProjectAudit>("ZomBit UI - Auditoria");
        w.minSize = new Vector2(650, 560);
        w.Show();
    }

    // Atalho adicional no submenu UI, mais fácil de localizar no menu ZomBit.
    [MenuItem("ZomBit/UI/Auditoria e Pré-build", priority = 0)]
    public static void OpenFromUiMenu()
    {
        Open();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("AUDITORIA DE INTEGRAÇÃO DA UI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Auditoria somente de leitura. Não apaga, move nem sobrescreve prefabs. Verifica a estrutura e os pontos de ligação com o runtime.", MessageType.Info);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("EXECUTAR AUDITORIA", GUILayout.Height(32))) RunAudit();
        using (new EditorGUI.DisabledScope(reportLines.Count == 0))
        {
            if (GUILayout.Button("EXPORTAR RELATÓRIO", GUILayout.Height(32), GUILayout.Width(150))) ExportReport();
        }
        if (EditorApplication.isPlaying && GUILayout.Button("DIAGNOSTICAR PLAY MODE", GUILayout.Height(32), GUILayout.Width(190))) DiagnosePlayMode();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox(summary, summaryType);
        EditorGUILayout.BeginHorizontal();
        Stat("OK", passed, new Color(.25f, .72f, .4f));
        Stat("Avisos", warnings, new Color(.92f, .62f, .18f));
        Stat("Erros", errors, new Color(.85f, .25f, .22f));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in reportLines)
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndScrollView();
    }

    void Stat(string label, int value, Color color)
    {
        var old = GUI.color; GUI.color = color;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(120));
        EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(value.ToString(), EditorStyles.largeLabel);
        EditorGUILayout.EndVertical(); GUI.color = old;
    }

    void AddOk(string text) { passed++; reportLines.Add("[OK] " + text); }
    void AddWarning(string text) { warnings++; reportLines.Add("[AVISO] " + text); }
    void AddError(string text) { errors++; reportLines.Add("[ERRO] " + text); }

    void RunAudit()
    {
        reportLines.Clear(); errors = warnings = passed = 0;
        reportLines.Add("ZOMBIT SURVIVAL — AUDITORIA UI");
        reportLines.Add("Data: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        reportLines.Add("Unity configurado: 6000.6.7f1");
        reportLines.Add("");

        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        if (theme == null) AddError("UITheme.asset ausente em " + ThemePath);
        else if (theme.useEditorUI) AddOk("UI baseada em prefabs está ativada.");
        else AddWarning("UITheme.useEditorUI está desativado; o jogo usa builders antigos em runtime.");

        CheckPrefabGroup("Telas diretas", DirectScreens.Select(k => ScreensPath + "/Screen_" + k + ".prefab"));
        CheckPrefabGroup("Layouts dinâmicos", DynamicScreens.Select(k => ScreensPath + "/Screen_" + k + ".prefab"));
        CheckPrefabGroup("Componentes", RequiredComponents.Select(k => ComponentsPath + "/" + k + ".prefab"));
        CheckPrefabGroup("Overlays", RequiredOverlays.Select(k => OverlaysPath + "/" + k + ".prefab"));

        CheckRuntimeHook("MainMenuController", "Assets/_Project/Scripts/UI/MainMenuController.cs", "UILayoutPrefabApplier.Apply");
        CheckRuntimeHook("HUD da campanha", "Assets/_Project/Scripts/UI/HudController.cs", "UILayoutPrefabApplier.Apply");
        CheckRuntimeHook("Estande de tiro", "Assets/_Project/Scripts/Range/ShootingRange.cs", "UILayoutPrefabApplier.Apply");
        CheckRuntimeHook("Last Stand", "Assets/_Project/Scripts/Systems/LastStandController.cs", "UILayoutPrefabApplier.Apply");
        CheckRuntimeHook("Boss Rush", "Assets/_Project/Scripts/Systems/BossRushController.cs", "UILayoutPrefabApplier.Apply");
        CheckRuntimeHook("Defesa do Refúgio", "Assets/_Project/Scripts/Systems/RefugeDefenseController.cs", "UILayoutPrefabApplier.Apply");

        CheckReapplyFitsApi();
        CheckAuditMenuRegistration();
        CheckAudioEditorGuard();
        CheckArchiveIsolation();

        summary = "Auditoria concluída: " + passed + " verificações OK, " + warnings + " avisos e " + errors + " erros. Nenhum arquivo foi alterado.";
        summaryType = errors > 0 ? MessageType.Error : warnings > 0 ? MessageType.Warning : MessageType.Info;
        Debug.Log("[ZomBit UI Audit] " + summary);
        Repaint();
    }

    void CheckPrefabGroup(string title, IEnumerable<string> paths)
    {
        reportLines.Add("## " + title);
        foreach (string path in paths)
        {
            if (!File.Exists(path)) { AddWarning("Prefab ainda ausente: " + path); continue; }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { AddError("Asset existe, mas não pode ser carregado: " + path); continue; }
            var rootRect = prefab.GetComponent<RectTransform>();
            if (rootRect == null) AddWarning("Raiz não possui RectTransform: " + path);
            int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab);
            if (missingScripts > 0) AddError(path + " possui " + missingScripts + " componente(s) Missing Script.");
            else AddOk("Prefab carregável: " + Path.GetFileName(path));

            bool dynamic = path.Contains("/Screens/Screen_") && !DirectScreens.Any(k => path.EndsWith("Screen_" + k + ".prefab", StringComparison.OrdinalIgnoreCase));
            if (dynamic)
            {
                var flags = prefab.GetComponentsInChildren<UIPrefabLayoutOverride>(true);
                if (flags.Length == 0) AddWarning(Path.GetFileName(path) + " não possui UIPrefabLayoutOverride; alterações visuais podem não chegar ao runtime.");
            }

            string[] deps = AssetDatabase.GetDependencies(path, true);
            if (deps.Any(d => d.IndexOf("/Archive/LegacyUI/", StringComparison.OrdinalIgnoreCase) >= 0))
                AddError(Path.GetFileName(path) + " depende de um prefab arquivado em Archive/LegacyUI.");
            int nullSpriteImages = 0;
            foreach (var image in prefab.GetComponentsInChildren<Image>(true))
                if (image.enabled && image.sprite == null && image.color.a > 0.95f) nullSpriteImages++;
            if (nullSpriteImages > 0) AddWarning(Path.GetFileName(path) + " tem " + nullSpriteImages + " Image(s) visíveis sem sprite (pode ser intencional para cor sólida).");
        }
        reportLines.Add("");
    }

    void CheckRuntimeHook(string label, string path, string token)
    {
        if (!File.Exists(path)) { AddError("Script não encontrado: " + path); return; }
        string source = File.ReadAllText(path);
        if (source.Contains(token)) AddOk("Aplicação de layout conectada: " + label + ".");
        else AddWarning("Não foi encontrado o ponto de aplicação de layout: " + label + ".");
    }

    void CheckReapplyFitsApi()
    {
        var methods = typeof(UILayoutPrefabApplier).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Type[] required = { typeof(RectTransform), typeof(Transform), typeof(GameObject), typeof(Component) };
        foreach (Type argument in required)
        {
            bool found = methods.Any(m => m.Name == "ReapplyFits" &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == argument);
            if (found) AddOk("UILayoutPrefabApplier.ReapplyFits(" + argument.Name + ") disponível.");
            else AddError("Falta UILayoutPrefabApplier.ReapplyFits(" + argument.Name + "). Verifique UILayoutPrefabApplier.cs.");
        }
        bool noArguments = methods.Any(m => m.Name == "ReapplyFits" && m.GetParameters().Length == 0);
        if (noArguments) AddOk("UILayoutPrefabApplier.ReapplyFits() disponível.");
        else AddWarning("Sobrecarga ReapplyFits() sem parâmetros não encontrada.");
    }

    void CheckAuditMenuRegistration()
    {
        const string path = "Assets/_Project/Scripts/Editor/ZombitUIProjectAudit.cs";
        if (!File.Exists(path)) { AddError("Script da auditoria não encontrado."); return; }
        string source = File.ReadAllText(path);
        if (source.Contains("ZomBit/UI/Auditoria e Pré-build") && source.Contains("ZomBit/Interface/Auditoria e Pré-build"))
            AddOk("Auditoria registrada nos menus ZomBit/UI e ZomBit/Interface.");
        else AddWarning("Atalho direto da auditoria não está registrado nos dois menus esperados.");
    }

    void CheckAudioEditorGuard()
    {
        string path = "Assets/_Project/Scripts/Systems/ZombitAudio.cs";
        if (!File.Exists(path)) { AddWarning("ZombitAudio.cs não encontrado para validar o guard do Editor."); return; }
        string text = File.ReadAllText(path);
        if (text.Contains("if (!Application.isPlaying) return;") && text.Contains("DontDestroyOnLoad"))
            AddOk("ZombitAudio protege o DontDestroyOnLoad fora do Play Mode.");
        else AddWarning("Revise a proteção de DontDestroyOnLoad em ZombitAudio.");
    }

    void CheckArchiveIsolation()
    {
        string archive = "Assets/_Project/Archive/LegacyUI";
        if (!Directory.Exists(archive)) { AddWarning("Archive/LegacyUI não existe; nenhum arquivo legado foi encontrado nessa pasta."); return; }
        int count = Directory.GetFiles(archive, "*.prefab", SearchOption.AllDirectories).Length;
        if (count > 0) AddOk(count + " prefab(s) legado(s) preservados fora de Resources.");
        else AddWarning("Archive/LegacyUI existe, mas não contém prefabs.");
    }

    void DiagnosePlayMode()
    {
        if (!EditorApplication.isPlaying) { AddWarning("Entre em Play Mode antes de executar o diagnóstico runtime."); return; }
        reportLines.Add(""); reportLines.Add("## DIAGNÓSTICO PLAY MODE");
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int totalButtons = 0, totalImages = 0, totalTexts = 0, activeCanvases = 0;
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            if (canvas.isActiveAndEnabled) activeCanvases++;
            totalButtons += canvas.GetComponentsInChildren<Button>(true).Length;
            totalImages += canvas.GetComponentsInChildren<Image>(true).Length;
            totalTexts += canvas.GetComponentsInChildren<TMP_Text>(true).Length;
            reportLines.Add("Canvas: " + canvas.name + " | ativo=" + canvas.isActiveAndEnabled + " | render=" + canvas.renderMode);
        }
        var controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        var hud = UnityEngine.Object.FindFirstObjectByType<HudController>();
        reportLines.Add("MainMenuController: " + (controller != null ? "presente" : "ausente"));
        reportLines.Add("HudController: " + (hud != null ? "presente" : "ausente"));
        reportLines.Add("Canvas ativos: " + activeCanvases + "/" + canvases.Length);
        reportLines.Add("UI total: " + totalButtons + " botões, " + totalImages + " imagens, " + totalTexts + " textos TMP.");
        Debug.Log("[ZomBit UI Runtime] Canvases=" + canvases.Length + ", ativos=" + activeCanvases + ", Buttons=" + totalButtons + ", Images=" + totalImages + ", TMP=" + totalTexts);
        summary = "Diagnóstico Play Mode registrado. Veja a seção final desta janela e o Console.";
        summaryType = MessageType.Info;
        Repaint();
    }

    void ExportReport()
    {
        Directory.CreateDirectory(DocsPath);
        string path = DocsPath + "/AUDITORIA_UI_E_PRE_BUILD_PT_BR.md";
        var sb = new StringBuilder();
        foreach (string line in reportLines) sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("## Resumo");
        sb.AppendLine("OK: " + passed + " | Avisos: " + warnings + " | Erros: " + errors);
        sb.AppendLine("Auditoria estática/editorial. Não substitui compilação, Play Mode ou builds.");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (asset != null) { Selection.activeObject = asset; EditorGUIUtility.PingObject(asset); }
        Debug.Log("[ZomBit UI Audit] Relatório exportado: " + path);
    }
}
#endif
