using UnityEngine;
public sealed class RegionalCosmeticsHUD : MonoBehaviour
{
    Canvas canvas;
    void Start()
    {
        if(GameSession.Save==null)return;var s=RegionalRewards.Read(GameSession.Save);
        if(string.IsNullOrEmpty(s.selectedTitle) && string.IsNullOrEmpty(s.selectedFrame))return;
        canvas=UIFactory.MakeCanvas("RegionalCosmeticsCanvas",22);
        var title=UIFactory.Txt(canvas.transform,s.selectedTitle,19,TMPro.TextAlignmentOptions.Center,UIFactory.Gold,"RegionalTitle");
        UIFactory.Place(title.rectTransform,new Vector2(.5f,1),new Vector2(0,-16),new Vector2(620,30));title.raycastTarget=false;
        int rank=ProgressionRuntime.Order(s.selectedFrame);if(rank>=9)return;
        Color color=Color.HSVToRGB((rank+1)/10f,.65f,.95f);color.a=.7f;
        for(int i=0;i<4;i++)
        {
            var im=UIFactory.Img(canvas.transform,color,"Frame"+i);im.raycastTarget=false;
            if(i==0)UIFactory.Stretch(im.rectTransform,0,.997f,1,1);
            if(i==1)UIFactory.Stretch(im.rectTransform,0,0,1,.003f);
            if(i==2)UIFactory.Stretch(im.rectTransform,0,0,.002f,1);
            if(i==3)UIFactory.Stretch(im.rectTransform,.998f,0,1,1);
        }
    }
    void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
}
