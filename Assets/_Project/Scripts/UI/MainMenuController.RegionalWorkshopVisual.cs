using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class MainMenuController
{
    RectTransform regionalRewardCard;
    Image regionalRewardIcon;
    TMP_Text regionalRewardName,regionalRewardProgress;
    void BuildRegionalInventoryEntry()
    {
        if(sharedInventoryView==null || sharedInventoryView.Stage==null)return;
        var stage=sharedInventoryView.Stage;
        if(stage.Find("RegionalWorkshopTab")!=null)return;
        var title=stage.Find("Title") as RectTransform;if(title!=null)title.sizeDelta=new Vector2(640,title.sizeDelta.y);
        var b=UIFactory.Btn(stage,"OFICINA REGIONAL",20,OpenRegionalFromInventory,null,"RegionalWorkshopTab");
        TacticalInventoryView.Place(b.transform,940,26,232,54);
        var image=b.GetComponent<Image>();if(image!=null){try{image.sprite=PcInvSprite("panel",true);image.type=Image.Type.Sliced;}catch(System.Exception e){Debug.LogWarning("Oficina: usando estilo genérico. "+e.Message);}}
        var colors=b.colors;colors.normalColor=Color.white;colors.selectedColor=Color.white;colors.highlightedColor=new Color(1,.9f,.6f);b.colors=colors;
        var label=b.GetComponentInChildren<TMP_Text>();if(label!=null){label.color=UIKit.Col.Gold;label.enableAutoSizing=true;label.fontSizeMin=11;label.fontSizeMax=20;}
    }
    void OpenRegionalFromInventory()
    {
        var fit=sharedInventoryView!=null?sharedInventoryView.Fit:null;
        bool enabledBefore=fit!=null && fit.enabled;if(fit!=null)fit.enabled=false;
        string survivor=invSurvivorId,selected=mapInventorySelectedUid;int category=invCategory;var sort=invSort;
        RegionalRewardsUI.OpenForRegion(selectedLoc!=null?selectedLoc.id:null,()=>
        {
            if(fit!=null)fit.enabled=enabledBefore;
            if(this==null || GameSession.Save==null)return;
            invSurvivorId=survivor;invCategory=category;invSort=sort;
            ShowMapInventory(selected,false);
        });
    }
    void BuildRegionalCampaignEntry()
    {
        if(dStart==null || selectedLoc==null)return;
        var parent=dStart.transform.parent;
        foreach(string n in new[]{"ItemsTitle","ItemRow"}){var t=parent.Find(n);if(t!=null)t.gameObject.SetActive(false);}
        if(regionalRewardCard==null || regionalRewardCard.parent!=parent)
        {
            var existing=parent.Find("RegionalRewardCard");if(existing!=null){existing.gameObject.SetActive(false);Destroy(existing.gameObject);}
            regionalRewardCard=UIKit.Frame(parent,"RegionalRewardCard",new Color(.025f,.045f,.06f,1),UIFactory.Gold,1.5f);
            PlaceRegional(regionalRewardCard,20,578,550,118);
            regionalRewardIcon=UIFactory.Img(regionalRewardCard,Color.white,"ExclusiveIcon");PlaceRegional(regionalRewardIcon.rectTransform,10,22,66,66);regionalRewardIcon.preserveAspect=true;regionalRewardIcon.raycastTarget=false;
            var heading=UIFactory.Txt(regionalRewardCard,"EXCLUSIVO DA REGIÃO",15,TextAlignmentOptions.MidlineLeft,UIFactory.Gold,"Title");PlaceRegional(heading.rectTransform,88,7,440,22);heading.raycastTarget=false;
            regionalRewardName=UIFactory.Txt(regionalRewardCard,"",20,TextAlignmentOptions.MidlineLeft,Color.white,"ItemName");PlaceRegional(regionalRewardName.rectTransform,88,31,440,28);regionalRewardName.enableAutoSizing=true;regionalRewardName.fontSizeMin=12;regionalRewardName.fontSizeMax=20;regionalRewardName.raycastTarget=false;
            regionalRewardProgress=UIFactory.Txt(regionalRewardCard,"",15,TextAlignmentOptions.MidlineLeft,Color.white,"Progress");PlaceRegional(regionalRewardProgress.rectTransform,88,64,274,40);regionalRewardProgress.enableAutoSizing=true;regionalRewardProgress.fontSizeMin=10;regionalRewardProgress.fontSizeMax=15;regionalRewardProgress.raycastTarget=false;
            var b=UIFactory.Btn(regionalRewardCard,"VER NA OFICINA",16,OpenRegionalFromCampaign,null,"ViewRegionalReward");PlaceRegional((RectTransform)b.transform,366,67,172,38);
            var image=b.GetComponent<Image>();if(image!=null){try{image.sprite=PcInvSprite("panel",true);image.type=Image.Type.Sliced;}catch(System.Exception e){Debug.LogWarning("Oficina: usando estilo genérico. "+e.Message);}}
        }
        UpdateRegionalCampaignEntry();
    }
    static void PlaceRegional(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    void UpdateRegionalCampaignEntry()
    {
        if(regionalRewardCard==null || selectedLoc==null)return;
        var r=RegionalRewards.Get(selectedLoc.id);
        if(r==null){regionalRewardCard.gameObject.SetActive(false);return;}
        regionalRewardCard.gameObject.SetActive(true);
        regionalRewardIcon.sprite=r.icon!=null?r.icon:ItemVisual.IconForSlot(r.slot);regionalRewardName.text=r.itemName;
        if(!RegionalRewards.Installed || GameSession.Save==null){regionalRewardProgress.text="Instale recompensas regionais";return;}
        try
        {
            var record=RegionalRewards.Record(RegionalRewards.Read(GameSession.Save),r.id);
            regionalRewardProgress.text="Selos: "+record.seals+"/"+r.sealsCost+"\nProjeto: "+(record.blueprint?"desbloqueado":"vença no Normal+");
        }
        catch(System.Exception e){regionalRewardProgress.text="Não foi possível ler o progresso";Debug.LogError(e);}
    }
    void OpenRegionalFromCampaign()
    {
        if(selectedLoc==null)return;
        var updater=pcCampaignStage!=null?pcCampaignStage.GetComponent<CampaignPcRefresh>():null;
        bool enabledBefore=updater!=null && updater.enabled;if(updater!=null)updater.enabled=false;
        RegionalRewardsUI.OpenForRegion(selectedLoc.id,()=>
        {
            if(updater!=null)updater.enabled=enabledBefore;
            if(this==null || GameSession.Save==null)return;
            RefreshDifficultyUI();RefreshPcCampaignWallet();UpdateRegionalCampaignEntry();
        });
    }
}
