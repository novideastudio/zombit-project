using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Daily store, PT-BR. All offer/transaction authority is in TimedCommerce.
public partial class MainMenuController
{
    int dailyShopCategory=-1;
    static Sprite dailyShopPanel,dailyShopRed;
    static readonly CultureInfo ShopCulture=CultureInfo.GetCultureInfo("pt-BR");
    Sprite DailyShopArt(string key)
    {
        if(key=="panel" && dailyShopPanel!=null)return dailyShopPanel;
        if(key=="red" && dailyShopRed!=null)return dailyShopRed;
        var tex=Resources.Load<Texture2D>("Art/PCInventoryUI/"+key);if(tex==null)return null;
        var sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        if(key=="panel")dailyShopPanel=sprite;else dailyShopRed=sprite;return sprite;
    }
    RectTransform DailyShopRect(Transform p,string name,float x,float y,float w,float h)
    {var r=UIFactory.Node(name,p);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
    RectTransform DailyShopBox(Transform p,string name,float x,float y,float w,float h)
    {
        var r=DailyShopRect(p,name,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.sprite=DailyShopArt("panel");im.type=Image.Type.Sliced;im.color=im.sprite!=null?Color.white:new Color(.025f,.045f,.06f);im.raycastTarget=false;return r;
    }
    TMP_Text DailyShopText(Transform p,string text,float x,float y,float w,float h,float size=23,Color? color=null)
    {
        var rt=DailyShopRect(p,"Text",x,y,w,h);var t=UIFactory.Txt(rt,text,size,TextAlignmentOptions.TopLeft,color??Color.white,"Label");UIFactory.Stretch(t.rectTransform);
        t.enableAutoSizing=true;t.fontSizeMin=12;t.fontSizeMax=size;t.textWrappingMode=TextWrappingModes.Normal;t.raycastTarget=false;return t;
    }
    Button DailyShopButton(Transform p,string text,float x,float y,float w,float h,Action action,bool red=false)
    {
        var rt=DailyShopRect(p,text,x,y,w,h);var b=UIFactory.Btn(rt,text,22,action,null,text+"Button");UIFactory.Stretch((RectTransform)b.transform);
        var im=b.GetComponent<Image>();var sp=DailyShopArt(red?"red":"panel");if(im!=null && sp!=null){im.sprite=sp;im.type=Image.Type.Sliced;}
        var c=b.colors;c.normalColor=Color.white;c.selectedColor=Color.white;c.highlightedColor=new Color(1,.92f,.7f);b.colors=c;
        var t=b.GetComponentInChildren<TMP_Text>();if(t!=null){t.enableAutoSizing=true;t.fontSizeMin=11;t.fontSizeMax=22;}return b;
    }
    void ShowShop()
    {
        var save=GameSession.Save;if(save==null){Show("main");return;}
        var screen=Rebuild("shop");var stage=UIFactory.Node("ShopStage",screen);stage.sizeDelta=new Vector2(1920,1080);
        var fit=stage.gameObject.AddComponent<CampaignPcFit>();fit.content=stage;fit.viewport=screen;fit.Apply();
        var bg=UIFactory.Img(stage,new Color(.03f,.045f,.06f),"ShopBackground");UIFactory.Stretch(bg.rectTransform);bg.raycastTarget=false;
        DailyShopBox(stage,"ShopHeader",16,12,1888,92);
        DailyShopText(stage,"ARSENAL TÁTICO · LOJA DIÁRIA",38,28,940,48,36,UIKit.Col.Gold);
        DailyShopText(stage,"OURO  "+save.gold.ToString("N0",ShopCulture)+"    |    SUCATA  "+save.scrap.ToString("N0",ShopCulture),1060,34,566,42,25,UIKit.Col.Gold);
        DailyShopButton(stage,"VOLTAR",1650,24,230,66,ReturnFromUtility,true);
        CommerceStock stock;
        try{stock=TimedCommerce.EnsureDaily(save);}
        catch(Exception e){DailyShopText(stage,e.Message+"\nInstale: ZomBit > Loja > Instalar loja e presentes",40,155,1810,240,28);Show("shop");return;}
        var actor=GameDatabase.GetSurvivor(save.selectedSurvivor);
        DailyShopText(stage,actor.name+" · NÍVEL "+TimedCommerce.Level(save,save.selectedSurvivor)+"   |   ESTOQUE "+stock.day+
            "   |   RENOVA ÀS 00H UTC",38,116,1400,40,22);
        int pending=TimedCommerce.Read(save).pending.Count;
        DailyShopButton(stage,"RESGATAR PENDENTES ("+pending+")",1450,112,430,48,()=>{Toast(TimedCommerce.ClaimPending(GameSession.Save));ShowShop();});
        string[] names={"TODOS","MELHORIAS","CAPACETES","COLETES","CALÇAS","BOTAS","MOCHILAS"};
        int[] ids={-1,(int)ItemSlot.Arma,(int)ItemSlot.Capacete,(int)ItemSlot.Colete,(int)ItemSlot.Calca,(int)ItemSlot.Botas,(int)ItemSlot.Mochila};
        for(int i=0;i<ids.Length;i++)
        {int cat=ids[i];DailyShopButton(stage,names[i],30+i*266,176,254,52,()=>{dailyShopCategory=cat;ShowShop();},dailyShopCategory==cat);}
        int shown=0;string uid=ZombitCloud.Uid;
        foreach(var offer in stock.offers)
        {
            if(offer==null || offer.item==null || (dailyShopCategory>=0 && (int)offer.item.slot!=dailyShopCategory))continue;
            int n=shown++;float x=30+(n%3)*626,y=250+(n/3)*382;
            var card=DailyShopBox(stage,"DailyOffer_"+offer.item.uid,x,y,608,360);
            var icon=DailyShopRect(card,"IconTile",18,24,120,120);ItemTileStyling.Apply(icon,offer.item);
            var image=UIFactory.Img(icon,Color.white,"Icon");UIFactory.Stretch(image.rectTransform,.08f,.08f,.92f,.92f);image.sprite=ItemVisual.Icon(offer.item);image.preserveAspect=true;image.raycastTarget=false;
            DailyShopText(card,offer.item.name,154,24,432,45,29,RarityColors.Get(offer.item.rarity));
            DailyShopText(card,DailyShopRarityLabel(offer.item.rarity)+"  ·  NV. "+offer.level,154,78,432,32,21,UIKit.Col.Gold);
            DailyShopText(card,"Requer nível "+offer.level+". "+(offer.item.slot==ItemSlot.Arma?"Melhoria da arma da classe.":"Equipamento do sobrevivente."),154,119,432,64,20);
            string stats="DANO +"+offer.item.bonusDamage.ToString("0.#",ShopCulture)+"   VIDA +"+offer.item.bonusHealth.ToString("0.#",ShopCulture)+"   DEFESA +"+offer.item.bonusDefense.ToString("0.#",ShopCulture);
            if(offer.item.affixes!=null)foreach(var a in offer.item.affixes)if(a!=null)stats+="\n"+a.Describe();
            DailyShopText(card,stats,22,194,564,68,22);
            DailyShopText(card,offer.price.ToString("N0",ShopCulture)+" OURO",22,290,300,42,29,UIKit.Col.Gold);
            var captured=offer;
            var buy=DailyShopButton(card,offer.sold?"VENDIDO":"COMPRAR",348,282,238,58,()=>
            {
                if(GameSession.Save!=save || ZombitCloud.Uid!=uid){Toast("A sessão mudou. Reabra a loja.");return;}
                try{Toast(TimedCommerce.Buy(save,stock.survivor,stock.day,captured.item.uid));}catch(Exception e){Toast(e.Message);Debug.LogError(e);}ShowShop();
            },true);
            buy.interactable=!offer.sold && save.gold>=offer.price && offer.level<=TimedCommerce.Level(save,stock.survivor);
        }
        if(shown==0)DailyShopText(stage,"Nenhuma oferta nesta categoria hoje.",80,300,1760,90,32);
        DailyShopText(stage,"Cada oferta pode ser comprada uma vez. Subir de nível não sorteia outro estoque no mesmo dia.\nPresentes semanais: segunda-feira às 00h UTC. Horário local do aparelho não é validação de servidor.",36,1014,1820,54,19,new Color(.65f,.75f,.8f));
        Show("shop");fit.Apply();
    }
    // Compatibility with any older editor-bound shop callbacks; still uses saved daily stock.
    void BuyShopOffer(int index)
    {
        var save=GameSession.Save;if(save==null)return;
        try{var stock=TimedCommerce.EnsureDaily(save);if(index<0 || index>=stock.offers.Count)return;
            Toast(TimedCommerce.Buy(save,stock.survivor,stock.day,stock.offers[index].item.uid));ShowShop();}
        catch(Exception e){Toast(e.Message);Debug.LogError(e);}
    }
    static string DailyShopRarityLabel(Rarity r)
    {switch(r){case Rarity.Comum:return "COMUM";case Rarity.Incomum:return "INCOMUM";case Rarity.Raro:return "RARO";case Rarity.Epico:return "ÉPICO";default:return "LENDÁRIO";}}
}
