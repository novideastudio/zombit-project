using System.Collections.Generic;
using UnityEngine;

// Contact shadows anchored to visible feet, not the transparent canvas of the sprite.
[DefaultExecutionOrder(100)]
public class YSort : MonoBehaviour
{
    [HideInInspector] public int sortBias;
    [Header("Sombra de contato")]
    public bool contactShadow=true;
    [Range(0,1)]public float shadowOpacity=.60f;
    [Range(.3f,1.5f)]public float shadowWidth=.95f;
    [Range(.1f,.5f)]public float shadowAspect=.23f;
    public Vector2 shadowFootOffset=Vector2.zero;
    SpriteRenderer sr,shadow;
    Sprite cachedSprite;
    Vector2 footLocal;
    static Sprite shadowSprite;
    static Transform shadowRoot;
    static readonly Dictionary<Sprite,Vector2> feetCache=new Dictionary<Sprite,Vector2>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){feetCache.Clear();shadowSprite=null;shadowRoot=null;}
    void Awake(){sr=GetComponent<SpriteRenderer>();}
    void OnEnable(){if(shadow!=null)shadow.gameObject.SetActive(true);}
    void OnDisable(){if(shadow!=null)shadow.gameObject.SetActive(false);}
    void OnDestroy(){if(shadow!=null)Destroy(shadow.gameObject);}
    void LateUpdate()
    {
        if(sr==null)sr=GetComponent<SpriteRenderer>();
        if(sr==null || sr.sprite==null){if(shadow!=null)shadow.enabled=false;return;}
        if(cachedSprite!=sr.sprite){cachedSprite=sr.sprite;footLocal=VisibleFoot(cachedSprite);}
        Vector2 local=footLocal;
        if(sr.flipX)local.x=-local.x;if(sr.flipY)local.y=-local.y;
        Vector3 feet=transform.TransformPoint(new Vector3(local.x,local.y,0));
        sr.sortingOrder=Mathf.RoundToInt(-feet.y*100)+sortBias;
        if(!contactShadow && !CombatVisuals.AlwaysShowShadows){if(shadow!=null)shadow.enabled=false;return;}
        if(shadow==null)CreateShadow();
        if(shadow==null)return;
        float width=CombatVisuals.ShadowWidth(cachedSprite,cachedSprite.bounds.size.x*Mathf.Abs(transform.lossyScale.x)*shadowWidth,Mathf.Abs(transform.lossyScale.y));
        float height=width*shadowAspect*CombatVisuals.ShadowAspectMult;
        shadow.transform.position=feet+new Vector3(shadowFootOffset.x,shadowFootOffset.y,0);
        shadow.transform.localScale=new Vector3(width,height,1);
        shadow.enabled=sr.enabled;shadow.sortingLayerID=sr.sortingLayerID;shadow.sortingOrder=sr.sortingOrder-1;
        shadow.color=CombatVisuals.ShadowColor(shadowOpacity*CombatVisuals.ShadowOpacityMult*sr.color.a);
    }
    static Vector2 VisibleFoot(Sprite sprite)
    {
        if(feetCache.TryGetValue(sprite,out var result))return result;
        var metrics=SpriteMetrics.Get(sprite);
        var rect=sprite.rect;float ppu=Mathf.Max(.001f,sprite.pixelsPerUnit);
        float x=sprite.pivot.x;
        float bottom=metrics.bottomPx;
        // Estimate foot centre from the bottom 12% of visible pixels, excluding the weapon.
        var texture=sprite.texture;
        if(texture!=null && texture.isReadable && !sprite.packed)
        {
            try
            {
                var pixels=texture.GetPixels32();
                int ox=Mathf.RoundToInt(rect.x),oy=Mathf.RoundToInt(rect.y);
                int w=Mathf.RoundToInt(rect.width),h=Mathf.RoundToInt(rect.height);
                int lo=Mathf.Clamp(Mathf.FloorToInt(bottom),0,Mathf.Max(0,h-1));
                int hi=Mathf.Min(h-1,lo+Mathf.Max(1,Mathf.RoundToInt(metrics.heightPx*.12f)));
                double sum=0;int count=0;
                for(int py=lo;py<=hi;py++)for(int px=0;px<w;px++)
                {
                    int at=(oy+py)*texture.width+ox+px;
                    if(at<0 || at>=pixels.Length || pixels[at].a<=32)continue;
                    sum+=px+.5;count++;
                }
                if(count>0)x=(float)(sum/count);
            }
            catch(UnityException){ /* Non-readable/unsupported atlas: pivot fallback. */ }
        }
        result=new Vector2((x-sprite.pivot.x)/ppu,(bottom-sprite.pivot.y)/ppu);
        feetCache[sprite]=result;return result;
    }
    void CreateShadow()
    {
        if(shadowSprite==null)shadowSprite=MakeShadowSprite();
        if(shadowRoot==null)shadowRoot=new GameObject("ContactShadows").transform;
        var go=new GameObject("ContactShadow");go.transform.SetParent(shadowRoot,false);
        shadow=go.AddComponent<SpriteRenderer>();shadow.sprite=shadowSprite;
    }
    static Sprite MakeShadowSprite()
    {
        const int n=96;
        var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
        var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float dx=(x+.5f)/n*2-1,dy=(y+.5f)/n*2-1;
            float r=Mathf.Sqrt(dx*dx+dy*dy);
            // Dense contact centre and a short soft falloff, rather than a pale haze.
            float a=1-Mathf.SmoothStep(0f,1f,Mathf.Clamp01((r-.28f)/.72f));
            pixels[y*n+x]=new Color(1,1,1,a);
        }
        texture.SetPixels(pixels);texture.Apply(false,true);
        return Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),n);
    }
}
