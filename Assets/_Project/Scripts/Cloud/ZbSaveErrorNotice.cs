using UnityEngine;

public sealed class ZbSaveErrorNotice : MonoBehaviour
{
    static string message;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        message = null;
        ZombitCloud.SaveRejected -= Show;
        ZombitCloud.SaveRejected += Show;
        var go = new GameObject("ZbSaveErrorNotice");
        DontDestroyOnLoad(go);
        go.AddComponent<ZbSaveErrorNotice>();
    }
    static void Show(string text) { message = text; }
    void OnGUI()
    {
        if (string.IsNullOrEmpty(message)) return;
        float width = Mathf.Min(520f, Screen.width - 24f);
        GUILayout.BeginArea(new Rect((Screen.width-width)/2f,24f,width,200f),GUI.skin.box);
        GUILayout.Label("Problema ao salvar");
        var style = new GUIStyle(GUI.skin.label) { wordWrap = true };
        GUILayout.Label(message,style);
        GUILayout.Label("O progresso recusado sera substituido pelo ultimo save do servidor. Se a recuperacao falhar, use Tentar novamente na tela de conta.",style);
        if (GUILayout.Button("Entendi")) message = null;
        GUILayout.EndArea();
    }
}