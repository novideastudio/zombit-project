using TMPro;
using UnityEngine;

public partial class MainMenuController
{
    Canvas progressionWarning;
    string progressionAccepted;
    string ProgressionSelectionKey()=>selectedLoc.id+"#"+selectedDiff+"#"+JsonUtility.ToJson(GameSession.Save);
    bool ConfirmProgressionRisk()
    {
        if(selectedLoc==null || GameSession.Save==null)return false;
        string key=ProgressionSelectionKey();
        if(progressionAccepted==key){progressionAccepted=null;return true;}
        progressionAccepted=null;
        int have=ProgressionRuntime.TeamPower(GameSession.Save,selectedDiff),need=ProgressionRuntime.Recommended(selectedLoc,selectedDiff);
        if(have>=need*ProgressionRuntime.Config.highBelow)return true;
        if(progressionWarning!=null)return false;
        progressionWarning=UIFactory.MakeCanvas("ProgressionRiskWarning",650);
        var dim=UIFactory.Img(progressionWarning.transform,new Color(0,0,0,.85f),"Dim");UIFactory.Stretch(dim.rectTransform);
        var p=UIKit.Frame(progressionWarning.transform,"Warning",new Color(.07f,.03f,.035f,1),UIFactory.Gold,2);
        UIFactory.Place(p,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1100,480));
        var t=UIFactory.Txt(p,selectedLoc.name+" · "+Difficulties.Get(selectedDiff).name+"\n\n"+
            ProgressionRuntime.Describe(GameSession.Save,selectedLoc,selectedDiff)+
            "\n\nVocê pode tentar. Aplicam-se as penalidades da dificuldade e retenção parcial de sucata em derrota/abandono.",27,TextAlignmentOptions.Center,Color.white,"WarningText");
        UIFactory.Stretch(t.rectTransform,.04f,.28f,.96f,.94f);
        var yes=UIFactory.Btn(p,"TENTAR MESMO ASSIM",24,()=>
        {
            Destroy(progressionWarning.gameObject);progressionWarning=null;
            if(selectedLoc==null || ProgressionSelectionKey()!=key){Toast("Seleção mudou. Confira o risco novamente.");return;}
            progressionAccepted=key;OnStartMission();
        },UIFactory.Red,"Continue");
        UIFactory.Stretch((RectTransform)yes.transform,.06f,.06f,.49f,.22f);
        var no=UIFactory.Btn(p,"VOLTAR E MELHORAR EQUIPE",23,()=>{Destroy(progressionWarning.gameObject);progressionWarning=null;},null,"Cancel");
        UIFactory.Stretch((RectTransform)no.transform,.51f,.06f,.94f,.22f);
        return false;
    }
    void RefreshProgressionPowerLine()
    {
        if(dInfo==null || selectedLoc==null || GameSession.Save==null)return;
        int have=ProgressionRuntime.TeamPower(GameSession.Save,selectedDiff),need=ProgressionRuntime.Recommended(selectedLoc,selectedDiff);
        dInfo.text="PODER "+have+" / "+need+" · "+ProgressionRuntime.Risk(have,need);
        dInfo.color=have>=need?new Color(.45f,1,.55f):have>=need*.7f?UIFactory.Gold:new Color(1,.35f,.3f);
        var rt=dInfo.rectTransform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(24,-322);rt.sizeDelta=new Vector2(540,28);
        dInfo.enableAutoSizing=true;dInfo.fontSizeMin=11;dInfo.fontSizeMax=17;
        dInfo.textWrappingMode=TextWrappingModes.NoWrap;
    }
}
