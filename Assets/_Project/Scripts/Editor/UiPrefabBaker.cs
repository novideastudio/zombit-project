#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Conversor universal de UI: gera prefabs uGUI editaveis a partir das telas
/// montadas por codigo. Os prefabs ficam em Assets/Resources/UI/Screens e sao
/// aplicados pelo UILayoutPrefabApplier quando useEditorUI esta ativo.
/// </summary>
public static class UiPrefabBaker
{
    const string OutputFolder = "Assets/Resources/UI/Screens";

    [MenuItem("ZomBit/UI/0 - Ativar edicao visual (useEditorUI)")]
    public static void EnableEditorUi()
    {
        var themes = Resources.FindObjectsOfTypeAll<UITheme>();
        if (themes == null || themes.Length == 0)
        {
            Debug.LogError("[ZomBit UI] Nenhum asset UITheme encontrado.");
            return;
        }

        int changed = 0;
        foreach (var theme in themes)
        {
            if (theme == null) continue;
            var so = new SerializedObject(theme);
            var prop = so.FindProperty("useEditorUI");
            if (prop == null)
            {
                Debug.LogWarning("[ZomBit UI] UITheme sem campo useEditorUI: " + theme.name);
                continue;
            }
            if (!prop.boolValue) { prop.boolValue = true; changed++; }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(theme);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ZomBit UI] Edicao visual ativada em " + changed + " asset(s).");
    }

    [MenuItem("ZomBit/UI/1 - Gerar prefabs editaveis de todas as telas")]
    public static void BakeAllScreens()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("ZomBit UI",
                "Entre em Play Mode na cena MainMenu antes de gerar os prefabs.",
                "OK");
            return;
        }

        var controller = Object.FindFirstObjectByType<MainMenuController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("ZomBit UI", "MainMenuController nao encontrado.", "OK");
            return;
        }

        var field = typeof(MainMenuController).GetField("screens", BindingFlags.NonPublic | BindingFlags.Instance);
        var screens = field != null ? field.GetValue(controller) as Dictionary<string, RectTransform> : null;

        if (screens == null || screens.Count == 0)
        {
            EditorUtility.DisplayDialog("ZomBit UI", "Nenhuma tela registrada.", "OK");
            return;
        }

        if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

        int created = 0, updated = 0, skipped = 0;
        var skippedNames = new List<string>();

        foreach (var kv in screens)
        {
            string key = kv.Key;
            var screen = kv.Value;
            if (string.IsNullOrEmpty(key) || screen == null) { skipped++; continue; }

            if (key == "mapSkills") { skipped++; skippedNames.Add(key); continue; }

            string path = OutputFolder + "/Layout_" + key + ".prefab";
            var clone = Object.Instantiate(screen.gameObject);
            clone.name = key;
            TrimDynamicChildren(clone.transform);

            try
            {
                if (File.Exists(path)) updated++; else created++;
                PrefabUtility.SaveAsPrefabAsset(clone, path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ZomBit UI] Falha ao salvar " + path + ": " + e.Message);
                skipped++; skippedNames.Add(key);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ZomBit UI] Conversao concluida. Novos: " + created + ". Atualizados: " + updated + ". Ignorados: " + skipped + ".");
        if (skippedNames.Count > 0)
            Debug.Log("[ZomBit UI] Ignoradas: " + string.Join(", ", skippedNames));

        EditorUtility.DisplayDialog("ZomBit UI",
            "Prefabs gerados em:\n" + OutputFolder +
            "\n\nNovos: " + created + "\nAtualizados: " + updated + "\nIgnorados: " + skipped,
            "OK");
    }

    [MenuItem("ZomBit/UI/2 - Abrir pasta dos prefabs")]
    public static void OpenFolder()
    {
        if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);
        EditorUtility.RevealInFinder(Path.GetFullPath(OutputFolder));
    }

    [MenuItem("ZomBit/UI/3 - Apagar todos os prefabs gerados")]
    public static void DeleteGenerated()
    {
        if (!EditorUtility.DisplayDialog("ZomBit UI",
            "Apagar todos os Layout_*.prefab gerados? As telas voltarao ao visual original.",
            "Apagar", "Cancelar")) return;

        if (!Directory.Exists(OutputFolder)) return;

        foreach (var file in Directory.GetFiles(OutputFolder, "Layout_*.prefab"))
        {
            AssetDatabase.DeleteAsset(file);
        }
        AssetDatabase.Refresh();
        Debug.Log("[ZomBit UI] Prefabs gerados apagados.");
    }

    static void TrimDynamicChildren(Transform root)
    {
        string[] removePrefixes = { "Item_", "Card_", "Node_", "Enemy_", "EquipSlot_", "Slot_", "Entry_", "Row_" };
        string[] removeNames = { "Content", "List", "Grid", "Items", "Cards", "Slots" };

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            string n = child.name;
            bool remove = false;

            foreach (var p in removePrefixes)
                if (n.StartsWith(p)) { remove = true; break; }

            if (!remove)
                foreach (var rn in removeNames)
                    if (n == rn) { remove = true; break; }

            if (remove) { Object.DestroyImmediate(child.gameObject); continue; }
            TrimDynamicChildren(child);
        }
    }
}
#endif
