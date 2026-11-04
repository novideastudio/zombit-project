#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Zombit > Interface > Bake tela ativa (Play Mode) -> Screen_<chave>.prefab
// Uso: entre em Play, abra a tela no jogo, selecione a raiz da tela na Hierarchy
// (o RectTransform que o codigo cria para a tela) e rode o menu. Informe a chave (map, home, shop...).
public static class ZombitPrefabRebake
{
    const string OutDir = "Assets/_Project/Resources/UI/Screens/";
    static readonly string[] Keys = { "bossRushHud","bossRushSetup","combatHud","credits","gameModes","home","lastStandHud","lastStandSetup","login","main","map","mapInventory","mapSkills","nickname","options","refugeDefenseHud","refugeDefenseSetup","select","shop","slots","trainingRange" };

    [MenuItem("Zombit/Interface/Bake tela selecionada (Play Mode)")]
    static void BakeSelected()
    {
        if (!Application.isPlaying) { EditorUtility.DisplayDialog("Zombit", "Entre em Play Mode e abra a tela primeiro.", "OK"); return; }
        var go = Selection.activeGameObject;
        if (go == null || !(go.transform is RectTransform)) { EditorUtility.DisplayDialog("Zombit", "Selecione a raiz da tela na Hierarchy.", "OK"); return; }
        string key = KeyWindow.Ask(go.name);
        if (string.IsNullOrEmpty(key)) return;
        Bake(go, key);
    }

    [MenuItem("Zombit/Interface/Lista de chaves de tela")]
    static void ListKeys() { Debug.Log("Chaves: " + string.Join(", ", Keys)); }

    public static void Bake(GameObject live, string key)
    {
        var wasActive = live.activeSelf;
        var copy = Object.Instantiate(live, live.transform.parent, false);
        copy.name = "Screen_" + key;
        var report = new StringBuilder();
        int sprites = 0, missingSprites = 0, nodes = 0;
        foreach (var t in copy.GetComponentsInChildren<Transform>(true))
        {
            nodes++;
            foreach (var mb in t.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                var ns = mb.GetType().Namespace ?? "";
                bool keep = ns.StartsWith("UnityEngine.UI") || ns.StartsWith("TMPro") || mb is UIPrefabLayoutOverride;
                if (!keep) Object.DestroyImmediate(mb);
            }
        }
        foreach (var t in copy.GetComponentsInChildren<Transform>(true))
        {
            var f = t.GetComponent<UIPrefabLayoutOverride>();
            if (f == null) f = t.gameObject.AddComponent<UIPrefabLayoutOverride>();
            f.overrideAnchorsAndPosition = f.overrideSize = f.overridePivot = f.overrideScaleAndRotation = true;
            f.overrideActiveState = true; f.overrideSiblingOrder = true; f.overrideLayoutComponents = true;
            var img = t.GetComponent<Image>();
            if (img != null)
            {
                f.overrideImageSprite = true; f.overrideImageColor = true;
                f.bakedImageSprite = img.sprite; f.bakedImageColor = img.color;
                if (img.sprite != null)
                {
                    sprites++;
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(img.sprite))) { missingSprites++; report.AppendLine("Sprite sem asset (gerado em runtime): " + Path(t)); }
                }
            }
            var tx = t.GetComponent<TMP_Text>();
            if (tx != null)
            {
                f.overrideTextContent = f.overrideTextColor = f.overrideTextStyle = true;
                f.bakedTextContent = tx.text; f.bakedTextColor = tx.color;
            }
            var bt = t.GetComponent<Button>();
            if (bt != null) { f.overrideButtonColors = f.overrideButtonTransition = true; f.bakedButtonColors = bt.colors; }
        }
        var dupes = new Dictionary<string, int>();
        foreach (var t in copy.GetComponentsInChildren<Transform>(true))
        {
            var p = Path(t);
            dupes[p] = dupes.ContainsKey(p) ? dupes[p] + 1 : 1;
        }
        foreach (var kv in dupes) if (kv.Value > 1) report.AppendLine("Nome duplicado (ordem importa): " + kv.Key + " x" + kv.Value);

        System.IO.Directory.CreateDirectory(OutDir);
        string path = OutDir + "Screen_" + key + ".prefab";
        if (System.IO.File.Exists(path)) System.IO.File.Copy(path, path + ".bak", true);
        var prefab = PrefabUtility.SaveAsPrefabAsset(copy, path);
        Object.DestroyImmediate(copy);
        live.SetActive(wasActive);
        AssetDatabase.Refresh();
        Debug.Log("[ZomBit Rebake] " + path + " | nos: " + nodes + " | sprites: " + sprites + " (sem asset: " + missingSprites + ")\n" + report);
        EditorGUIUtility.PingObject(prefab);
    }

    static string Path(Transform t)
    {
        var s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    class KeyWindow : EditorWindow
    {
        string value; System.Action<string> cb; static string result;
        public static string Ask(string def)
        {
            result = null;
            var w = CreateInstance<KeyWindow>();
            w.value = def.Replace("Screen_", "");
            w.titleContent = new GUIContent("Chave da tela");
            w.ShowModalUtility();
            return result;
        }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("Chave (ex.: map, home, shop, select)", MessageType.Info);
            value = EditorGUILayout.TextField(value);
            if (GUILayout.Button("Bake")) { result = value; Close(); }
        }
    }
}
#endif
