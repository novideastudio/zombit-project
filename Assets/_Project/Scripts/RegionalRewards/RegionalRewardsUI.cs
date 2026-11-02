using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Visual integration only. Reward logic and save schema remain in RegionalRewards.
public sealed class RegionalRewardsUI : MonoBehaviour
{
    public static RegionalRewardsUI Instance;
    Canvas canvas;RectTransform screen,stage;Action closed;
    SaveData boundSave;string boundUid;int index;string message="";
    static Sprite panelSprite,redSprite;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){SceneManager.sceneLoaded-=Loaded;Instance=null;panelSprite=null;redSprite=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.buildIndex==GameSession.MenuSceneIndex && Instance==null)
            new GameObject("RegionalRewardsUI").AddComponent<RegionalRewardsUI>();
        // Preserve effect/cosmetic setup from the installed gameplay package.
        if(scene.buildIndex==GameSession.GameSceneIndex && RegionalRewards.Installed && !GameSession.DebugRun && !GameSession.IsRange)
        {
            RegionalEffects.ResetRun();
            foreach(var m in HeroSquad.Members)
                if(m!=null && m.stats!=null && m.stats.GetComponent<RegionalItemEffects>()==null)m.stats.gameObject.AddComponent<RegionalItemEffects>();
            if(UnityEngine.Object.FindFirstObjectByType<RegionalCosmeticsHUD>()==null)
                new GameObject("RegionalCosmeticsHUD").AddComponent<RegionalCosmeticsHUD>();
        }
    }
    void Awake()
    {
        if(Instance!=null && Instance!=this){Destroy(gameObject);return;}
        Instance=this;
        canvas=UIFactory.MakeCanvas("RegionalWorkshopCanvas",600);canvas.gameObject.SetActive(false);
        // No Home button is created here.
    }
    public static void OpenForRegion(string id,Action onClosed=null)
    {
        if(SceneManager.GetActiveScene().buildIndex!=GameSession.MenuSceneIndex)return;
        if(Instance==null)new GameObject("RegionalRewardsUI").AddComponent<RegionalRewardsUI>();
        Instance.Open(id,onClosed);
    }
    void Open(string id,Action callback)
    {
        if(screen!=null)return;
        closed=callback;boundSave=GameSession.Save;boundUid=ZombitCloud.Uid;
        index=0;
        var regions=RegionalRewards.Config.regions;
        if(regions!=null)for(int i=0;i<regions.Length;i++)if(regions[i]!=null && regions[i].id==id){index=i;break;}
        message="";canvas.gameObject.SetActive(true);Draw();
    }
    static Sprite LoadFrame(string name)
    {
        if(name=="red" && redSprite!=null)return redSprite;
        if(name=="panel" && panelSprite!=null)return panelSprite;
        var tex=Resources.Load<Texture2D>("Art/PCInventoryUI/"+name);if(tex==null)return null;
        var sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        if(name=="red")redSprite=sp;else panelSprite=sp;return sp;
    }
    RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var rt=UIFactory.Node(name,parent);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;
    }
    RectTransform Panel(string name,float x,float y,float w,float h)
    {
        var p=Rect(stage,name,x,y,w,h);var im=p.gameObject.AddComponent<Image>();
        im.sprite=LoadFrame("panel");im.type=Image.Type.Sliced;im.color=im.sprite!=null?Color.white:new Color(.035f,.06f,.08f,1);im.raycastTarget=false;
        return p;
    }
    TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size=23,Color? color=null)
    {
        var rt=Rect(parent,name,x,y,w,h);var t=UIFactory.Txt(rt,value,size,TextAlignmentOptions.TopLeft,color??Color.white,"Label");
        UIFactory.Stretch(t.rectTransform);t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;
        t.enableAutoSizing=true;t.fontSizeMin=12;t.fontSizeMax=size;return t;
    }
    Button Button(Transform parent,string name,string label,float x,float y,float w,float h,Action action,bool primary=false)
    {
        var rt=Rect(parent,name,x,y,w,h);var b=UIFactory.Btn(rt,label,22,action,null,name+"Button");
        UIFactory.Stretch((RectTransform)b.transform);
        var im=b.GetComponent<Image>();var sp=LoadFrame(primary?"red":"panel");if(im!=null && sp!=null){im.sprite=sp;im.type=Image.Type.Sliced;}
        var cb=b.colors;cb.normalColor=Color.white;cb.highlightedColor=new Color(1,.91f,.65f);cb.pressedColor=new Color(.7f,.7f,.7f);b.colors=cb;
        var t=b.GetComponentInChildren<TMP_Text>();if(t){t.enableAutoSizing=true;t.fontSizeMin=11;t.fontSizeMax=22;}return b;
    }
    void Draw()
    {
        if(screen!=null){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}
        screen=UIFactory.Node("WorkshopOverlay",canvas.transform);UIFactory.Stretch(screen);
        var dim=screen.gameObject.AddComponent<Image>();dim.color=new Color(0,0,0,.88f);dim.raycastTarget=true;
        stage=UIFactory.Node("WorkshopStage",screen);stage.sizeDelta=new Vector2(1520,980);
        stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(.5f,.5f);
        var fit=stage.gameObject.AddComponent<RegionalWorkshopVisualFit>();fit.stage=stage;fit.viewport=screen;fit.Apply();
        Panel("Header",0,0,1520,92);
        Text(stage,"Title","OFICINA REGIONAL",30,18,860,52,36,UIFactory.Gold);
        Button(stage,"Close","VOLTAR",1260,16,236,60,Close,true);
        if(!RegionalRewards.Installed || boundSave==null)
        {Text(stage,"Missing","Carregue seu jogo e instale o pacote de recompensas antes de acessar a oficina.",40,160,1440,200,28);return;}
        try
        {
            var regions=RegionalRewards.Config.regions;
            if(regions==null || regions.Length==0){Text(stage,"Missing","Nenhuma região configurada.",30,140,1400,100);return;}
            index=Mathf.Clamp(index,0,regions.Length-1);var r=regions[index];
            var state=RegionalRewards.Read(boundSave);var rec=RegionalRewards.Record(state,r.id);
            var list=Panel("RegionList",0,110,350,778);
            Text(list,"ListTitle","REGIÕES",24,16,300,40,26,UIFactory.Gold);
            for(int i=0;i<regions.Length;i++)
            {
                int choice=i;var region=regions[i];if(region==null)continue;
                Button(list,"Region_"+region.id,(i+1).ToString("00")+"  "+region.name,16,70+i*75,318,62,()=>{index=choice;message="";Draw();},i==index);
            }
            var detail=Panel("RecipeDetails",370,110,1150,778);
            Text(detail,"Region",(index+1).ToString("00")+" · "+r.name.ToUpperInvariant(),26,18,1090,42,28,UIFactory.Gold);
            Text(detail,"ItemName",r.itemName+"  ·  ÉPICO",230,88,880,56,32);
            var iconRoot=Rect(detail,"Icon",38,92,164,164);var icon=iconRoot.gameObject.AddComponent<Image>();
            icon.sprite=r.icon!=null?r.icon:ItemVisual.IconForSlot(r.slot);icon.preserveAspect=true;icon.raycastTarget=false;
            Text(detail,"Effect",r.description,230,158,880,118,25);
            Text(detail,"Blueprint","PROJETO: "+(rec.blueprint?"DESBLOQUEADO":"VENÇA NO NORMAL OU ACIMA"),30,294,1080,40,24,rec.blueprint?new Color(.4f,1,.5f):UIFactory.Gold);
            Text(detail,"Seals","SELOS DA REGIÃO   "+rec.seals+" / "+r.sealsCost,30,346,1080,40,25);
            var bar=UIKit.Bar(detail,UIFactory.Gold,Mathf.Clamp01((float)rec.seals/Mathf.Max(1,r.sealsCost)),"SealProgress");
            var br=(RectTransform)bar.transform.parent;br.anchorMin=br.anchorMax=br.pivot=new Vector2(0,1);br.anchoredPosition=new Vector2(30,-398);br.sizeDelta=new Vector2(1080,12);
            Text(detail,"Cost","FABRICAÇÃO\n"+r.sealsCost+" selos  +  "+r.goldCost+" ouro  +  "+r.scrapCost+" sucata",30,440,1080,80,25);
            Text(detail,"Money","SEUS RECURSOS: "+boundSave.gold.ToString("N0")+" ouro  |  "+boundSave.scrap.ToString("N0")+" sucata",30,540,1080,38,23,UIFactory.Gold);
            var craft=Button(detail,"Craft","FABRICAR EQUIPAMENTO",30,600,526,66,()=>Act(()=>RegionalRewards.Craft(boundSave,r.id)),true);
            craft.interactable=rec.blueprint && rec.seals>=r.sealsCost && boundSave.gold>=r.goldCost && boundSave.scrap>=r.scrapCost && (boundSave.inventory==null || boundSave.inventory.Count<60);
            Button(detail,"Claim","RESGATAR PENDENTES · "+state.pending.Count,580,600,538,66,()=>Act(()=>RegionalRewards.Claim(boundSave)));
            var title=Button(detail,"TitleReward",rec.title?"USAR TÍTULO":"TÍTULO · NORMAL+",30,686,526,58,()=>Act(()=>RegionalRewards.EquipCosmetic(boundSave,r.id,false)));title.interactable=rec.title;
            var frame=Button(detail,"FrameReward",rec.frame?"USAR MOLDURA":"MOLDURA · PESADELO",580,686,538,58,()=>Act(()=>RegionalRewards.EquipCosmetic(boundSave,r.id,true)));frame.interactable=rec.frame;
            var footer=Panel("Footer",0,906,1520,74);
            Text(footer,"Feedback",string.IsNullOrEmpty(message)?"Ao voltar, seus itens e recursos serão atualizados. As escolhas da equipe não são alteradas.":message,24,12,1472,50,22,UIFactory.Gold);
        }
        catch(Exception e){Text(stage,"Failure","Não foi possível mostrar a oficina: "+e.Message,400,160,1080,240);Debug.LogError(e);}
    }
    void Act(Func<string> action)
    {
        if(GameSession.Save!=boundSave || ZombitCloud.Uid!=boundUid){message="A sessão mudou. Feche e abra a oficina novamente.";Draw();return;}
        try{message=action();RegionalRewards.LastMessage=message;}catch(Exception e){message=e.Message;Debug.LogError(e);}
        Draw();
    }
    void Update()
    {
        if(screen!=null && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)Close();
    }
    public void Close()
    {
        if(screen==null)return;
        screen.gameObject.SetActive(false);Destroy(screen.gameObject);screen=null;canvas.gameObject.SetActive(false);
        var callback=closed;closed=null;boundSave=null;
        if(callback!=null)StartCoroutine(InvokeClosedNextFrame(callback));
    }
    IEnumerator InvokeClosedNextFrame(Action callback){yield return null;callback?.Invoke();}
    void OnDestroy(){if(Instance==this)Instance=null;if(canvas!=null)Destroy(canvas.gameObject);}
}
