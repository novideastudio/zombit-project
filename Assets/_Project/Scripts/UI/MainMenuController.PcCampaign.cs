using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PC campaign skin. All progression/mission actions remain in the original controllers.
public partial class MainMenuController
{
    const string PcCampaignArt="Art/PCCampaignUI/";
    static readonly Dictionary<string,Sprite> pcCampaignSprites=new Dictionary<string,Sprite>();
    readonly Dictionary<string,Image> pcCampaignNodes=new Dictionary<string,Image>();
    readonly Dictionary<string,Image> pcCampaignNodeBadges=new Dictionary<string,Image>();
    readonly Dictionary<string,GameObject> pcCampaignPins=new Dictionary<string,GameObject>();
    RectTransform pcCampaignStage,pcCampaignMap;
    GameObject pcCampaignDetails,pcCampaignRules;
    readonly List<GameObject> pcCampaignDiffLocks=new List<GameObject>();
    Button pcCampaignFormation;
    TMP_Text pcCampaignGoldText,pcCampaignScrapText;
    GameObject pcCampaignCompletionIcon;
    bool pcCampaignOpen;
    string pcCampaignVisualKey;
    string optionsReturnScreen;

    static RectTransform PcCampRect(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0f,1f);
        rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);rt.localScale=Vector3.one;
        return rt;
    }
    Sprite PcCampSprite(string name,bool sliced=true)
    {
        if(pcCampaignSprites.TryGetValue(name,out var sp) && sp!=null)return sp;
        var tex=Resources.Load<Texture2D>(PcCampaignArt+name);
        if(tex==null)
        {
            Debug.LogWarning("Zombit: textura de campanha ausente: "+PcCampaignArt+name);
            return null;
        }
        var border=sliced?new Vector4(20,20,20,20):Vector4.zero;
        sp=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(0.5f,0.5f),100f,0,SpriteMeshType.FullRect,border);
        pcCampaignSprites[name]=sp;return sp;
    }
    Image PcCampImage(Transform parent,string name,string texture,float x,float y,float w,float h,bool sliced=true)
    {
        var rt=PcCampRect(parent,name,x,y,w,h);var im=rt.gameObject.AddComponent<Image>();
        im.sprite=PcCampSprite(texture,sliced);im.type=sliced?Image.Type.Sliced:Image.Type.Simple;
        im.color=Color.white;im.raycastTarget=false;return im;
    }
    Image PcCampArtImage(Transform parent,string name,string resourcePath,float x,float y,float w,float h)
    {
        var rt=PcCampRect(parent,name,x,y,w,h);var im=rt.gameObject.AddComponent<Image>();
        im.sprite=ArtLoader.Get(resourcePath);im.type=Image.Type.Simple;im.preserveAspect=true;
        im.color=Color.white;im.raycastTarget=false;
        if(im.sprite==null) Debug.LogWarning("Zombit: recurso visual ausente: "+resourcePath);
        return im;
    }
    TMP_Text PcCampText(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color,bool wrap=false,TextAlignmentOptions alignment=TextAlignmentOptions.MidlineLeft)
    {
        var rt=PcCampRect(parent,name,x,y,w,h);var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
        var theme=UITheme.Current;if(theme.displayFont!=null)t.font=theme.displayFont;else if(theme.font!=null)t.font=theme.font;
        t.text=text;t.fontSize=size;t.fontSizeMax=size;t.fontSizeMin=Mathf.Min(size,12f);t.enableAutoSizing=true;
        t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=alignment;t.richText=true;t.raycastTarget=false;
        t.textWrappingMode=wrap?TextWrappingModes.Normal:TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Truncate;
        return t;
    }
    Button PcCampButton(Transform parent,string name,string text,string texture,float x,float y,float w,float h,Action action,float size=22f)
    {
        var im=PcCampImage(parent,name,texture,x,y,w,h);im.raycastTarget=true;
        var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
        var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1f,0.95f,0.84f);c.pressedColor=new Color(0.73f,0.73f,0.70f);c.selectedColor=Color.white;b.colors=c;
        if(action!=null)b.onClick.AddListener(()=>{ZombitAudio.PlayUI();action();});
        PcCampText(im.transform,"Label",text,10,0,w-20,h,size,new Color(0.94f,0.90f,0.78f),false,TextAlignmentOptions.Center);
        return b;
    }
    void PcCampIcon(Transform parent,string key,float x,float y,float size)
    {
        var image=PcCampImage(parent,"Icon_"+key,key,x,y,size,size,false);image.preserveAspect=true;
    }
    static Color PcCampCream => new Color(0.93f,0.89f,0.77f);
    static Color PcCampMuted => new Color(0.67f,0.75f,0.77f);

    // Layout de missão inspirado na referência 16:9. Os campos continuam ligados à lógica original.
    void BuildPcCampaignPanel(RectTransform screen)
    {
        pcCampaignDiffLocks.Clear();pcCampaignVisualKey=null;
        var panel=PcCampImage(screen,"PcCampaignDetails","panel",1302,94,590,958);
        pcCampaignDetails=panel.gameObject;
        var title=PcCampRect(panel.transform,"TitleBar",16,12,558,62);
        PcCampImage(title,"NumberBadge","red",0,6,58,50);
        hubBadge=PcCampText(title,"Num","",0,5,58,50,27,new Color(1f,.25f,.08f,1f),false,TextAlignmentOptions.Center);
        dName=PcCampText(title,"DName","",70,0,418,62,29,PcCampCream);
        PcCampButton(title,"CloseDetails","X","button",506,6,46,46,()=>SetPcCampaignDetails(false),25);

        var previewFrame=PcCampImage(panel.transform,"PreviewFrame","slot",20,82,550,151);
        var mask=PcCampRect(previewFrame.transform,"PreviewMask",6,6,538,139);mask.gameObject.AddComponent<RectMask2D>();
        var previewGo=PcCampRect(mask,"SelectedLocationPreview",0,0,538,139);
        selectedLocationPreview=previewGo.gameObject.AddComponent<Image>();selectedLocationPreview.color=Color.white;selectedLocationPreview.raycastTarget=false;
        hubHeroFitter=previewGo.gameObject.AddComponent<AspectRatioFitter>();hubHeroFitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;hubHeroFitter.aspectRatio=3.85f;

        dDesc=PcCampText(panel.transform,"Description","",24,242,542,50,18,new Color(0.78f,0.83f,0.84f),true);
        var completionCircle=UIFactory.Img(panel.transform,new Color(.025f,.13f,.085f,1f),"CompletionCheckCircle");
        UIKit.ApplyCircle(completionCircle,new Color(.025f,.13f,.085f,1f));
        UIFactory.Place(completionCircle.rectTransform,new Vector2(0f,1f),new Vector2(24f,-299f),new Vector2(24f,24f));
        completionCircle.raycastTarget=false;pcCampaignCompletionIcon=completionCircle.gameObject;
        PcCampArtImage(completionCircle.transform,"Check","Art/Skills/ui_check",4,4,16,16);
        dInfo=PcCampText(panel.transform,"LocationNote","",51,296,275,30,15,UIKit.Col.Gold,true);
        var recommended=PcCampImage(panel.transform,"RecommendedLevelFrame","gold",340,294,230,34);
        hubRecLevel=PcCampText(recommended.transform,"RecommendedLevel","",6,0,218,34,14,PcCampCream,false,TextAlignmentOptions.Center);

        hubStatA=BuildPcCampaignStat(panel.transform,"Waves",20,"Art/Skills/icon_skull");
        hubStatB=BuildPcCampaignStat(panel.transform,"Enemies",202,"Art/PCCampaignUI/formation");
        hubStatD=BuildPcCampaignStat(panel.transform,"Reward",384,"Art/Skills/icon_coins");hubStatC=null;

        PcCampText(panel.transform,"EnemiesTitle","INIMIGOS PRINCIPAIS",24,425,526,26,17,PcCampCream);
        hubEnemyRow=PcCampRect(panel.transform,"EnemyRow",20,456,550,100);
        var h=UIFactory.HLayout(hubEnemyRow,8,TextAnchor.MiddleCenter);h.childForceExpandWidth=true;h.childForceExpandHeight=true;

        PcCampText(panel.transform,"ItemsTitle","ITENS POSSÍVEIS",24,578,526,26,17,PcCampCream);
        var items=PcCampRect(panel.transform,"ItemRow",20,604,550,70);
        var ih=UIFactory.HLayout(items,6,TextAnchor.MiddleCenter);ih.childForceExpandWidth=true;ih.childForceExpandHeight=true;
        BuildHubItems(items);

        PcCampText(panel.transform,"DifficultyTitle","DIFICULDADE",24,688,526,26,17,PcCampCream);
        for(int i=0;i<4;i++)
        {
            int level=i+1;
            var b=PcCampButton(panel.transform,"Difficulty_"+level,Difficulties.Get(level).name,"button",20+i*137,711,129,52,()=>{SelectDifficulty(level);RefreshPcCampaignStyle();},18);
            diffButtons[i]=b;
            var lockIcon=PcCampImage(b.transform,"DifficultyLock","lock",106,27,14,17,false);lockIcon.preserveAspect=true;
            pcCampaignDiffLocks.Add(lockIcon.gameObject);
        }
        var rules=PcCampButton(panel.transform,"MissionRules","REGRAS DA MISSÃO","gold",20,772,269,62,()=>{pcCampaignRules.SetActive(true);},20);
        PcCampIcon(rules.transform,"info",12,16,30);PcCampShiftLabel(rules,44f);
        // Formação fica como ícone dentro da barra para manter a composição da referência sem perder o recurso.
        pcCampaignFormation=PcCampButton(panel.transform,"ChooseFormation","FORMAÇÃO","gold",301,772,269,62,OpenMissionFormation,20);
        PcCampIcon(pcCampaignFormation.transform,"formation",12,16,30);PcCampShiftLabel(pcCampaignFormation,44f);
        dStart=PcCampButton(panel.transform,"StartMission","INICIAR MISSÃO","red",20,846,550,88,OnStartMission,30);
        PcCampIcon(dStart.transform,"arrow",485,28,30);
    }
    TMP_Text BuildPcCampaignStat(Transform parent,string name,float x,string iconPath)
    {
        var f=PcCampImage(parent,"Stat_"+name,"slot",x,346,174,60);
        Image icon;
        if(iconPath.StartsWith("Art/PCCampaignUI/",StringComparison.Ordinal))
            icon=PcCampImage(f.transform,"StatIcon",iconPath.Substring("Art/PCCampaignUI/".Length),8,15,30,30,false);
        else icon=PcCampArtImage(f.transform,"StatIcon",iconPath,8,15,30,30);
        icon.preserveAspect=true;
        return PcCampText(f.transform,"Value","",42,3,126,54,17,PcCampCream,true,TextAlignmentOptions.Center);
    }

    void SetupPcCampaign(RectTransform screen,RectTransform mapArea)
    {
        // Ajusta toda a composição como um único quadro 16:9 para não deformar os marcadores.
        var children=new List<Transform>();for(int i=0;i<screen.childCount;i++)children.Add(screen.GetChild(i));
        var black=UIFactory.Img(screen,Color.black,"PcCampaignLetterbox");UIFactory.Stretch(black.rectTransform);black.raycastTarget=false;black.transform.SetAsFirstSibling();
        pcCampaignStage=PcCampRect(screen,"PcCampaignStage",0,0,1920,1080);
        pcCampaignStage.anchorMin=pcCampaignStage.anchorMax=pcCampaignStage.pivot=new Vector2(0.5f,0.5f);pcCampaignStage.anchoredPosition=Vector2.zero;
        foreach(var c in children)c.SetParent(pcCampaignStage,false);
        var fit=pcCampaignStage.gameObject.AddComponent<CampaignPcFit>();fit.content=pcCampaignStage;fit.viewport=screen;fit.Apply();
        pcCampaignMap=mapArea;
        var backdrop=pcCampaignStage.Find("MapBackdrop");if(backdrop!=null)backdrop.SetAsFirstSibling();
        mapArea.SetSiblingIndex(backdrop!=null?backdrop.GetSiblingIndex()+1:0);
        var inner=mapArea.Find("MapInner") as RectTransform;if(inner)ExactMapRect(inner,Vector2.zero,Vector2.one);

        PcCampImage(pcCampaignStage,"CampaignHeader","panel",100,18,575,70);
        PcCampArtImage(pcCampaignStage,"CampaignLogo","Art/PCInventoryUI/logo",106,8,218,78);
        PcCampText(pcCampaignStage,"CampaignTitle","CAMPANHA",354,13,305,36,31,PcCampCream);
        PcCampText(pcCampaignStage,"CampaignChapter","CAPÍTULO 1 · REGIÃO URBANA",355,49,305,28,17,new Color(0.40f,0.86f,0.94f));
        var back=PcCampButton(pcCampaignStage,"CampaignBack","","button",18,18,68,68,()=>{ClosePcCampaignRules();ShowHome();});PcCampIcon(back.transform,"back",17,17,34);

        // Carteira e configurações no canto superior direito, conforme a referência.
        var wallet=PcCampImage(pcCampaignStage,"CampaignWallet","panel",1342,10,446,72);
        PcCampArtImage(wallet.transform,"GoldIcon","Art/Skills/icon_coins",22,17,38,38);
        pcCampaignGoldText=PcCampText(wallet.transform,"GoldValue","0",70,5,96,60,25,PcCampCream,false,TextAlignmentOptions.MidlineLeft);
        var sep=UIFactory.Img(wallet.transform,new Color(0.55f,0.62f,0.64f,0.75f),"WalletSeparator");
        UIFactory.Place(sep.rectTransform,new Vector2(0f,0.5f),new Vector2(182f,0f),new Vector2(2f,42f));sep.raycastTarget=false;
        PcCampArtImage(wallet.transform,"ScrapIcon","Art/Skills/icon_gear",205,17,38,38);
        pcCampaignScrapText=PcCampText(wallet.transform,"ScrapValue","0",253,5,120,60,25,PcCampCream,false,TextAlignmentOptions.MidlineLeft);
        var settings=PcCampButton(pcCampaignStage,"CampaignSettings","","button",1800,10,80,72,OpenCampaignOptions);
        PcCampArtImage(settings.transform,"SettingsIcon","Art/Skills/icon_gear",16,14,48,44);

        BuildPcCampaignRulesModal();
        SetPcCampaignDetails(false); // ZOMBIT_MAP_START_CLOSED_V2
        var updater=pcCampaignStage.gameObject.AddComponent<CampaignPcRefresh>();
        updater.refresh=()=>{RefreshPcCampaignStyle();RefreshPcCampaignWallet();};
        updater.escape=()=>{if(missionFormationCanvas!=null){CloseMissionFormation();return;}if(pcCampaignRules!=null && pcCampaignRules.activeSelf){ClosePcCampaignRules();return;}if(pcCampaignOpen)SetPcCampaignDetails(false);else ShowHome();};
        RefreshPcCampaignStyle();RefreshPcCampaignWallet();fit.Apply();
    }
    void RefreshPcCampaignWallet()
    {
        var save=GameSession.Save;if(save==null)return;
        if(pcCampaignGoldText)pcCampaignGoldText.text=save.gold.ToString("N0");
        if(pcCampaignScrapText)pcCampaignScrapText.text=save.scrap.ToString("N0");
    }
    void OpenCampaignOptions()
    {
        optionsReturnScreen="map";
        RefreshOptions();
        Show("options");
    }
    void SetPcCampaignDetails(bool visible)
    {
        pcCampaignOpen=visible;
        if(pcCampaignMap)ExactMapRect(pcCampaignMap,Vector2.zero,new Vector2(visible?0.70f:1f,1f));
        if(pcCampaignDetails) { pcCampaignDetails.SetActive(visible);pcCampaignDetails.transform.SetAsLastSibling(); }
        if(pcCampaignRules)pcCampaignRules.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
    }
    void TogglePcCampaignLocation(LocationDef loc)
    {
        if(loc==null)return;
        if(pcCampaignOpen && selectedLoc!=null && selectedLoc.id==loc.id) {SetPcCampaignDetails(false);return;}
        SelectLocation(loc);SetPcCampaignDetails(true);pcCampaignVisualKey=null;RefreshPcCampaignStyle();
    }

    // Caminho tactico entre os locais principais e as duas rotas laterais.
    void BuildPcCampaignRoutes(Transform parent, SaveData save)
    {
        if(parent==null)return;
        var go=new GameObject("CampaignProgressRoutes",typeof(RectTransform),typeof(CanvasRenderer),typeof(UIDashedPath));
        var rt=(RectTransform)go.transform;rt.SetParent(parent,false);ExactMapRect(rt,Vector2.zero,Vector2.one);
        var route=go.GetComponent<UIDashedPath>();route.thickness=4f;route.dash=13f;route.gap=10f;route.color=Color.white;route.raycastTarget=false;
        AddCampaignRoute(route,"suburbio","residencial",save,0.05f);
        AddCampaignRoute(route,"residencial","shopping",save,-0.08f);
        AddCampaignRoute(route,"shopping","metro",save,0.08f);
        AddCampaignRoute(route,"metro","centro",save,-0.09f);
        AddCampaignRoute(route,"centro","base",save,0.10f);
        AddCampaignRoute(route,"suburbio","hospital",save,-0.08f);
        AddCampaignRoute(route,"hospital","delegacia",save,0.09f);
        AddCampaignRoute(route,"centro","industrial",save,-0.06f);
        go.transform.SetAsFirstSibling();
    }

    void AddCampaignRoute(UIDashedPath path,string from,string to,SaveData save,float bend)
    {
        Vector2 a=CampaignNodeCenter(from),b=CampaignNodeCenter(to);
        bool active=save!=null && (save.CompletedLevelCount(from)>0 || save.CompletedLevelCount(to)>0);
        Color c=active?new Color(1f,0.61f,0.23f,0.72f):new Color(0.58f,0.71f,0.76f,0.42f);
        path.Add(a,b,c,bend);
    }

    static Vector2 CampaignNodeCenter(string id)
    {
        float x=0f,top=0f,w=0f,h=0f;
        switch(id)
        {
            case "suburbio":    x=.033f;top=.165f;w=.217f;h=.079f;break;
            case "residencial": x=.240f;top=.256f;w=.277f;h=.082f;break;
            case "shopping":    x=.392f;top=.449f;w=.266f;h=.083f;break;
            case "hospital":    x=.052f;top=.510f;w=.241f;h=.075f;break;
            case "delegacia":   x=.300f;top=.682f;w=.249f;h=.077f;break;
            case "metro":       x=.609f;top=.313f;w=.267f;h=.077f;break;
            case "centro":      x=.669f;top=.526f;w=.271f;h=.075f;break;
            case "base":        x=.683f;top=.152f;w=.246f;h=.082f;break;
            case "industrial":  x=.634f;top=.733f;w=.309f;h=.078f;break;
        }
        return new Vector2(x+w*0.5f,1f-top-h*0.5f);
    }

    // Cartoes com miniaturas recortadas por mascara (sem esticar as artes panoramicas),
    // nome, nivel recomendado, progresso e estado de desbloqueio.
    void BuildPcCampaignNode(Transform parent,LocationDef loc,int index,SaveData save)
    {
        if(parent==null || loc==null)return;
        float x,top,w,h;
        switch(loc.id)
        {
            case "suburbio":    x=.033f;top=.165f;w=.217f;h=.079f;break;
            case "residencial": x=.240f;top=.256f;w=.277f;h=.082f;break;
            case "shopping":    x=.392f;top=.449f;w=.266f;h=.083f;break;
            case "hospital":    x=.052f;top=.510f;w=.241f;h=.075f;break;
            case "delegacia":   x=.300f;top=.682f;w=.249f;h=.077f;break;
            case "metro":       x=.609f;top=.313f;w=.267f;h=.077f;break;
            case "centro":      x=.669f;top=.526f;w=.271f;h=.075f;break;
            case "base":        x=.683f;top=.152f;w=.246f;h=.082f;break;
            case "industrial":  x=.634f;top=.733f;w=.309f;h=.078f;break;
            default:return;
        }
        bool unlocked=GameDatabase.IsUnlocked(loc,save);
        int completed=Mathf.Clamp(save.CompletedLevelCount(loc.id),0,4);
        LocationDef location=loc;
        var go=new GameObject("Node_"+loc.id,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
        ExactMapRect(rt,new Vector2(x,1f-top-h),new Vector2(x+w,1f-top));
        var image=go.GetComponent<Image>();image.sprite=PcCampSprite("button");image.type=Image.Type.Sliced;image.raycastTarget=true;
        var button=go.GetComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;
        button.onClick.AddListener(()=>{ZombitAudio.PlayUI();TogglePcCampaignLocation(location);});

        // Thumb a direita: RectMask2D + EnvelopeParent faz crop sem deformar proporcoes.
        var thumbViewGo=new GameObject("LocationThumbnailViewport",typeof(RectTransform),typeof(RectMask2D));
        var thumbView=(RectTransform)thumbViewGo.transform;thumbView.SetParent(rt,false);
        ExactMapRect(thumbView,new Vector2(.585f,.07f),new Vector2(.98f,.93f));
        var artGo=new GameObject("LocationThumbnail",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var artRt=(RectTransform)artGo.transform;artRt.SetParent(thumbView,false);
        artRt.anchorMin=Vector2.zero;artRt.anchorMax=Vector2.one;artRt.pivot=new Vector2(.5f,.5f);
        artRt.offsetMin=Vector2.zero;artRt.offsetMax=Vector2.zero;
        var art=artGo.GetComponent<Image>();
        art.sprite=ArtLoader.Get(CampaignThumbnailPath(loc))??ArtLoader.Get("Art/Map/Locations/"+loc.id)??ArtLoader.Get("Art/Map/Locations/suburbio");
        art.raycastTarget=false;
        if(art.sprite!=null)
        {
            var fitter=artGo.AddComponent<AspectRatioFitter>();
            fitter.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio=art.sprite.rect.width/Mathf.Max(1f,art.sprite.rect.height);
        }
        if(!unlocked)art.color=new Color(.57f,.61f,.63f,1f);
        var artShade=UIFactory.Img(thumbView,new Color(.008f,.014f,.018f,unlocked ? 0.20f : 0.56f),"ThumbnailShade");
        UIFactory.Stretch(artShade.rectTransform);artShade.raycastTarget=false;
        var leftShade=UIFactory.Img(rt,new Color(.006f,.012f,.017f,.78f),"InfoShade");
        UIFactory.Stretch(leftShade.rectTransform,0f,.02f,.615f,.98f);leftShade.raycastTarget=false;

        var numPlate=PcCampImage(rt,"NumberBadge","gold",4,5,0,0);
        ExactMapRect(numPlate.rectTransform,new Vector2(.025f,.13f),new Vector2(.205f,.87f));
        numPlate.raycastTarget=false;pcCampaignNodeBadges[loc.id]=numPlate;
        var num=PcCampText(rt,"Number",MapDisplayNumber(loc).ToString("00"),0,0,1,1,25,unlocked?UIKit.Col.Gold:new Color(.53f,.58f,.59f),false,TextAlignmentOptions.Center);
        ExactMapRect(num.rectTransform,new Vector2(.025f,.13f),new Vector2(.205f,.87f));num.raycastTarget=false;

        var name=PcCampText(rt,"LocationName",MapDisplayName(loc),0,0,1,1,15,unlocked?Color.white:new Color(.66f,.71f,.72f),false,TextAlignmentOptions.MidlineLeft);
        ExactMapRect(name.rectTransform,new Vector2(.225f,.60f),new Vector2(.575f,.91f));
        name.characterSpacing=.15f;name.enableAutoSizing=true;name.fontSizeMin=9f;name.fontSizeMax=15f;name.overflowMode=TextOverflowModes.Truncate;name.raycastTarget=false;

        string recommended="RECOMMENDED LV. "+Mathf.Max(1,loc.recommendedLevel).ToString("00");
        var level=PcCampText(rt,"RecommendedLevel",recommended,0,0,1,1,11,unlocked?new Color(1f,.73f,.37f):new Color(.55f,.60f,.61f),false,TextAlignmentOptions.MidlineLeft);
        ExactMapRect(level.rectTransform,new Vector2(.23f,.36f),new Vector2(.575f,.57f));
        level.characterSpacing=.35f;level.raycastTarget=false;

        var progress=PcCampText(rt,"LocationProgress",completed>=4?"ALL LEVELS CLEARED":"PROGRESS  "+completed+" / 4",0,0,1,1,10,
            completed>0?new Color(.65f,.88f,.67f):new Color(.62f,.70f,.73f),false,TextAlignmentOptions.MidlineLeft);
        ExactMapRect(progress.rectTransform,new Vector2(.23f,.10f),new Vector2(.575f,.29f));progress.raycastTarget=false;

        for(int i=0;i<4;i++)
        {
            var starGo=new GameObject("Star_"+i,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var sr=(RectTransform)starGo.transform;sr.SetParent(rt,false);
            float sx=.235f+i*.070f;ExactMapRect(sr,new Vector2(sx,.29f),new Vector2(sx+.055f,.46f));
            var star=starGo.GetComponent<Image>();star.sprite=UIKit.Star;star.type=Image.Type.Simple;star.preserveAspect=true;star.raycastTarget=false;
            star.color=i<completed?UIKit.Col.Gold:new Color(.25f,.31f,.32f,1f);
        }
        if(!unlocked)
        {
            var lockImage=PcCampImage(rt,"Lock","lock",0,0,18,22,false);
            ExactMapRect(lockImage.rectTransform,new Vector2(.925f,.07f),new Vector2(.98f,.36f));lockImage.preserveAspect=true;
        }
        else if(completed>=4)
        {
            var clear=PcCampArtImage(rt,"ClearMark","Art/Skills/ui_check",0,0,22,22);
            ExactMapRect(clear.rectTransform,new Vector2(.925f,.67f),new Vector2(.98f,.93f));
            clear.color=new Color(.65f,1f,.69f,1f);
        }
        var outline=go.AddComponent<Outline>();outline.effectColor=unlocked?new Color(.66f,.73f,.74f,1f):new Color(.22f,.30f,.33f,1f);outline.effectDistance=new Vector2(1f,-1f);
        nodeOutlines[loc.id]=outline;nodeBorders[loc.id]=outline.effectColor;pcCampaignNodes[loc.id]=image;

        var pin=PcCampArtImage(rt,"SelectedPin","Art/Skills/ui_arrow_down",0,0,24,18);
        var pr=pin.rectTransform;pr.anchorMin=pr.anchorMax=new Vector2(.5f,0f);pr.pivot=new Vector2(.5f,1f);pr.anchoredPosition=new Vector2(0,-2);pr.sizeDelta=new Vector2(28,22);
        pin.color=new Color(1f,.30f,.08f,1f);pin.gameObject.SetActive(false);
        nodePins[loc.id]=pin.gameObject;pcCampaignPins[loc.id]=pin.gameObject;
    }

    string CampaignThumbnailPath(LocationDef loc)
    {
        if(loc==null)return "Art/Map/Locations/suburbio";
        switch(loc.id)
        {
            case "suburbio": return "Art/Backgrounds/wave_ruins";
            case "residencial": return "Art/Backgrounds/wave_city";
            case "shopping": return "Art/Backgrounds/wave_shopping";
            case "hospital": return "Art/Backgrounds/wave_hospital";
            case "delegacia": return "Art/Backgrounds/wave_rpd";
            case "metro": return "Art/Backgrounds/wave_metro";
            case "centro": return "Art/Backgrounds/wave_city";
            case "base": return "Art/Backgrounds/wave_military";
            case "industrial": return "Art/Backgrounds/wave_dock";
            default: return "Art/Map/Locations/"+loc.id;
        }
    }

    void RefreshPcCampaignStyle()
    {
        var save=GameSession.Save;if(save==null || selectedLoc==null || dStart==null)return;
        string key=selectedLoc.id+"#"+selectedDiff+"#"+save.selectedSurvivor+"#"+save.selectedPartner+"#"+save.selectedThird+"#"+buyInsurance;
        if(pcCampaignVisualKey==key)return;pcCampaignVisualKey=key;
        foreach(var kv in pcCampaignNodes)if(kv.Value)kv.Value.sprite=PcCampSprite(kv.Key==selectedLoc.id?"red":"button");
        foreach(var kv in pcCampaignNodeBadges)if(kv.Value)kv.Value.sprite=PcCampSprite(kv.Key==selectedLoc.id?"red":"gold");
        for(int i=0;i<4;i++)
        {
            if(diffButtons[i]==null)continue;
            bool unlocked=GameDatabase.IsDifficultyUnlocked(selectedLoc,save,i+1);
            var im=diffButtons[i].GetComponent<Image>();if(im)im.sprite=PcCampSprite(i+1==selectedDiff?"gold":"button");
            var cb=diffButtons[i].colors;cb.normalColor=Color.white;cb.disabledColor=new Color(0.50f,0.56f,0.59f,1f);cb.selectedColor=Color.white;diffButtons[i].colors=cb;
            var t=diffButtons[i].transform.Find("Label")?.GetComponent<TMP_Text>();if(t)t.text=Difficulties.Get(i+1).name;
            if(i<pcCampaignDiffLocks.Count && pcCampaignDiffLocks[i])pcCampaignDiffLocks[i].SetActive(!unlocked);
        }
        bool can=GameDatabase.IsDifficultyUnlocked(selectedLoc,save,selectedDiff);
        if(pcCampaignFormation)pcCampaignFormation.interactable=can;
        var startColors=dStart.colors;startColors.normalColor=Color.white;startColors.disabledColor=new Color(0.45f,0.45f,0.45f,1f);dStart.colors=startColors;
        var label=dStart.transform.Find("Label")?.GetComponent<TMP_Text>();if(label)label.text="INICIAR MISSÃO";
    }

    void BuildPcCampaignRulesModal()
    {
        var modal=PcCampRect(pcCampaignStage,"PcCampaignRules",0,0,1920,1080);pcCampaignRules=modal.gameObject;
        var shade=modal.gameObject.AddComponent<Image>();shade.color=new Color(0f,0f,0f,0.80f);shade.raycastTarget=true;
        var p=PcCampImage(modal,"RulesPanel","panel",420,150,1080,780);
        PcCampText(p.transform,"Title","REGRAS DA MISSÃO / SEGURO",24,16,960,48,30,PcCampCream);
        PcCampButton(p.transform,"CloseRules","X","button",1006,18,48,48,ClosePcCampaignRules,25);
        var viewport=PcCampRect(p.transform,"Scroll",24,82,1032,562);viewport.gameObject.AddComponent<RectMask2D>();
        var capture=viewport.gameObject.AddComponent<Image>();capture.color=Color.clear;capture.raycastTarget=true;
        dDiffInfo=PcCampText(viewport,"Rules","",0,0,1000,560,24,PcCampCream,true,TextAlignmentOptions.TopLeft);
        dDiffInfo.enableAutoSizing=false;dDiffInfo.overflowMode=TextOverflowModes.Overflow;
        dDiffInfo.rectTransform.anchorMin=new Vector2(0f,1f);dDiffInfo.rectTransform.anchorMax=new Vector2(1f,1f);dDiffInfo.rectTransform.pivot=new Vector2(0.5f,1f);dDiffInfo.rectTransform.sizeDelta=new Vector2(-18f,560f);dDiffInfo.rectTransform.anchoredPosition=Vector2.zero;
        var fitter=dDiffInfo.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=dDiffInfo.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=35;
        dInsurance=PcCampButton(p.transform,"Insurance","","button",24,670,1032,72,()=>{buyInsurance=!buyInsurance;RefreshDifficultyUI();pcCampaignVisualKey=null;RefreshPcCampaignStyle();},21);
        dInsuranceInfo=dInsurance.GetComponentInChildren<TMP_Text>();if(dInsuranceInfo)dInsuranceInfo.richText=true;
        pcCampaignRules.SetActive(false);
        // Bind rule/insurance texts for the location selected before this modal was created.
        RefreshDifficultyUI();
    }
    void ClosePcCampaignRules() {if(pcCampaignRules)pcCampaignRules.SetActive(false);}
}
