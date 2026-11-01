using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Formation screen: stored order is independent from mission participation.
// Uses existing save fields and mission limit (two; three on Nightmare).
public partial class MainMenuController
{
    Canvas missionFormationCanvas;
    readonly List<string> missionFormationIds = new List<string>();
    readonly Dictionary<string, Button> missionFormationButtons = new Dictionary<string, Button>();
    TMP_Text missionFormationSummary;
    Button missionFormationConfirm;
    int MissionFormationLimit => selectedDiff >= HeroSquad.TrioDifficulty ? 3 : 2;
    RectTransform formationSlots, formationRoster;
    TMP_Text formationTeamInfo;
    int formationSelectedSeat = -1;
    static readonly Color FormationGold = new Color(.95f,.70f,.23f);
    static readonly Color FormationDark = new Color(.025f,.045f,.055f,.99f);

    void AddMissionFormationButton() { /* PC campaign owns the entry button. */ }

    RectTransform FormationRect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rt = UIFactory.Node(name,parent);
        rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
        rt.anchoredPosition=new Vector2(x,-y); rt.sizeDelta=new Vector2(w,h);
        return rt;
    }
    TMP_Text FormationText(Transform p,string name,string text,float x,float y,float w,float h,float size,Color color)
    {
        var rt=FormationRect(p,name,x,y,w,h);
        var t=UIFactory.Txt(rt,text,size,TextAlignmentOptions.MidlineLeft,color,"Text");
        UIFactory.Stretch(t.rectTransform);
        t.fontStyle=FontStyles.Bold;t.enableAutoSizing=true;
        t.fontSizeMin=12;t.fontSizeMax=size;t.textWrappingMode=TextWrappingModes.Normal;
        t.raycastTarget=false; return t;
    }
    RectTransform FormationPanel(Transform p,string name,float x,float y,float w,float h,Color border)
    {
        var holder=FormationRect(p,name,x,y,w,h);
        var frame=UIKit.Frame(holder,"Frame",FormationDark,border,2);
        UIFactory.Stretch(frame);return holder;
    }
    Button FormationButton(Transform p,string name,string label,float x,float y,float w,float h,System.Action action,Color color)
    {
        var rt=FormationRect(p,name,x,y,w,h);
        var b=UIFactory.Btn(rt,label,23,action,color,name+"Button");
        UIFactory.Stretch((RectTransform)b.transform);
        var text=b.GetComponentInChildren<TMP_Text>();
        if(text){text.enableAutoSizing=true;text.fontSizeMin=12;text.fontSizeMax=23;}
        return b;
    }
    void CloseMissionFormation()
    {
        if(missionFormationCanvas) Destroy(missionFormationCanvas.gameObject);
        missionFormationCanvas=null;formationSelectedSeat=-1;
    }
    void OpenMissionFormation()
    {
        var save=GameSession.Save;
        if(save==null || selectedLoc==null || missionFormationCanvas!=null)return;
        missionFormationIds.Clear();missionFormationButtons.Clear();formationSelectedSeat=-1;
        foreach(var id in new[]{save.selectedSurvivor,save.selectedPartner,save.selectedThird})
            if(FormationDef(id)!=null && !missionFormationIds.Contains(id))missionFormationIds.Add(id);
        missionFormationCanvas=UIFactory.MakeCanvas("MissionFormationPicker",200);
        var shade=UIFactory.Img(missionFormationCanvas.transform,new Color(0,0,0,.96f),"Dim");
        UIFactory.Stretch(shade.rectTransform);shade.raycastTarget=true;
        var stage=FormationRect(missionFormationCanvas.transform,"FormationStage",0,0,1920,1080);
        stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(.5f,.5f);stage.anchoredPosition=Vector2.zero;
        var fit=stage.gameObject.AddComponent<ZomBitFormationStageFit>();fit.stage=stage;
        fit.viewport=(RectTransform)missionFormationCanvas.transform;fit.Apply();
        FormationText(stage,"Campaign","CAMPANHA  /  CAPÍTULO 1 · REGIÃO URBANA",32,12,1160,48,26,FormationGold);
        FormationText(stage,"Wallet",save.gold.ToString("N0")+" OURO    |    "+save.scrap.ToString("N0")+" SUCATA",1260,12,620,48,25,Color.white);
        FormationPanel(stage,"OuterFrame",20,74,1880,986,FormationGold);
        FormationText(stage,"Heading","FORMAÇÃO DA MISSÃO",540,86,1100,66,45,FormationGold);
        var location=FormationPanel(stage,"Location",40,174,400,382,FormationGold);
        FormationText(location,"LocationName",MapDisplayNumber(selectedLoc).ToString("00")+"  "+selectedLoc.name.ToUpperInvariant(),18,12,364,48,29,Color.white);
        var image=UIFactory.Img(location,Color.white,"Preview");
        image.sprite=ArtLoader.Get(CampaignThumbnailPath(selectedLoc));
        if(image.sprite==null)image.sprite=ArtLoader.Get(SceneBackgrounds.PathFor(selectedLoc));
        var ir=image.rectTransform;ir.anchorMin=ir.anchorMax=ir.pivot=new Vector2(0,1);
        ir.anchoredPosition=new Vector2(18,-70);ir.sizeDelta=new Vector2(364,174);image.raycastTarget=false;
        if(image.sprite==null)image.color=new Color(.1f,.15f,.18f);
        FormationText(location,"Rules","DIFICULDADE: "+Difficulties.Get(selectedDiff).name.ToUpperInvariant()+
            "\nMÁX. NESTA MISSÃO: "+MissionFormationLimit+"\nOBJETIVO: ELIMINAR AS HORDAS",18,255,365,114,23,FormationGold);
        formationSlots=FormationRect(stage,"Seats",470,174,1080,382);
        var team=FormationPanel(stage,"Team",1570,174,310,382,FormationGold);
        missionFormationSummary=FormationText(team,"Count","",18,16,274,104,27,FormationGold);
        formationTeamInfo=FormationText(team,"TeamInfo","",18,130,274,232,22,Color.white);
        FormationText(stage,"AvailableTitle","SOBREVIVENTES DISPONÍVEIS",48,572,950,50,31,FormationGold);
        FormationText(stage,"Instructions","Escolha uma posição e depois um sobrevivente. Clique em outra posição ocupada para trocar.",1000,572,870,50,19,Color.white);
        formationRoster=FormationRect(stage,"Available",48,640,1360,270);
        var help=FormationPanel(stage,"PositionHelp",1440,640,438,270,FormationGold);
        FormationText(help,"Help","POSIÇÕES NA FORMAÇÃO\n\nFRENTE — recebe primeiro o dano direto.\nMEIO — segundo na ordem.\nRETAGUARDA — terceiro na ordem.\n\nA posição não concede bônus de atributos.",18,12,402,246,22,Color.white);
        FormationButton(stage,"Cancel","CANCELAR",390,960,480,70,CloseMissionFormation,new Color(.14f,.22f,.27f));
        missionFormationConfirm=FormationButton(stage,"Confirm","CONFIRMAR FORMAÇÃO",900,960,660,70,ConfirmMissionFormation,new Color(.65f,.07f,.055f));
        RefreshMissionFormation();
    }
    SurvivorDef FormationDef(string id)
    {
        if(string.IsNullOrEmpty(id))return null;
        foreach(var d in GameDatabase.Survivors)if(d.id==id)return d;
        return null;
    }
    void FormationClear(Transform t)
    {
        for(int i=t.childCount-1;i>=0;i--)
        {var child=t.GetChild(i);child.gameObject.SetActive(false);Destroy(child.gameObject);}
    }
    void FormationPickSeat(int index)
    {
        if(formationSelectedSeat>=0 && formationSelectedSeat<missionFormationIds.Count &&
            index<missionFormationIds.Count && index!=formationSelectedSeat)
        {
            string temp=missionFormationIds[index];missionFormationIds[index]=missionFormationIds[formationSelectedSeat];
            missionFormationIds[formationSelectedSeat]=temp;formationSelectedSeat=-1;
        }
        else formationSelectedSeat=index;
        RefreshMissionFormation();
    }
    void FormationRemove(int index)
    {
        if(index<missionFormationIds.Count)missionFormationIds.RemoveAt(index);
        formationSelectedSeat=-1;RefreshMissionFormation();
    }
    void ToggleMissionFormation(string id)
    {
        if(FormationDef(id)==null)return;
        int existing=missionFormationIds.IndexOf(id);
        if(formationSelectedSeat>=0 && formationSelectedSeat<missionFormationIds.Count)
        {
            if(existing>=0)
            {
                string old=missionFormationIds[formationSelectedSeat];
                missionFormationIds[formationSelectedSeat]=id;missionFormationIds[existing]=old;
            }
            else missionFormationIds[formationSelectedSeat]=id;
        }
        else if(existing<0 && missionFormationIds.Count<3)missionFormationIds.Add(id);
        else if(existing>=0)formationSelectedSeat=existing;
        else {Toast("Selecione a posição que deseja substituir.");return;}
        if(existing<0 || formationSelectedSeat<missionFormationIds.Count)formationSelectedSeat=-1;
        RefreshMissionFormation();
    }
    void FormationPortrait(Transform p,SurvivorDef d,float x,float y,float w,float h)
    {
        var rt=FormationRect(p,"Portrait",x,y,w,h);
        var img=rt.gameObject.AddComponent<Image>();img.sprite=HubPortrait(d);
        img.preserveAspect=true;img.raycastTarget=false;
        img.color=img.sprite!=null?Color.white:d.color;
    }
    void RefreshMissionFormation()
    {
        if(formationSlots==null || formationRoster==null)return;
        FormationClear(formationSlots);FormationClear(formationRoster);missionFormationButtons.Clear();
        string[] roles={"FRENTE","MEIO","RETAGUARDA"};
        for(int i=0;i<3;i++)
        {
            int seat=i;bool inactive=i>=MissionFormationLimit;
            var panel=FormationPanel(formationSlots,"Seat_"+i,i*360,0,342,382,
                formationSelectedSeat==i?Color.cyan:FormationGold);
            FormationText(panel,"Role",roles[i],16,8,310,44,30,FormationGold);
            FormationButton(panel,"SelectSeat","",10,60,322,264,()=>FormationPickSeat(seat),new Color(.07f,.10f,.12f));
            if(i<missionFormationIds.Count)
            {
                var d=FormationDef(missionFormationIds[i]);
                if(d!=null)
                {
                    FormationPortrait(panel,d,18,66,306,206);
                    FormationText(panel,"Name",d.name.ToUpperInvariant()+"  NV. "+GameSession.Save.GetProgress(d.id).level,20,272,298,48,25,Color.white);
                }
            }
            else FormationText(panel,"Empty","+\nSELECIONAR",64,122,230,120,28,FormationGold);
            if(inactive)
            {
                // Overlay is visual only: the reserved seat remains editable.
                var dim=UIFactory.Img(panel,new Color(0,0,0,.55f),"InactiveTint");
                UIFactory.Stretch(dim.rectTransform,0,.15f,1,.84f);dim.raycastTarget=false;
            }
            if(i<missionFormationIds.Count)
                FormationButton(panel,"Remove","X",286,64,42,38,()=>FormationRemove(seat),new Color(.55f,.06f,.04f));
            FormationText(panel,"Participation",inactive?"FORA DESTA MISSÃO\nSua escolha permanece salva":"PARTICIPA DESTA MISSÃO",14,330,314,44,17,inactive?new Color(.8f,.8f,.8f):new Color(.45f,1,.5f));
        }
        int n=GameDatabase.Survivors.Count;float step=1360f/Mathf.Max(1,n);
        for(int i=0;i<n;i++)
        {
            var d=GameDatabase.Survivors[i];string id=d.id;bool chosen=missionFormationIds.Contains(id);
            var panel=FormationPanel(formationRoster,"Available_"+id,i*step,0,step-16,268,chosen?Color.green:d.color);
            var b=FormationButton(panel,"Pick","",0,0,step-16,268,()=>ToggleMissionFormation(id),new Color(.04f,.08f,.10f));
            missionFormationButtons[id]=b;
            FormationPortrait(panel,d,12,36,step-40,172);
            FormationText(panel,"State",chosen?"EM FORMAÇÃO":"SELECIONAR",12,6,step-40,30,18,chosen?Color.green:FormationGold);
            FormationText(panel,"Class",d.name.ToUpperInvariant()+"\nNV. "+GameSession.Save.GetProgress(id).level,12,211,step-40,50,23,Color.white);
        }
        int active=Mathf.Min(MissionFormationLimit,missionFormationIds.Count);
        missionFormationSummary.text="NESTA MISSÃO\n"+active+" / "+MissionFormationLimit+"\nSALVOS: "+missionFormationIds.Count+" / 3";
        var lines=new List<string>();
        for(int i=0;i<missionFormationIds.Count;i++)
        {
            var d=FormationDef(missionFormationIds[i]);
            lines.Add(roles[i]+": "+(d!=null?d.name:"—")+(i>=MissionFormationLimit?" (reserva)":""));
        }
        formationTeamInfo.text="ORDEM DA EQUIPE\n\n"+string.Join("\n\n",lines)+"\n\nConfirmar salva a ordem neste slot de jogo.";
        missionFormationConfirm.interactable=missionFormationIds.Count>0;
    }
    void ConfirmMissionFormation()
    {
        var save=GameSession.Save;
        if(save==null || missionFormationIds.Count<1 || missionFormationIds.Count>3)return;
        // Save synchronously before closing. Restore in-memory selection on failure.
        string oldFront=save.selectedSurvivor,oldMiddle=save.selectedPartner,oldRear=save.selectedThird;
        save.selectedSurvivor=missionFormationIds[0];
        save.selectedPartner=missionFormationIds.Count>1?missionFormationIds[1]:"";
        save.selectedThird=missionFormationIds.Count>2?missionFormationIds[2]:"";
        try { SaveSystem.Save(save); }
        catch(System.Exception e)
        {
            save.selectedSurvivor=oldFront;save.selectedPartner=oldMiddle;save.selectedThird=oldRear;
            Debug.LogError("ZomBit: falha ao salvar formação: "+e.Message);
            Toast("Não foi possível salvar a formação. Tente novamente.");return;
        }
        int keep=selectedDiff;
        CloseMissionFormation();
        if(selectedLoc!=null){SelectLocation(selectedLoc);SelectDifficulty(keep);}
        Toast("Formação salva. Reservas mantidas para as próximas missões.");
    }
}
