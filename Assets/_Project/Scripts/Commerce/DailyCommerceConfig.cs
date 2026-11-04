using UnityEngine;
[CreateAssetMenu(menuName="ZomBit/Commerce/Daily config",fileName="DailyCommerceConfig")]
public sealed class DailyCommerceConfig : ScriptableObject
{
    public int inventoryCapacity=60;
    public int maxLevel=50;
    public float basePrice=60,pricePerLevel=40;
    public Color uncommon=new Color(.05f,.32f,.12f,.88f),rare=new Color(.04f,.16f,.48f,.88f),epic=new Color(.30f,.06f,.43f,.88f),legendary=new Color(.48f,.31f,.03f,.9f),regional=new Color(.48f,.025f,.035f,.92f);
}
