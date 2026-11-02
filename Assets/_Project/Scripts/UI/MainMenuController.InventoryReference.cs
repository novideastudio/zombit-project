public partial class MainMenuController
{
    void ApplyInventoryReferenceLayout()
    {
        if(sharedInventoryView!=null)
            InventoryReferenceLayout.Bind(sharedInventoryView.Stage,invSurvivorId,mapInventorySelectedUid,GameSession.Save);
    }
}
