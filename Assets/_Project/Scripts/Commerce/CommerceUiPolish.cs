using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Presentation-only pass. Does not delete navigation actions or change item attributes.
public sealed class CommerceUiPolish : MonoBehaviour
{
    float nextRefresh;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){SceneManager.sceneLoaded-=Loaded;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(UnityEngine.Object.FindFirstObjectByType<CommerceUiPolish>()==null)new GameObject("CommerceUiPolish").AddComponent<CommerceUiPolish>();
    }
    static void Shorten(Button button)
    {
        var label=button.GetComponentInChildren<TMP_Text>(true);if(label==null)return;
        if(label.text.TrimStart().StartsWith("MELHORAR",StringComparison.OrdinalIgnoreCase))
        {label.text="REFINAR";label.enableAutoSizing=true;label.fontSizeMin=11;label.fontSizeMax=22;label.textWrappingMode=TextWrappingModes.NoWrap;}
    }
    public static void ShortenRefineButtons(Transform root)
    {if(root!=null)foreach(var button in root.GetComponentsInChildren<Button>(true))Shorten(button);}
    void LateUpdate()
    {
        if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.3f;
        foreach(var button in FindObjectsByType<Button>(FindObjectsSortMode.None))Shorten(button);
        var save=GameSession.Save;if(save==null)return;
        var lookup=new System.Collections.Generic.Dictionary<string,ItemInstance>();
        if(save.inventory!=null)foreach(var it in save.inventory)if(it!=null && !string.IsNullOrEmpty(it.uid))lookup[it.uid]=it;
        if(save.progress!=null)foreach(var p in save.progress)if(p!=null && p.equipped!=null)foreach(var it in p.equipped)if(it!=null && !string.IsNullOrEmpty(it.uid))lookup[it.uid]=it;
        foreach(var rt in FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
        {
            if(rt.name.StartsWith("Item_",StringComparison.Ordinal) && lookup.TryGetValue(rt.name.Substring(5),out var item))ItemTileStyling.Apply(rt,item);
        }
    }
}
