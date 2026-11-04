#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assistente seguro para concluir a migração de UI do Zombit.
/// Só cria telas ausentes e adiciona metadados/Canvas de preview sem reconstruir prefabs existentes.
/// HUDs de gameplay que só existem durante o jogo continuam sendo capturados em Play Mode.
/// </summary>
public sealed class ZombitUIFinalizationWizard : EditorWindow
{
    const string ScreensPath = "Assets/_Project/Resources/UI/Screens";
    const string ComponentsPath = "Assets/_Project/Resources/UI/Components";
    const string OverlaysPath = "Assets/_Project/Resources/UI/Overlays";
    const string ThemePath = "Assets/_Project/Resources/Config/UITheme.asset";
    const string DocsPath = "Assets/_Project/Documentacao";

    static readonly string[] DirectScreens = { "login", "nickname", "options", "credits", "slots" };
    static readonly string[] DynamicLayouts =
    {
        "main", "select", "home", "map", "mapInventory", "mapSkills", "shop", "gameModes",
        "lastStandSetup", "bossRushSetup", "refugeDefenseSetup", "combatHud", "trainingRange",
        "refugeDefenseHud", "lastStandHud", "bossRushHud"
    };
    static readonly string[] Components = { "Card_Survivor", "Cell_Equip", "Node_Location", "Row_slot", "Tile_Hub" };
    static readonly string[] Overlays = { "Overlay_confirm", "Overlay_pause" };

    Vector2 scroll;
    string status = "Pronto para diagnosticar a interface.";
    MessageType statusType = MessageType.Info;

    [MenuItem("ZomBit/Interface/Assistente de finalização (seguro)", priority = 1)]
    public static void Open()
    {
        var window = GetWindow<ZombitUIFinalizationWizard>("ZomBit UI - Finalização");
        window.minSize = new Vector2(570, 560);
        window.Show();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("FINALIZAÇÃO DO EDITOR VISUAL", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Este assistente prepara prefabs existentes sem sobrescrevê-los, gera apenas telas ausentes, corrige o Canvas de prévia e mostra o que ainda precisa ser capturado durante o jogo. A auditoria também está disponível em ZomBit > UI > Auditoria e Pré-build.",
            MessageType.Info);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawSection("TELAS COMPLETAS (carregadas diretamente)", DirectScreens, "Screen_", true);
        DrawSection("LAYOUTS DINÂMICOS (visual sobre UI gerada pelo jogo)", DynamicLayouts, "Screen_", false);
        DrawSection("COMPONENTES REUTILIZÁVEIS", Components, "", false, ComponentsPath);
        DrawSection("SOBREPOSIÇÕES", Overlays, "", false, OverlaysPath);
        EditorGUILayout.EndScrollView();

        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        EditorGUILayout.Space(4);
        if (theme == null)
            EditorGUILayout.HelpBox("UITheme.asset não foi encontrado. O jogo continuará usando os valores padrão até o asset ser restaurado.", MessageType.Warning);
        else
            EditorGUILayout.HelpBox("UI editável no runtime: " + (theme.useEditorUI ? "ATIVADA" : "DESATIVADA") +
                ". Não altere essa opção até concluir a geração e conferir as telas principais.", theme.useEditorUI ? MessageType.Info : MessageType.Warning);

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            if (GUILayout.Button("EXECUTAR PREPARAÇÃO SEGURA COMPLETA", GUILayout.Height(34)))
                RunSafePreparation();
        }
        if (GUILayout.Button("CAPTURAR INTERFACES ABERTAS (somente em Play Mode)", GUILayout.Height(28)))
            UIBaker.CaptureOpenInterfacesSafe();
        if (GUILayout.Button("ABRIR AUDITORIA E PRÉ-BUILD", GUILayout.Height(26)))
            ZombitUIProjectAudit.Open();
        if (GUILayout.Button("EXPORTAR DIAGNÓSTICO E CHECKLIST", GUILayout.Height(26)))
            ExportDiagnostic();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(status, statusType);
        EditorGUILayout.LabelField("Importante: a prévia visual, os callbacks e o comportamento final devem ser testados no Unity Editor antes de gerar uma build.", EditorStyles.wordWrappedMiniLabel);
    }

    void DrawSection(string title, string[] keys, string prefix, bool direct, string customPath = null)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        int exists = 0;
        foreach (string key in keys)
        {
            string path = customPath != null
                ? customPath + "/" + key + ".prefab"
                : ScreensPath + "/" + prefix + key + ".prefab";
            bool found = File.Exists(path);
            if (found) exists++;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(found ? "●" : "○", GUILayout.Width(18));
                GUILayout.Label(key, GUILayout.MinWidth(160));
                GUILayout.FlexibleSpace();
                GUILayout.Label(found ? "Encontrado" : (direct ? "Falta gerar" : "Gerar ou capturar"), EditorStyles.miniLabel, GUILayout.Width(105));
                if (found && GUILayout.Button("Selecionar", GUILayout.Width(72)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
        }
        EditorGUILayout.LabelField(exists + " de " + keys.Length + " encontrados", EditorStyles.miniLabel);
    }

    void RunSafePreparation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            status = "Saia do Play Mode antes de preparar ou gerar prefabs.";
            statusType = MessageType.Warning;
            return;
        }

        if (!EditorUtility.DisplayDialog("Preparação segura da UI",
            "A sequência irá:\n\n" +
            "1. Adicionar metadados ausentes aos prefabs migrados.\n" +
            "2. Gerar apenas telas/componentes que ainda não existem.\n" +
            "3. Corrigir Canvas de prévia sem reconstruir hierarquias.\n" +
            "4. Executar auditoria.\n\n" +
            "Prefabs existentes NÃO serão sobrescritos. Continuar?",
            "Executar", "Cancelar")) return;

        try
        {
            EditorUtility.DisplayProgressBar("ZomBit UI", "Executando sequência segura de preparação...", 0.25f);
            UIBaker.RunSafePreparationFromWizard();
            EditorUtility.DisplayProgressBar("ZomBit UI", "Atualizando inventário...", 0.9f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            status = "Preparação concluída. Atualize a janela para ver os arquivos gerados. Os cinco HUDs de gameplay precisam ser capturados abrindo cada modo em Play Mode.";
            statusType = MessageType.Info;
            Repaint();
        }
        catch (Exception ex)
        {
            status = "A preparação foi interrompida: " + ex.GetBaseException().Message + ". Veja o Console para o stack trace.";
            statusType = MessageType.Error;
            Debug.LogException(ex);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    void ExportDiagnostic()
    {
        Directory.CreateDirectory(DocsPath);
        string path = DocsPath + "/DIAGNOSTICO_UI_E_CHECKLIST_FINAL_PT_BR.md";
        var sb = new StringBuilder();
        sb.AppendLine("# Diagnóstico da UI — ZomBit Survival");
        sb.AppendLine();
        sb.AppendLine("Gerado em: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("Unity do projeto: 6000.6.7f1");
        sb.AppendLine();
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
        sb.AppendLine("UI de prefabs ativada: " + (theme != null && theme.useEditorUI ? "Sim" : "Não / UITheme ausente"));
        sb.AppendLine();
        AppendAssetSection(sb, "Telas completas", DirectScreens, ScreensPath, "Screen_");
        AppendAssetSection(sb, "Layouts dinâmicos", DynamicLayouts, ScreensPath, "Screen_");
        AppendAssetSection(sb, "Componentes", Components, ComponentsPath, "");
        AppendAssetSection(sb, "Overlays", Overlays, OverlaysPath, "");
        sb.AppendLine("## HUDs que exigem captura durante o jogo");
        foreach (string key in new[] { "combatHud", "trainingRange", "refugeDefenseHud", "lastStandHud", "bossRushHud" })
        {
            bool exists = File.Exists(ScreensPath + "/Screen_" + key + ".prefab");
            sb.AppendLine("- [" + (exists ? "x" : " ") + "] Screen_" + key + (exists ? " — encontrado" : " — abra esse modo em Play Mode e use Capturar interfaces abertas"));
        }
        sb.AppendLine();
        sb.AppendLine("## Roteiro de teste no Unity");
        sb.AppendLine("1. Abra MainMenu.unity e confirme que o menu principal aparece sem erros.");
        sb.AppendLine("2. Teste login/local, nickname, selecionar sobrevivente, opções, créditos e slots.");
        sb.AppendLine("3. Abra Home, Mapa, Inventário, Habilidades, Loja e Modos de Jogo.");
        sb.AppendLine("4. Entre em Campanha, Estande de Tiro, Last Stand, Boss Rush e Defesa do Refúgio.");
        sb.AppendLine("5. Em cada modo, teste pausa, retorno, HUD, vida, munição, ondas e resultados.");
        sb.AppendLine("6. Teste Continue, três slots, salvar, carregar e excluir save.");
        sb.AppendLine("7. Teste música, SFX, volumes, tela cheia e redimensionamento em 1920x1080 e 1280x720.");
        sb.AppendLine("8. Edite um botão no Editor Visual, salve, entre em Play Mode e confirme que o layout mudou.");
        sb.AppendLine("9. Somente após os testes, gere builds Windows, Android e WebGL.");
        sb.AppendLine();
        sb.AppendLine("Observação: esta ferramenta audita a estrutura de assets no Editor; não substitui testes de Play Mode ou build.");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
        status = "Diagnóstico exportado: " + path;
        statusType = MessageType.Info;
    }

    static void AppendAssetSection(StringBuilder sb, string title, string[] keys, string folder, string prefix)
    {
        sb.AppendLine("## " + title);
        foreach (string key in keys)
        {
            string path = folder + "/" + prefix + key + ".prefab";
            bool exists = File.Exists(path);
            sb.AppendLine("- [" + (exists ? "x" : " ") + "] " + prefix + key + " — " + (exists ? path : "ausente"));
        }
        sb.AppendLine();
    }
}
#endif
