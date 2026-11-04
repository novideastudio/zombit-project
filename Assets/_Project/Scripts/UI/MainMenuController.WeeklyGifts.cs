using UnityEngine;
public partial class MainMenuController
{
    Transform weeklyGiftStage;
    void BindWeeklyGiftButton(Transform stage){weeklyGiftStage=stage;RefreshWeeklyGiftVisibility();}
    void ClaimWeeklyGiftFromHome(Transform stage)
    {
        try{Toast(TimedCommerce.ClaimGift(GameSession.Save));}
        catch(System.Exception e){Toast("Não foi possível receber presentes. Confira o Console.");Debug.LogError(e);}
        weeklyGiftStage=stage;RefreshWeeklyGiftVisibility();
    }
    void RefreshWeeklyGiftVisibility()
    {
        if(weeklyGiftStage==null)return;var button=weeklyGiftStage.Find("ReferenceButton_gifts");if(button==null)return;
        try{button.gameObject.SetActive(TimedCommerce.GiftAvailable(GameSession.Save));}
        catch(System.Exception e){button.gameObject.SetActive(false);Debug.LogWarning(e.Message);}
    }
}
