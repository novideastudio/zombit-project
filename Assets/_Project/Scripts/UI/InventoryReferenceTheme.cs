using UnityEngine;
[CreateAssetMenu(menuName="ZomBit/UI/Inventory Reference Theme",fileName="InventoryReferenceTheme")]
public sealed class InventoryReferenceTheme : ScriptableObject
{
    public bool enabledTheme=true;
    public Sprite panel,sectionHeader,slot,gold,red,green,orange,coinIcon,scrapIcon,bagIcon,weaponIcon,workshopIcon;
    public Sprite inventoryBackground;
    public Color titleColor=new Color(1,.78f,.3f);
    public Vector4 equipmentRect=new Vector4(16,112,720,690);
    public Vector4 bagRect=new Vector4(752,112,640,690);
    public Vector4 detailsRect=new Vector4(1408,112,624,690);
    public Vector4 statsRect=new Vector4(16,824,2016,310);
    [System.Serializable]public class CharacterArt{public string id;public Sprite fullBody,portrait,weapon;}
    public CharacterArt[] characters={new CharacterArt{id="assaulter"},new CharacterArt{id="sniper"},new CharacterArt{id="medic"},new CharacterArt{id="engineer"},new CharacterArt{id="explorer"}};
    [System.Serializable]public class SlotArt{public ItemSlot slot;public Sprite icon;}
    public SlotArt[] slotIcons={new SlotArt{slot=ItemSlot.Capacete},new SlotArt{slot=ItemSlot.Colete},new SlotArt{slot=ItemSlot.Calca},new SlotArt{slot=ItemSlot.Botas},new SlotArt{slot=ItemSlot.Mochila},new SlotArt{slot=ItemSlot.Arma}};
}
