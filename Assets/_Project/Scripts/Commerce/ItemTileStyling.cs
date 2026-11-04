using UnityEngine;
using UnityEngine.UI;

public static class ItemTileStyling
{
    public static Color Background(ItemInstance item)
    {
        if(item==null)return Color.clear;
        var id=typeof(ItemInstance).GetField("regionUniqueId");
        if(id!=null && !string.IsNullOrEmpty(id.GetValue(item) as string))return TimedCommerce.Config.regional;
        switch(item.rarity)
        {
            case Rarity.Incomum:return TimedCommerce.Config.uncommon;
            case Rarity.Raro:return TimedCommerce.Config.rare;
            case Rarity.Epico:return TimedCommerce.Config.epic;
            case Rarity.Lendario:return TimedCommerce.Config.legendary;
            default:return Color.clear;
        }
    }
    public static void Apply(Transform tile,ItemInstance item)
    {
        if(tile==null)return;
        var child=tile.Find("ItemRarityBackground");Image image;
        if(child==null)
        {
            var rt=UIFactory.Node("ItemRarityBackground",tile);UIFactory.Stretch(rt,.045f,.045f,.955f,.955f);
            image=rt.gameObject.AddComponent<Image>();image.sprite=UIFactory.White;image.raycastTarget=false;
        }
        else image=child.GetComponent<Image>();
        if(image==null)return;image.color=Background(item);image.enabled=image.color.a>0;
        // Place behind the icon, but above old generic background layers.
        var icon=tile.Find("Icon");int target=icon!=null?icon.GetSiblingIndex():1;
        image.transform.SetSiblingIndex(Mathf.Max(0,target-1));
    }
}
