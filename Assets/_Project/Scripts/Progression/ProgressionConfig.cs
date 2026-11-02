using UnityEngine;
[CreateAssetMenu(menuName="ZomBit/Progression/Complete V1",fileName="ProgressionConfig")]
public sealed class ProgressionConfig : ScriptableObject
{
    [System.Serializable]public class Region
    {
        public string id;public int power,level,waves;public float hp,damage,spawn,goldReward;
        public Region(string i,int p,int l,int w,float h,float d,float s,int g){id=i;power=p;level=l;waves=w;hp=h;damage=d;spawn=s;goldReward=g;}
    }
    public Region[] regions={
        new Region("suburbio",100,1,3,1,1,1,100),
        new Region("residencial",160,6,3,1.326f,1.162f,1.058f,120),
        new Region("shopping",260,12,4,1.774f,1.358f,1.122f,160),
        new Region("hospital",430,18,4,2.400f,1.595f,1.191f,200),
        new Region("delegacia",720,25,5,3.268f,1.881f,1.267f,260),
        new Region("metro",1200,32,5,4.441f,2.215f,1.347f,330),
        new Region("centro",2000,39,6,6.034f,2.608f,1.433f,430),
        new Region("base",3300,46,6,8.148f,3.061f,1.522f,550),
        new Region("industrial",5500,50,7,11.071f,3.605f,1.600f,700)
    };
    public float[] tierPower={.75f,1,1.45f,2.1f};
    public float[] tierHp={.8f,1,1.4f,1.9f};
    public float[] tierDamage={.8f,1,1.3f,1.65f};
    public float[] tierSpawn={.85f,1,1.15f,1.3f};
    public float phaseSeconds=35,hordeRamp=.004f;
    public float rewardGrowth=1.12f,baseXp=.254f,baseGold=.105f,baseScrap=.41f;
    public float eliteSharePerRegion=.01f,spitterSharePerRegion=.008f;
    public float failedScrapRetention=.25f;
    [Header("Score heuristico; nao e probabilidade de vitoria")]
    public float referenceDps=40,referenceEhp=100,scoreExponent=1.35f;
    public float extremeBelow=.7f,highBelow=.9f,challengingBelow=1.1f;
}
