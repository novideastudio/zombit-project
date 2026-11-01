using UnityEngine;

// Fits the 1920x1080 formation composition without stretching its proportions.
public sealed class ZomBitFormationStageFit : MonoBehaviour
{
    public RectTransform stage;
    public RectTransform viewport;
    Vector2 lastSize;
    public void Apply()
    {
        if(stage==null || viewport==null)return;
        var size=viewport.rect.size;
        if(size.x<=0 || size.y<=0)return;
        float scale=Mathf.Min(size.x/1920f,size.y/1080f);
        stage.localScale=new Vector3(scale,scale,1);
        stage.anchoredPosition=Vector2.zero;lastSize=size;
    }
    void LateUpdate(){if(viewport!=null && viewport.rect.size!=lastSize)Apply();}
}
