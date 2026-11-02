using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reapplies the same metal HUD frame in every combat scene, including rebuilt squad cards.
// Does not modify scenery, character grading, saves, stats or shadow settings.
public sealed class ZomBitUniformCombatHud : MonoBehaviour
{
    const string FrameName="PcHudFrame";
    static Sprite sharedFrame;
    static float nextFrameAttempt;
    static bool warned;
    float nextRefresh;
    HudController hud;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        sharedFrame=null;nextFrameAttempt=0;warned=false;
        SceneManager.sceneLoaded-=Loaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.buildIndex!=GameSession.GameSceneIndex)return;
        if(Object.FindFirstObjectByType<ZomBitUniformCombatHud>()==null)
            new GameObject("UniformCombatHud").AddComponent<ZomBitUniformCombatHud>();
    }
    static Sprite Frame()
    {
        if(sharedFrame!=null)return sharedFrame;
        if(Time.unscaledTime<nextFrameAttempt)return null;
        nextFrameAttempt=Time.unscaledTime+2;
        var texture=Resources.Load<Texture2D>("Art/PCHudUI/hud_frame");
        if(texture==null)
        {
            if(!warned){warned=true;Debug.LogWarning("Uniform HUD: falta Resources/Art/PCHudUI/hud_frame. Preservando visual atual; não substitui por uma arte diferente.");}
            return null;
        }
        sharedFrame=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(18,18,18,18));
        return sharedFrame;
    }
    void LateUpdate()
    {
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.25f;
        if(GameSession.IsRange)return;
        if(hud==null)hud=FindFirstObjectByType<HudController>();
        if(hud==null || Frame()==null)return;
        Ensure(hud.transform.Find("HudPlate_Wave") as RectTransform);
        Ensure(hud.transform.Find("HudPlate_Backpack") as RectTransform);
        Ensure(hud.transform.Find("HudPlate_BackToMap") as RectTransform);
        for(int i=0;i<3;i++)Ensure(hud.transform.Find("HudSquadCard_"+i) as RectTransform);
        var posture=GameObject.Find("CombatPostureToggle");
        if(posture!=null)Ensure(posture.transform as RectTransform);
    }
    static void Ensure(RectTransform target)
    {
        if(target==null || !target.gameObject.activeInHierarchy)return;
        var child=target.Find(FrameName) as RectTransform;
        if(child==null)
        {
            child=new GameObject(FrameName,typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(target,false);
        }
        child.anchorMin=Vector2.zero;child.anchorMax=Vector2.one;
        child.offsetMin=new Vector2(-2,-2);child.offsetMax=new Vector2(2,2);
        child.localScale=Vector3.one;child.localRotation=Quaternion.identity;
        var image=child.GetComponent<Image>();if(image==null)image=child.gameObject.AddComponent<Image>();
        image.sprite=sharedFrame;image.type=Image.Type.Sliced;image.color=Color.white;
        image.raycastTarget=false;image.enabled=true;
        child.gameObject.SetActive(true);
        if(child.GetSiblingIndex()!=target.childCount-1)child.SetAsLastSibling();
    }
}
