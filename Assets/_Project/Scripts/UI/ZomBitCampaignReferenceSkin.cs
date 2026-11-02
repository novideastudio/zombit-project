using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Additive visual adapter: existing buttons, texts, callbacks and campaign data are preserved.
public sealed class ZomBitCampaignReferenceSkin : MonoBehaviour
{
    public ZomBitCampaignReferenceTheme theme;
    RectTransform stage;float nextSearch;string lastSelection;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.buildIndex==GameSession.MenuSceneIndex)
            new GameObject("CampaignReferenceSkin").AddComponent<ZomBitCampaignReferenceSkin>();
    }
    void Update()
    {
        if(theme==null)theme=Resources.Load<ZomBitCampaignReferenceTheme>("Config/CampaignReferenceTheme");
        if(theme==null || !theme.enabledTheme)return;
        if(stage==null && Time.unscaledTime>=nextSearch)
        {
            nextSearch=Time.unscaledTime+.5f;
            foreach(var menu in FindObjectsByType<MainMenuController>(FindObjectsSortMode.None))
                foreach(var t in menu.gameObject.scene.GetRootGameObjects())
                {
                    var transforms=t.GetComponentsInChildren<RectTransform>(true);
                    foreach(var rt in transforms)if(rt.name=="PcCampaignStage"){stage=rt;break;}
                    if(stage!=null)break;
                }
            if(stage!=null)ApplyLayout();
        }
    }
    void LateUpdate()
    {
        if(stage==null || theme==null || !theme.enabledTheme || !stage.gameObject.activeInHierarchy)return;
        // Update geometry every LateUpdate so controller callbacks cannot leave a stale layout.
        // Skin after the original updater; original controller may change button sprites.
        PaintSelection();
    }
    static RectTransform R(Transform p,string path){return p!=null?p.Find(path) as RectTransform:null;}
    static void Place(RectTransform rt,Vector4 q)
    {
        if(rt==null)return;
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.localScale=Vector3.one;rt.anchoredPosition=new Vector2(q.x,-q.y);rt.sizeDelta=new Vector2(q.z,q.w);
    }
    static void Stretch(RectTransform r)
    {
        if(r==null)return;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
    }
    static void Hide(Transform p,string name){var t=p!=null?p.Find(name):null;if(t!=null)t.gameObject.SetActive(false);}
    static void Art(Transform p,Sprite sprite,bool sliced=false)
    {
        if(p==null || sprite==null)return;var im=p.GetComponent<Image>();if(im==null)return;
        im.sprite=sprite;im.type=sliced?Image.Type.Sliced:Image.Type.Simple;im.color=Color.white;
    }
    static void Label(Transform p,string name,float size)
    {
        var t=p!=null?p.Find(name)?.GetComponent<TMP_Text>():null;
        if(t==null)return;t.fontStyle=FontStyles.Bold;t.fontSizeMax=size;t.fontSize=size;
        t.enableAutoSizing=true;t.fontSizeMin=12;t.color=Color.white;
    }
    void MoveAction(RectTransform panel,string name,Vector4 rect,Sprite icon,string iconName)
    {
        var b=R(panel,name);if(b==null)b=R(stage,name);
        if(b==null){Debug.LogWarning("Campaign skin: botão ausente: "+name);return;}
        b.SetParent(stage,false);Place(b,rect);Art(b,theme.goldFrame,true);
        var label=R(b,"Label");Place(label,new Vector4(60,0,rect.z-70,rect.w));Label(b,"Label",25);
        var old=R(b,iconName);
        if(old!=null){Place(old,new Vector4(17,(rect.w-34)/2,34,34));Art(old,icon);}
        var button=b.GetComponent<Button>();if(button!=null){var cb=button.colors;cb.normalColor=Color.white;button.colors=cb;}
        var modal=R(stage,"PcCampaignRules");if(modal!=null)b.SetSiblingIndex(modal.GetSiblingIndex());
    }
    void ApplyLayout()
    {
        var map=R(stage,"MapArea");Place(map,theme.mapRect);
        var panel=R(stage,"PcCampaignDetails");Place(panel,theme.detailsRect);Art(panel,theme.normalFrame,true);
        if(panel==null)return;
        Hide(stage,"CampaignLogo");
        // Replace absent logo with a configurable map icon instead of leaving a white rectangle.
        var logoT=stage.Find("CampaignReferenceMapIcon");
        var logo=logoT!=null?logoT.GetComponent<Image>():UIFactory.Img(stage,Color.white,"CampaignReferenceMapIcon");logo.sprite=theme.mapIcon;
        logo.preserveAspect=true;logo.raycastTarget=false;Place(logo.rectTransform,new Vector4(135,14,88,76));
        var header=stage.Find("CampaignHeader");if(header!=null)logo.transform.SetSiblingIndex(header.GetSiblingIndex()+1);
        Place(R(stage,"CampaignHeader"),new Vector4(112,0,1110,98));Art(R(stage,"CampaignHeader"),theme.normalFrame,true);
        Place(R(stage,"CampaignTitle"),new Vector4(250,0,700,62));Label(stage,"CampaignTitle",49);
        Place(R(stage,"CampaignChapter"),new Vector4(250,62,820,32));Label(stage,"CampaignChapter",26);
        Place(R(stage,"CampaignBack"),new Vector4(18,10,80,80));
        var wallet=R(stage,"CampaignWallet");Place(wallet,new Vector4(1240,8,536,80));Art(wallet,theme.normalFrame,true);
        Place(R(wallet,"GoldIcon"),new Vector4(18,18,40,40));Art(R(wallet,"GoldIcon"),theme.coinIcon);
        Place(R(wallet,"GoldValue"),new Vector4(70,10,140,60));Label(wallet,"GoldValue",34);
        Place(R(wallet,"WalletSeparator"),new Vector4(260,10,2,60));
        Place(R(wallet,"ScrapIcon"),new Vector4(280,18,40,40));Art(R(wallet,"ScrapIcon"),theme.gearIcon);
        Place(R(wallet,"ScrapValue"),new Vector4(335,10,180,60));Label(wallet,"ScrapValue",34);
        Place(R(stage,"CampaignSettings"),new Vector4(1800,8,88,80));Art(R(stage,"CampaignSettings/SettingsIcon"),theme.gearIcon);
        var close=R(panel,"TitleBar/CloseDetails");
        if(close!=null){close.gameObject.SetActive(true);Place(close,new Vector4(510,7,48,48));Art(close,theme.goldFrame,true);close.SetAsLastSibling();}
        Place(R(panel,"TitleBar"),new Vector4(16,10,570,66));
        Place(R(panel,"TitleBar/NumberBadge"),new Vector4(0,0,80,60));
        Place(R(panel,"TitleBar/Num"),new Vector4(0,0,80,60));Label(R(panel,"TitleBar"),"Num",38);
        Place(R(panel,"TitleBar/DName"),new Vector4(96,0,400,64));Label(R(panel,"TitleBar"),"DName",37);
        Place(R(panel,"PreviewFrame"),new Vector4(20,82,562,174));
        Place(R(panel,"PreviewFrame/PreviewMask"),new Vector4(6,6,550,162));
        var preview=R(panel,"PreviewFrame/PreviewMask/SelectedLocationPreview");
        if(preview!=null){preview.sizeDelta=new Vector2(550,162);}
        Place(R(panel,"Description"),new Vector4(24,264,554,60));Label(panel,"Description",21);
        // Keep level / lock information available, even though reference art omits it.
        Place(R(panel,"LocationNote"),new Vector4(24,323,310,25));
        Hide(panel,"CompletionCheckCircle");Hide(panel,"RecommendedLevelFrame");
        Place(R(panel,"Stat_Waves"),new Vector4(20,352,180,62));
        Place(R(panel,"Stat_Enemies"),new Vector4(210,352,180,62));
        Place(R(panel,"Stat_Reward"),new Vector4(400,352,180,62));
        Place(R(panel,"EnemiesTitle"),new Vector4(24,426,554,28));Label(panel,"EnemiesTitle",24);
        Place(R(panel,"EnemyRow"),new Vector4(20,462,562,106));
        Place(R(panel,"ItemsTitle"),new Vector4(24,584,554,28));Label(panel,"ItemsTitle",24);
        Place(R(panel,"ItemRow"),new Vector4(20,620,562,76));
        Place(R(panel,"DifficultyTitle"),new Vector4(24,708,554,30));Label(panel,"DifficultyTitle",24);
        for(int i=1;i<=4;i++)
        {
            var b=R(panel,"Difficulty_"+i);Place(b,new Vector4(20+(i-1)*142,746,134,58));
            Place(R(b,"Label"),new Vector4(6,0,122,58));Label(b,"Label",23);
            Place(R(b,"DifficultyLock"),new Vector4(112,36,16,18));
        }
        MoveAction(panel,"MissionRules",theme.rulesRect,theme.rulesIcon,"Icon_info");
        MoveAction(panel,"ChooseFormation",theme.formationRect,theme.formationIcon,"Icon_formation");
        var start=R(panel,"StartMission");Place(start,new Vector4(20,830,562,98));Art(start,theme.selectedFrame,true);
        Place(R(start,"Label"),new Vector4(68,0,418,98));Label(start,"Label",37);
        var playT=start!=null?start.Find("ReferenceStartIcon"):null;
        var play=playT!=null?playT.GetComponent<Image>():UIFactory.Img(start,Color.white,"ReferenceStartIcon");play.sprite=theme.startIcon;play.preserveAspect=true;play.raycastTarget=false;
        Place(play.rectTransform,new Vector4(18,26,46,46));
        Place(R(start,"Icon_arrow"),new Vector4(505,32,32,32));
        if(map!=null && theme.mapBackground!=null)Art(map.Find("MapInner/CityMapArt"),theme.mapBackground);
        var layer=map!=null?map.Find("MapInner/MapMarkerLayer"):null;
        if(layer!=null)foreach(var entry in theme.locations ?? new ZomBitCampaignReferenceTheme.Entry[0])
        {
            if(entry==null || string.IsNullOrEmpty(entry.id))continue;
            var node=R(layer,"Node_"+entry.id);if(node==null)continue;
            Place(node,entry.rect);
            Hide(node,"LocationThumbnail");Hide(node,"ThumbnailShade");Hide(node,"InfoShade");
            Place(R(node,"NumberBadge"),new Vector4(10,12,80,entry.rect.w-24));
            Place(R(node,"Number"),new Vector4(10,12,80,entry.rect.w-24));Label(node,"Number",40);
            Place(R(node,"LocationName"),new Vector4(104,10,entry.rect.z-116,entry.rect.w*.48f));Label(node,"LocationName",25);
            var txt=node.Find("LocationName")?.GetComponent<TMP_Text>();
            if(txt!=null)txt.textWrappingMode=TextWrappingModes.Normal;
            // Preserve four difficulty-completion stars; the supplied mockup shows three.
            for(int i=0;i<4;i++)Place(R(node,"Star_"+i),new Vector4(106+i*32,entry.rect.w-40,26,26));
            Place(R(node,"Lock"),new Vector4(entry.rect.z-32,entry.rect.w-38,22,28));
        }
        PaintSelection();
    }
    void PaintSelection()
    {
        var panel=R(stage,"PcCampaignDetails");if(panel==null)return;
        // Reapply map geometry after original open/close handlers, while details are visible.
        SyncMapGeometry(); // ZOMBIT_MAP_GEOMETRY_V2
        foreach(var entry in theme.locations ?? new ZomBitCampaignReferenceTheme.Entry[0])
        {
            if(entry==null)continue;
            var node=stage.Find("MapArea/MapInner/MapMarkerLayer/Node_"+entry.id);
            if(node==null)continue;
            var pin=node.Find("SelectedPin");bool selected=panel.gameObject.activeSelf && pin!=null && pin.gameObject.activeSelf;
            // Don't disable the pin: its active state is maintained by SelectLocation.
            var pinImage=pin!=null?pin.GetComponent<Image>():null;if(pinImage!=null)pinImage.enabled=false;
            Art(node,selected?theme.selectedFrame:theme.normalFrame,true);
            Art(node.Find("NumberBadge"),selected?theme.selectedFrame:theme.goldFrame,true);
            var outline=node.GetComponent<Outline>();if(outline!=null)
            {outline.effectColor=selected?new Color(1,.25f,0,1):new Color(.35f,.45f,.5f,1);outline.effectDistance=new Vector2(selected?3:1,selected?-3:-1);}
        }
        for(int i=1;i<=4;i++)
        {
            var t=panel.Find("Difficulty_"+i);if(t==null)continue;
            var old=t.GetComponent<Image>();
            // Existing controller owns selection, interactability and colors; retain it.
            if(old!=null && old.sprite==null)Art(t,theme.normalFrame,true);
        }
    }
    // Keep the map viewport and marker coordinates consistent in both states.
    void SyncMapGeometry()
    {
        var map=R(stage,"MapArea");var details=R(stage,"PcCampaignDetails");
        if(map==null || theme==null || details==null)return;
        bool open=details.gameObject.activeSelf;
        Vector4 q=theme.mapRect;
        if(!open)
        {
            // Full stage width; preserve the configured header/footer baseline.
            q.x=0;q.z=Mathf.Max(1,stage.rect.width);
        }
        Place(map,q);
        var inner=R(map,"MapInner");
        var markers=inner!=null?R(inner,"MapMarkerLayer"):null;
        var art=inner!=null?inner.Find("CityMapArt")?.GetComponent<Image>():null;
        if(markers==null || inner==null)return;
        // Fit image and marker layer to exactly the same rendered rectangle.
        Vector2 area=inner.rect.size;
        if(area.x<=0 || area.y<=0)return;
        Vector2 rendered=area;
        if(art!=null && art.sprite!=null)
        {
            art.preserveAspect=true;
            float aspect=art.sprite.rect.width/Mathf.Max(1,art.sprite.rect.height);
            if(area.x/area.y>aspect)rendered.x=area.y*aspect;
            else rendered.y=area.x/aspect;
        }
        markers.anchorMin=markers.anchorMax=markers.pivot=new Vector2(.5f,.5f);
        markers.anchoredPosition=Vector2.zero;markers.sizeDelta=rendered;markers.localScale=Vector3.one;
        float baseW=Mathf.Max(1,theme.mapRect.z),baseH=Mathf.Max(1,theme.mapRect.w);
        foreach(var entry in theme.locations ?? new ZomBitCampaignReferenceTheme.Entry[0])
        {
            if(entry==null || string.IsNullOrEmpty(entry.id))continue;
            var node=R(markers,"Node_"+entry.id);if(node==null)continue;
            node.anchorMin=node.anchorMax=new Vector2(entry.rect.x/baseW,1-entry.rect.y/baseH);
            node.pivot=new Vector2(0,1);node.anchoredPosition=Vector2.zero;
            node.sizeDelta=new Vector2(entry.rect.z,entry.rect.w);node.localScale=Vector3.one;
        }
    }

    [ContextMenu("Reaplicar layout (Play Mode)")]
    public void Reapply(){if(stage!=null && theme!=null)ApplyLayout();}
}
