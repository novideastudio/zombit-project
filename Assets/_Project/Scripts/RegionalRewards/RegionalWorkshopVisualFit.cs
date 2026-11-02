using UnityEngine;
public sealed class RegionalWorkshopVisualFit : MonoBehaviour
{
    public RectTransform stage,viewport;
    public void Apply()
    {
        if(stage==null || viewport==null)return;Vector2 s=viewport.rect.size;
        if(s.x<=0 || s.y<=0)return;float k=Mathf.Min(s.x/1520f,s.y/980f)*.94f;
        stage.localScale=new Vector3(k,k,1);stage.anchoredPosition=Vector2.zero;
    }
    void LateUpdate(){Apply();}
}
