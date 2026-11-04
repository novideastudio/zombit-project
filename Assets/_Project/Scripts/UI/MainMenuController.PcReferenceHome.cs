using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PC reference camp: artwork matches the approved 1672x941 composition.
// This is a new home renderer only; existing navigation and campaign data remain owned by the game.
public partial class MainMenuController
{
    const string RefPcArt = "Art/PCReferenceUI/";
    static readonly Dictionary<string,Sprite> refPcSprites = new Dictionary<string,Sprite>();
    static Sprite refPcLogo;
    readonly Dictionary<string,SurvivorHighlight> refPcBorders = new Dictionary<string,SurvivorHighlight>();
    TMP_Text refPcNick,refPcClass,refPcPower,refPcGold,refPcScrap;
    Image refPcPortrait,refPcXp;
    GameObject refPcSkillBadge;

    // ---- Controles de aura/etiqueta do sobrevivente selecionado (Inspector do MainMenuController) ----
    [Header("Home - Aura do sobrevivente selecionado")]
    [Range(0f, 1f)] public float homeAuraIntensity = 0.20f;
    [Range(0.2f, 1.5f)] public float homeAuraSizeX = 0.62f;
    [Range(0.2f, 1.5f)] public float homeAuraSizeY = 0.58f;
    [Range(0f, 1f)] public float homeAuraPulseAmount = 0.15f;
    [Range(0f, 6f)] public float homeAuraPulseSpeed = 2.2f;
    public Color homeAuraColor = new Color(1f, 0.72f, 0.26f, 1f);
    public bool homeShowArrow = true;
    [Header("Home - Etiqueta (Classe + Nivel + Poder)")]
    public bool homeShowLabel = true;
    [Range(10f, 28f)] public float homeLabelSize = 15f;
    [Range(0f, 1f)] public float homeLabelOpacity = 0.75f;
    readonly Dictionary<string,TMP_Text> refPcLabels = new Dictionary<string,TMP_Text>();

    Sprite RefPcSprite(string key)
    {
        if(refPcSprites.TryGetValue(key,out var sprite) && sprite!=null) return sprite;
        var texture=Resources.Load<Texture2D>(RefPcArt+key);
        if(texture==null) throw new InvalidOperationException("Arte PC ausente: "+RefPcArt+key);
        sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(0.5f,0.5f),100f);
        sprite.name="RefPc_"+key;refPcSprites[key]=sprite;
        return sprite;
    }

    static RectTransform RefPcRect(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));
        var rt=(RectTransform)go.transform;rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0f,1f);
        rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);
        rt.localScale=Vector3.one;
        return rt;
    }

    Image RefPcImage(Transform parent,string name,string art,float x,float y,float w,float h)
    {
        var rt=RefPcRect(parent,name,x,y,w,h);
        var image=rt.gameObject.AddComponent<Image>();
        image.sprite=RefPcSprite(art);image.color=Color.white;
        image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;
        return image;
    }

    static Image RefPcSolid(Transform parent,string name,Color color,float x,float y,float w,float h)
    {
        var rt=RefPcRect(parent,name,x,y,w,h);
        var image=rt.gameObject.AddComponent<Image>();
        image.sprite=UIFactory.White;image.color=color;image.raycastTarget=false;
        return image;
    }

    TMP_Text RefPcText(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var rt=RefPcRect(parent,name,x,y,w,h);
        var t=rt.gameObject.AddComponent<TextMeshProUGUI>();
        var theme=UITheme.Current;
        if(theme.displayFont!=null) t.font=theme.displayFont;
        else if(theme.font!=null) t.font=theme.font;
        t.text=text;t.fontSize=size;t.fontSizeMax=size;t.fontSizeMin=Mathf.Min(11f,size);
        t.enableAutoSizing=true;t.fontStyle=FontStyles.Bold;t.color=color;t.alignment=align;
        t.textWrappingMode=TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Truncate;
        t.raycastTarget=false;t.richText=false;
        return t;
    }

    Button RefPcHit(RectTransform rt,Action action,bool artwork)
    {
        var image=rt.GetComponent<Image>();
        if(image==null) { image=rt.gameObject.AddComponent<Image>();image.color=Color.clear; }
        image.raycastTarget=true;
        var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;
        button.transition=artwork?Selectable.Transition.ColorTint:Selectable.Transition.None;
        var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1f,0.95f,0.82f,1f);
        c.pressedColor=new Color(0.78f,0.75f,0.69f,1f);c.selectedColor=Color.white;c.fadeDuration=0.08f;button.colors=c;
        var navigation=button.navigation;navigation.mode=Navigation.Mode.None;button.navigation=navigation;
        button.onClick.AddListener(()=> { ZombitAudio.PlayUI();action(); });
        return button;
    }

    void RefPcButton(Transform parent,string art,float x,float y,float w,float h,Action action)
    {
        var image=RefPcImage(parent,"ReferenceButton_"+art,art,x,y,w,h);
        RefPcHit(image.rectTransform,action,true);
    }

    void ShowPcReferenceHome()
    {
        var save=GameSession.Save;
        if(save==null) { Show("main");return; }
        save.MigrateFromPreviousVersions();
        Sprite campArt=ArtLoader.Get("Art/Home/camp_home");
        if(campArt==null)
        {
            Debug.LogError("Zombit: arte limpa do acampamento ausente em Resources/Art/Home/camp_home.");
            Toast("Arte do acampamento não encontrada.");return;
        }
        homeOrigin=false;homeFocusId=null;homeLastTapId=null;
        homeSeatRects.Clear();homePlates.Clear();homePlateOutlines.Clear();
        refPcBorders.Clear();refPcLabels.Clear();
        var s=Rebuild("home");
        var black=RefPcSolid(s,"PC_Letterbox",Color.black,0,0,1,1);
        UIFactory.Stretch(black.rectTransform);
        var stage=RefPcRect(s,"PC_ReferenceStage",0,0,1672,941);
        stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(0.5f,0.5f);stage.anchoredPosition=Vector2.zero;
        var fit=stage.gameObject.AddComponent<ReferenceHomeFit>();fit.content=stage;fit.viewport=s;
        fit.Apply();

        // Usa a arte limpa do acampamento (sem HUD embutido). A imagem e dimensionada
        // pelo seu aspect ratio real e recortada suavemente nas laterais, nunca deformada.
        float bgHeight=941f;
        float bgWidth=bgHeight*campArt.rect.width/Mathf.Max(1f,campArt.rect.height);
        var sceneRt=RefPcRect(stage,"PC_Scene",(1672f-bgWidth)*0.5f,0f,bgWidth,bgHeight);
        var sceneImage=sceneRt.gameObject.AddComponent<Image>();
        sceneImage.sprite=campArt;sceneImage.preserveAspect=true;sceneImage.raycastTarget=false;
        sceneImage.color=Color.white;
        // O fundo fica totalmente estatico. Apenas o efeito local da fogueira permanece.
        MenuAmbience.Attach(sceneRt,new Vector2(0.50f,0.34f));

        // Logo superior central, independente do fundo e com proporcao original.
        var logoSprite=ArtLoader.Get("Art/PCInventoryUI/logo");
        if(logoSprite==null)
        {
            var logoTexture=Resources.Load<Texture2D>("Art/PCInventoryUI/logo");
            if(logoTexture!=null && refPcLogo==null)
                refPcLogo=Sprite.Create(logoTexture,new Rect(0,0,logoTexture.width,logoTexture.height),new Vector2(.5f,.5f),100f);
            logoSprite=refPcLogo;
        }
        if(logoSprite!=null)
        {
            var logo=RefPcRect(stage,"PC_Logo",736,12,200,80);
            var logoImage=logo.gameObject.AddComponent<Image>();logoImage.sprite=logoSprite;logoImage.preserveAspect=true;logoImage.raycastTarget=false;
        }

        // Areas clicaveis acompanham os corpos. O destaque do principal e desenhado por
        // SurvivorHighlight (halo + anel no chao + seta). Nenhum texto flutuante no cenario:
        // nivel e poder do principal ficam no perfil (canto superior esquerdo).
        string[] ids={"assaulter","sniper","medic","engineer","explorer"};
        Rect[] bodies={
            new Rect(235,398,230,315),
            new Rect(414,350,222,330),
            new Rect(624,375,184,310),
            new Rect(918,378,242,318),
            new Rect(1190,388,238,310)
        };
        for(int i=0;i<ids.Length;i++)
        {
            string id=ids[i];var body=bodies[i];
            var highlightRect=RefPcRect(stage,"PC_SurvivorHighlight_"+id,
                body.x-SurvivorHighlight.PadX,body.y-SurvivorHighlight.PadTop,
                body.width+SurvivorHighlight.PadX*2f,body.height+SurvivorHighlight.PadTop+SurvivorHighlight.PadBottom);
            var highlight=highlightRect.gameObject.AddComponent<SurvivorHighlight>();
            highlight.SetBody(body.width,body.height);
            highlight.raycastTarget=false;
            highlight.gameObject.SetActive(false);
            refPcBorders[id]=highlight;

            // Etiqueta discreta acima da setinha: Classe + Nivel + Poder (so do selecionado).
            var label=RefPcText(stage,"PC_SurvivorLabel_"+id,"",body.x-40f,body.y-46f,body.width+80f,20f,homeLabelSize,
                new Color(0.96f,0.88f,0.68f,homeLabelOpacity),TextAlignmentOptions.Center);
            label.fontStyle=FontStyles.Normal;label.gameObject.SetActive(false);
            refPcLabels[id]=label;

            var area=RefPcRect(stage,"PC_Survivor_"+id,body.x,body.y,body.width,body.height);
            RefPcHit(area,()=>RefPcChooseSurvivor(id),false);
        }

        // Perfil de jogador: frame no aspect ratio nativo (832x133).
        var profile=RefPcImage(stage,"PC_Profile","profile",16,16,560,89.5f);
        RefPcHit(profile.rectTransform,()=>{OpenFromHome(()=>{newGameMode=false;ShowSelect();});},false);
        refPcPortrait=RefPcImage(profile.transform,"Portrait","portrait_assaulter",12,6,78,78);refPcPortrait.preserveAspect=true;
        refPcNick=RefPcText(profile.transform,"Nick","",108,6,435,25,25,new Color(0.94f,0.88f,0.69f));
        refPcClass=RefPcText(profile.transform,"Class","",108,31,435,20,17,new Color(0.65f,0.77f,0.83f));
        RefPcSolid(profile.transform,"XpTrack",new Color(0.15f,0.20f,0.23f),108,55,430,7);
        refPcXp=RefPcSolid(profile.transform,"XpFill",new Color(0.91f,0.62f,0.17f),108,55,430,7);
        refPcPower=RefPcText(profile.transform,"Power","",108,64,435,18,17,new Color(0.93f,0.83f,0.61f));

        // Carteira e engrenagem com proporcao original dos assets.
        var resources=RefPcImage(stage,"PC_Resources","resources",1245,18,350,65.6f);
        refPcGold=RefPcText(resources.transform,"Gold","",64,10,94,46,24,new Color(0.94f,0.87f,0.68f),TextAlignmentOptions.Center);
        refPcScrap=RefPcText(resources.transform,"Scrap","",242,10,94,46,24,new Color(0.94f,0.87f,0.68f),TextAlignmentOptions.Center);
        RefPcButton(stage,"settings",1578,14,77,74,RefPcOpenOptions);
        RefPcButton(stage,"gifts",15,259,189,75,()=>ClaimWeeklyGiftFromHome(stage));
        BindWeeklyGiftButton(stage);
        RefPcButton(stage,"events",15,177,189,74,()=>Toast("Eventos: em breve."));
        // Esquerda: Loja, Sobreviventes, Habilidades, Mochila.
        RefPcButton(stage,"shop",15,762,181,162,()=>OpenFromHome(ShowShop));
        RefPcButton(stage,"survivors",204,762,181,165,()=>OpenFromHome(()=>{newGameMode=false;ShowSelect();}));
        RefPcButton(stage,"skills",393,762,181,167,()=>OpenFromHome(()=>ShowMapSkills(null)));
        RefPcButton(stage,"inventory",582,762,181,168,()=>OpenFromHome(()=>ShowMapInventory(null,true)));
        // Direita (mesmo tamanho dos demais, 181x168): Treinamento, Missoes, Campanha.
        RefPcButton(stage,"training",1096,762,181,168,OpenRangePicker);
        RefPcButton(stage,"missions_square",1285,762,181,168,ShowGameModes);
        RefPcButton(stage,"campaign_square",1474,762,181,168,()=>{homeOrigin=false;ShowMap();});

        var badge=RefPcSolid(stage,"PC_SkillPointsBadge",new Color(0.92f,0.63f,0.14f),549,755,25,25);
        RefPcText(badge.transform,"Mark","!",0,0,25,25,19,Color.black,TextAlignmentOptions.Center);refPcSkillBadge=badge.gameObject;
        var tick=stage.gameObject.AddComponent<ReferenceHomeRefresh>();
        tick.refresh=RefreshPcReferenceHome;
        tick.escape=GoToTitleScreenKeepingHome;
        Show("home");fit.Apply();RefreshPcReferenceHome();
    }

    void RefreshPcReferenceHome()
    {
        var save=GameSession.Save;if(save==null || refPcNick==null) return;
        var def=GameDatabase.GetSurvivor(save.selectedSurvivor);if(def==null)return;
        var p=save.GetProgress(def.id);
        string nick=TruncateNick(GetPlayerNickname(save),18).ToUpperInvariant();
        if(refPcNick.text!=nick) refPcNick.text=nick;
        string identity=def.name.ToUpperInvariant()+" · NV. "+p.level;
        if(refPcClass.text!=identity)refPcClass.text=identity;
        refPcPower.text="PODER "+PowerRating.Of(save,def.id);
        refPcGold.text=save.gold.ToString("N0");refPcScrap.text=save.scrap.ToString("N0");
        float ratio=Mathf.Clamp01((float)p.xp/Mathf.Max(1,PlayerStats.XpForLevel(p.level)));
        refPcXp.rectTransform.sizeDelta=new Vector2(430f*ratio,7f);
        var portrait=def.id=="assaulter"?RefPcSprite("portrait_assaulter"):HubPortrait(def);
        if(portrait!=null)refPcPortrait.sprite=portrait;
        if(refPcSkillBadge)refPcSkillBadge.SetActive(HomeAnySkillPoints(save));
        RefPcApplyStrongSelection();
        RefPcUpdateLabels(save);
        RefreshWeeklyGiftVisibility(); // WEEKLY_GIFTS_REFRESH_V1
    }

    void RefPcUpdateLabels(SaveData save)
    {
        foreach(var pair in refPcBorders)
        {
            var h=pair.Value;if(h==null)continue;
            h.auraIntensity=homeAuraIntensity;h.auraSizeX=homeAuraSizeX;h.auraSizeY=homeAuraSizeY;
            h.auraPulseAmount=homeAuraPulseAmount;h.auraPulseSpeed=homeAuraPulseSpeed;
            h.auraColor=homeAuraColor;h.showArrow=homeShowArrow;h.SetVerticesDirty();
        }
        foreach(var pair in refPcLabels)
        {
            var t=pair.Value;if(t==null)continue;
            bool on=homeShowLabel && save.selectedSurvivor==pair.Key;
            if(t.gameObject.activeSelf!=on)t.gameObject.SetActive(on);
            if(!on)continue;
            var d=GameDatabase.GetSurvivor(pair.Key);if(d==null)continue;
            var pr=save.GetProgress(pair.Key);
            t.fontSizeMax=homeLabelSize;t.fontSize=homeLabelSize;
            var c=t.color;c.a=homeLabelOpacity;t.color=c;
            string txt=d.name.ToUpperInvariant()+"  ·  NV. "+pr.level+"  ·  "+PowerRating.Of(save,pair.Key);
            if(t.text!=txt)t.text=txt;
        }
    }

    void RefPcChooseSurvivor(string id)
    {
        var save=GameSession.Save;
        if(save==null || GameDatabase.GetSurvivor(id)==null)return;
        if(save.selectedSurvivor==id)
        {
            RefreshPcReferenceHome();
            return;
        }
        // Um clique seleciona imediatamente: retrato, classe, nível, poder e contorno
        // são atualizados juntos pelo fluxo já usado pelo restante do menu.
        SelectHomeSurvivor(id);
    }

    void RefPcOpenOptions()
    {
        RefreshOptions();Show("options");
        // Preserve the correct return target for this entry, with restoration on close.
        if(screens.TryGetValue("options",out var options))
        {
            foreach(var button in options.GetComponentsInChildren<Button>(true))
            {
                if(button.name!="BtnBack")continue;
                var original=button.onClick;
                button.onClick=new Button.ButtonClickedEvent();
                button.onClick.AddListener(()=> {button.onClick=original;ShowHome();});
                break;
            }
        }
    }
}
