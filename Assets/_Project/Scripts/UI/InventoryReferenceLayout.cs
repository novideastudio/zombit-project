using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menu inventory only. Reuses real buttons, listeners and item data; never writes a save.
public sealed class InventoryReferenceLayout : MonoBehaviour
{
    InventoryReferenceTheme theme;SaveData save;string activeId,selectedUid;RectTransform stage;
    readonly Dictionary<string,RectTransform> panels=new Dictionary<string,RectTransform>();
    public static void Bind(RectTransform target,string id,string uid,SaveData data)
    {
        if(target==null)return;var t=Resources.Load<InventoryReferenceTheme>("Config/InventoryReferenceTheme");
        if(t==null || !t.enabledTheme)return;
        var view=target.GetComponent<InventoryReferenceLayout>();if(view==null)view=target.gameObject.AddComponent<InventoryReferenceLayout>();
        view.theme=t;view.stage=target;view.save=data;view.activeId=id;view.selectedUid=uid;view.Apply();
    }
    static Transform Find(Transform root,string name)
    {
        if(root==null)return null;if(root.name==name)return root;
        for(int i=0;i<root.childCount;i++){var t=Find(root.GetChild(i),name);if(t!=null)return t;}return null;
    }
    static RectTransform R(Transform p,string name)=>Find(p,name) as RectTransform;
    static TMP_Text T(Transform p,string name)=>Find(p,name)?.GetComponent<TMP_Text>();
    static void Place(RectTransform rt,float x,float y,float w,float h)
    {if(rt==null)return;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);rt.localScale=Vector3.one;}
    static void Place(RectTransform rt,Vector4 q){Place(rt,q.x,q.y,q.z,q.w);}
    static void Hide(Transform p,string name){var t=Find(p,name);if(t!=null)t.gameObject.SetActive(false);}
    static void Skin(Transform t,Sprite sp)
    {if(t==null || sp==null)return;var i=t.GetComponent<Image>();if(i==null)return;i.sprite=sp;i.type=Image.Type.Sliced;i.color=Color.white;}
    static void StyleText(TMP_Text t,float size,Color? color=null)
    {if(t==null)return;t.enableAutoSizing=true;t.fontSizeMin=11;t.fontSizeMax=size;t.fontSize=size;t.fontStyle=FontStyles.Bold;if(color.HasValue)t.color=color.Value;}
    RectTransform Node(Transform p,string name,float x,float y,float w,float h)
    {var r=UIFactory.Node(name,p);Place(r,x,y,w,h);return r;}
    TMP_Text Label(Transform p,string name,string value,float x,float y,float w,float h,float size=22,Color? color=null)
    {
        var rt=Node(p,name,x,y,w,h);var t=UIFactory.Txt(rt,value,size,TextAlignmentOptions.TopLeft,color??Color.white,"Text");UIFactory.Stretch(t.rectTransform);
        t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;StyleText(t,size,color);return t;
    }
    Image Icon(Transform p,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var rt=Node(p,name,x,y,w,h);var im=rt.gameObject.AddComponent<Image>();im.sprite=sprite;im.preserveAspect=true;im.raycastTarget=false;return im;
    }
    RectTransform Box(Transform p,string name,float x,float y,float w,float h,Sprite art)
    {var rt=Node(p,name,x,y,w,h);var im=rt.gameObject.AddComponent<Image>();im.sprite=art;im.type=Image.Type.Sliced;im.raycastTarget=false;im.color=art!=null?Color.white:new Color(.025f,.05f,.07f);return rt;}
    void ClearOwned(Transform p,string name)
    {var t=p!=null?p.Find(name):null;if(t!=null){t.gameObject.SetActive(false);t.name=name+"__Retired";Destroy(t.gameObject);}}
    InventoryReferenceTheme.CharacterArt CharacterArt()
    {foreach(var c in theme.characters)if(c!=null && c.id==activeId)return c;return null;}
    Sprite SlotIcon(ItemSlot slot,Sprite fallback)
    {foreach(var c in theme.slotIcons)if(c!=null && c.slot==slot && c.icon!=null)return c.icon;return fallback;}
    void Header(Transform panel,string title)
    {
        Skin(panel,theme.panel);ClearOwned(panel,"ReferenceSectionHeader");
        var rt=panel as RectTransform;if(rt==null)return;
        Box(panel,"ReferenceSectionHeader",0,0,rt.sizeDelta.x,56,theme.sectionHeader).SetAsFirstSibling();
        var t=T(panel,"PanelTitle");if(t!=null){t.text=title;Place(t.rectTransform,46,9,rt.sizeDelta.x-72,38);StyleText(t,25,Color.white);t.transform.SetAsLastSibling();}
        Hide(panel,"Tick");
    }
    [ContextMenu("Reaplicar visual da referencia")]
    public void Apply()
    {
        if(stage==null || theme==null || save==null)return;
        ClearOwned(stage,"ReferenceHeaderExtras");
        Place(R(stage,"MapEquipmentPanel"),theme.equipmentRect);Place(R(stage,"MapBackpackPanel"),theme.bagRect);
        Place(R(stage,"MapItemDetailsPanel"),theme.detailsRect);Place(R(stage,"MapStatsPanel"),theme.statsRect);
        if(theme.inventoryBackground!=null){var bg=Find(stage,"Background")?.GetComponent<Image>();if(bg!=null){bg.sprite=theme.inventoryBackground;bg.color=Color.white;}}
        LayoutHeader();LayoutEquipment();LayoutBag();LayoutStats();LayoutDetails();
    }
    void LayoutHeader()
    {
        Hide(stage,"Currency");Hide(stage,"CurrencyPanel");
        Place(R(stage,"Header"),12,8,824,86);Skin(R(stage,"Header"),theme.panel);
        Place(R(stage,"Logo"),20,12,220,78);
        Place(R(stage,"Title"),300,24,520,54);StyleText(T(stage,"Title"),34,theme.titleColor);
        var extra=Node(stage,"ReferenceHeaderExtras",0,0,2048,112);
        Icon(extra,"BagIcon",theme.bagIcon,248,24,44,50);
        var workshop=R(stage,"RegionalWorkshopTab");
        if(workshop!=null){Place(workshop,852,20,280,66);Skin(workshop,theme.gold);StyleText(workshop.GetComponentInChildren<TMP_Text>(),21,theme.titleColor);}
        var gold=Box(extra,"GoldWallet",1150,20,300,66,theme.panel);
        Icon(gold,"Coin",theme.coinIcon,12,12,40,40);Label(gold,"Value","OURO  "+save.gold.ToString("N0"),64,17,224,36,24,theme.titleColor);
        var scrap=Box(extra,"ScrapWallet",1468,20,300,66,theme.panel);
        Icon(scrap,"Gear",theme.scrapIcon,12,12,40,40);Label(scrap,"Value","SUCATA  "+save.scrap.ToString("N0"),64,17,224,36,24,theme.titleColor);
        var back=R(stage,"BackButton");Place(back,1786,12,246,78);Skin(back,theme.red);
        var caption=R(back,"Label");Place(caption,10,0,226,78);StyleText(T(back,"Label"),30,Color.white);
    }
    void LayoutEquipment()
    {
        var p=R(stage,"MapEquipmentPanel");if(p==null)return;Header(p,"EQUIPAMENTO DO SOBREVIVENTE");
        var art=CharacterArt();var body=R(p,"EquipmentCharacter");Place(body,138,62,430,516);
        if(art!=null && art.fullBody!=null){var image=body?.GetComponent<Image>();if(image!=null)image.sprite=art.fullBody;}
        MoveSlot(p,"Capacete",22,76);MoveSlot(p,"Mochila",22,204);MoveSlot(p,"Colete",22,332);MoveSlot(p,"Calca",22,460);
        MoveSlot(p,"Botas",594,76);MoveSlot(p,"Arma",594,332);
        Place(R(p,"FixedWeaponLabel"),574,203,136,24);
        var weapon=R(p,"ExclusiveWeaponDisplay");Place(weapon,594,230,96,96);Skin(weapon,theme.gold);
        Hide(weapon,"Lock");Hide(weapon,"Fixed");
        var caption=T(weapon,"WeaponName");if(caption!=null){Place(caption.rectTransform,2,76,92,18);StyleText(caption,13,theme.titleColor);}
        ClearOwned(weapon,"ReferenceWeaponIcon");
        Icon(weapon,"ReferenceWeaponIcon",art!=null && art.weapon!=null?art.weapon:theme.weaponIcon,8,8,80,62);
        Place(R(p,"AccessoryLabel"),574,460,136,24);Place(R(p,"UnavailableAccessory"),594,486,96,96);Skin(R(p,"UnavailableAccessory"),theme.slot);
        // The existing game has no accessory slot: keep it visibly unavailable.
        var strip=R(p,"SurvivorStrip");Place(strip,16,594,688,80);
        if(strip!=null)
        {
            var group=strip.GetComponent<HorizontalLayoutGroup>();if(group!=null){group.spacing=10;group.childControlWidth=true;group.childForceExpandWidth=true;}
            foreach(var button in strip.GetComponentsInChildren<Button>(true))
            {
                Skin(button.transform,button.name=="Survivor_"+activeId?theme.gold:theme.slot);
                var le=button.GetComponent<LayoutElement>();if(le!=null){le.preferredWidth=120;le.flexibleWidth=1;}
            }
        }
    }
    void MoveSlot(Transform p,string name,float x,float y)
    {
        var r=R(p,"EquipSlot_"+name);Place(r,x,y+22,96,96);Skin(r,theme.slot);
        var label=T(p,"EquipLabel_"+name);if(label!=null){Place(label.rectTransform,x-8,y,112,20);StyleText(label,14,Color.white);}
        if(r!=null && Enum.TryParse<ItemSlot>(name,out var slot))
        {var image=Find(r,"Icon")?.GetComponent<Image>();if(image!=null)image.sprite=SlotIcon(slot,image.sprite);}
    }
    void LayoutBag()
    {
        var p=R(stage,"MapBackpackPanel");if(p==null)return;Header(p,"MOCHILA DE CAMPO");
        var title=T(p,"PanelTitle");if(title!=null)Place(title.rectTransform,46,9,350,38);
        var count=R(p,"InventoryPanelCount");Place(count,434,12,190,32);
        var tabs=R(p,"InventoryCategoryTabs");Place(tabs,18,68,604,160);
        if(tabs!=null)
        {
            var group=tabs.GetComponent<HorizontalLayoutGroup>();if(group!=null)group.enabled=false;
            string[] order={"InvCat_TODOS","FixedWeaponTab","InvCat_CAPACETES","InvCat_COLETES","InvCat_CALÇAS","InvCat_BOTAS","InvCat_MOCHILAS","InvCat_MELHORIAS","InventorySortButton"};
            var fixedTab=tabs.Find("FixedWeaponTab") as RectTransform;
            if(fixedTab==null)
            {
                var b=UIFactory.Btn(tabs,"ARMA FIXA",17,null,null,"FixedWeaponTab");b.interactable=false;fixedTab=(RectTransform)b.transform;
            }
            var sort=R(p,"InventorySortButton");if(sort!=null)sort.SetParent(tabs,false);
            for(int i=0;i<order.Length;i++)
            {
                var t=R(tabs,order[i]);if(t==null)continue;
                Place(t,(i%3)*204,(i/3)*54,196,48);Skin(t,theme.panel);StyleText(t.GetComponentInChildren<TMP_Text>(),18,Color.white);
            }
        }
        var vp=R(p,"MapItemsViewport");Place(vp,18,242,604,430);
        var grid=p.GetComponentInChildren<GridLayoutGroup>(true);
        if(grid!=null){grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=5;grid.cellSize=new Vector2(106,106);grid.spacing=new Vector2(10,10);grid.padding=new RectOffset(6,6,6,6);}
        foreach(var t in p.GetComponentsInChildren<RectTransform>(true))
        {
            if(!t.name.StartsWith("Item_") && !t.name.StartsWith("Empty_"))continue;
            ItemInstance item=null;
            if(t.name.StartsWith("Item_") && save.inventory!=null)
            {string uid=t.name.Substring(5);foreach(var candidate in save.inventory)if(candidate!=null && candidate.uid==uid){item=candidate;break;}}
            Skin(t,theme.slot);
            if(item!=null)
            {
                var im=t.GetComponent<Image>();if(im!=null)im.color=Color.white;
                var button=t.GetComponent<Button>();if(button!=null){var cb=button.colors;cb.normalColor=Color.white;cb.selectedColor=Color.white;cb.highlightedColor=new Color(1,.94f,.75f);button.colors=cb;}
                var outline=t.GetComponent<Outline>();if(outline==null)outline=t.gameObject.AddComponent<Outline>();
                outline.effectColor=item.uid==selectedUid?theme.titleColor:RarityColors.Get(item.rarity);outline.effectDistance=new Vector2(2,-2);
                var icon=Find(t,"Icon")?.GetComponent<Image>();if(icon!=null)icon.sprite=SlotIcon(item.slot,icon.sprite);
            }
        }
    }
    void LayoutStats()
    {
        var p=R(stage,"MapStatsPanel");if(p==null)return;Header(p,"ATRIBUTOS DO SOBREVIVENTE");
        Place(R(p,"ProfilePortrait"),24,76,120,176);var art=CharacterArt();
        if(art!=null && art.portrait!=null){var im=Find(p,"ProfilePortrait")?.GetComponent<Image>();if(im!=null)im.sprite=art.portrait;}
        Place(R(p,"ProfileName"),166,75,280,42);StyleText(T(p,"ProfileName"),30,theme.titleColor);
        Place(R(p,"ProfileLevel"),458,75,110,42);StyleText(T(p,"ProfileLevel"),27,Color.white);
        Place(R(p,"ProfileClass"),166,126,410,28);Place(R(p,"ProfileXp"),166,242,410,28);
        foreach(var im in p.GetComponentsInChildren<Image>(true))if(im.type==Image.Type.Filled){Place(im.transform.parent as RectTransform,166,218,388,14);break;}
        Place(R(p,"Description"),588,75,480,106);StyleText(T(p,"Description"),23,Color.white);
        var blocks=new List<RectTransform>();foreach(var rt in p.GetComponentsInChildren<RectTransform>(true))if(rt.name.StartsWith("Stat_"))blocks.Add(rt);
        foreach(var block in blocks)block.SetParent(p,false);
        Hide(p,"StatsGrid");Hide(p,"SharedStatsGrid");
        string[] top={"VIDA","DANO","DEFESA","CADÊNCIA"};
        for(int i=0;i<top.Length;i++)
        {
            var b=R(p,"Stat_"+top[i]);if(b==null)continue;
            Place(b,1090+i*225,78,217,94);Skin(b,theme.panel);
            var label=T(b,"Label");if(label!=null){Place(label.rectTransform,12,6,193,24);StyleText(label,16,theme.titleColor);}
            var value=T(b,"Value");if(value!=null){Place(value.rectTransform,12,32,193,54);StyleText(value,36,Color.white);}
            Hide(b,"Sub");
        }
        SmallStat(p,"DANO BASE DA ARMA",588,208,460);
        SmallStat(p,"PONTOS DE HABILIDADE",1064,208,410);
        ClearOwned(p,"ReferenceItemBonus");var box=Box(p,"ReferenceItemBonus",1490,208,500,68,theme.panel);
        float damage=0,hp=0,defense=0;
        var progress=save.GetProgress(activeId);if(progress.equipped!=null)foreach(var item in progress.equipped)if(item!=null){damage+=item.bonusDamage;hp+=item.bonusHealth;defense+=item.bonusDefense;}
        Label(box,"Bonus","BÔNUS DE ITENS\n+"+damage.ToString("0.#")+" dano  |  +"+hp.ToString("0.#")+" vida  |  +"+defense.ToString("0.#")+" defesa",14,7,472,54,18,theme.titleColor);
        Place(R(p,"StatScope"),24,286,1960,18);StyleText(T(p,"StatScope"),12,new Color(.55f,.65f,.7f));
    }
    void SmallStat(Transform p,string name,float x,float y,float w)
    {
        var b=R(p,"Stat_"+name);if(b==null)return;Place(b,x,y,w,68);Skin(b,theme.panel);
        var label=T(b,"Label");if(label!=null){Place(label.rectTransform,14,8,w*.65f,50);StyleText(label,17,Color.white);}
        var value=T(b,"Value");if(value!=null){Place(value.rectTransform,w*.68f,8,w*.28f,50);StyleText(value,25,theme.titleColor);}
        Hide(b,"Sub");
    }
    void LayoutDetails()
    {
        var p=R(stage,"MapItemDetailsPanel");if(p==null)return;Header(p,"DETALHES DO ITEM");
        // Retain original action buttons and their validation/listeners before replacing presentation.
        var actions=new List<Button>();
        foreach(var b in p.GetComponentsInChildren<Button>(true))
        {
            if(!b.gameObject.activeSelf)continue;
            var txt=b.GetComponentInChildren<TMP_Text>(true);string words=txt!=null?txt.text.Trim().ToUpperInvariant():"";
            if(words.StartsWith("EQUIPAR") || words.StartsWith("DESEQUIPAR") || (words.StartsWith("MELHORAR") || words.StartsWith("REFINAR")) || words.StartsWith("VENDER"))actions.Add(b);
        }
        foreach(var action in actions)action.transform.SetParent(p,false);
        var keep=new HashSet<Transform>();foreach(var a in actions)keep.Add(a.transform);
        for(int i=p.childCount-1;i>=0;i--)
        {
            var child=p.GetChild(i);if(child.name=="PanelTitle" || child.name=="ReferenceSectionHeader" || keep.Contains(child))continue;
            child.gameObject.SetActive(false);
            if(child.name=="ReferenceDetails")Destroy(child.gameObject);
        }
        var body=Node(p,"ReferenceDetails",0,0,624,690);
        ItemInstance item=null;
        if(save.inventory!=null)foreach(var it in save.inventory)if(it!=null && it.uid==selectedUid){item=it;break;}
        var progress=save.GetProgress(activeId);
        if(item==null && progress.equipped!=null)foreach(var it in progress.equipped)if(it!=null && it.uid==selectedUid){item=it;break;}
        if(item==null){Label(body,"Empty","SELECIONE UM ITEM",24,90,576,100,26,theme.titleColor);return;}
        ItemInstance current=null;if(progress.equipped!=null)foreach(var it in progress.equipped)if(it!=null && it.slot==item.slot){current=it;break;}
        var iconBox=Box(body,"ItemFrame",20,76,142,142,theme.gold);
        ItemTileStyling.Apply(iconBox, item); // DAILY_COMMERCE_DETAILS_RARITY_V1
        Icon(iconBox,"Icon",SlotIcon(item.slot,ItemVisual.Icon(item)),12,12,118,118);
        Label(body,"Name",item.name,180,78,422,52,27,RarityColors.Get(item.rarity));
        Label(body,"Type",item.rarity.ToString().ToUpperInvariant()+"  ·  "+item.slot.ToString().ToUpperInvariant()+"  ·  +"+item.upgradeLevel,180,137,422,30,18,theme.titleColor);
        Label(body,"Description",string.IsNullOrWhiteSpace(item.description)?"Equipamento do sobrevivente.":item.description,180,177,422,50,17);
        Label(body,"CompareTitle","COMPARAÇÃO COM O EQUIPADO",20,240,584,30,21,theme.titleColor);
        var old=Box(body,"Current",20,280,260,116,theme.panel);var chosen=Box(body,"Selected",302,280,302,116,theme.green);
        Label(old,"Title",current!=null?"ATUAL  +"+current.upgradeLevel:"ATUAL · SEM ITEM",12,7,236,24,17);
        Label(old,"Values",Stats(current),12,38,236,70,19);
        Label(body,"Arrow",">",282,312,20,38,25,theme.titleColor);
        Label(chosen,"Title","SELECIONADO  +"+item.upgradeLevel,12,7,278,24,17,theme.titleColor);
        Label(chosen,"Values",Stats(item)+"\nΔ dano "+(item.bonusDamage-(current!=null?current.bonusDamage:0)).ToString("+0.#;-0.#;0"),12,36,278,74,17);
        Label(body,"AttributesTitle","ATRIBUTOS DO ITEM",20,406,584,30,21,theme.titleColor);
        var vp=Node(body,"AttributesViewport",20,446,584,106);vp.gameObject.AddComponent<RectMask2D>();
        var capture=vp.gameObject.AddComponent<Image>();capture.color=Color.clear;capture.raycastTarget=true;
        string extra="";if(item.affixes!=null)foreach(var a in item.affixes)if(a!=null)extra+="• "+a.Describe()+"\n";
        if(string.IsNullOrEmpty(extra))extra="Sem afixos adicionais.\n";
        var content=Node(vp,"Content",0,0,566,Mathf.Max(106,extra.Split('\n').Length*26));
        Label(content,"Affixes",extra,0,0,566,content.sizeDelta.y,19);
        var scroll=vp.gameObject.AddComponent<ScrollRect>();scroll.viewport=vp;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=32;
        Label(body,"SellValue","VALOR DE VENDA   "+item.sellValue+" OURO",20,565,584,28,21,theme.titleColor);
        // Order: equip/unequip, upgrade, sell. No new gameplay operations are fabricated.
        actions.Sort((a,b)=>ActionOrder(a).CompareTo(ActionOrder(b)));
        int n=actions.Count;float width=n>0?(584-8*(n-1))/n:584;
        for(int i=0;i<n;i++)
        {
            var b=actions[i];b.gameObject.SetActive(true);Place((RectTransform)b.transform,20+i*(width+8),614,width,58);
            int order=ActionOrder(b);Skin(b.transform,order==0?theme.green:order==1?theme.orange:theme.red);
            StyleText(b.GetComponentInChildren<TMP_Text>(true),19,Color.white);b.transform.SetAsLastSibling();
        }
    }
    static int ActionOrder(Button b)
    {var t=b.GetComponentInChildren<TMP_Text>(true);string s=t!=null?t.text.Trim().ToUpperInvariant():"";return (s.StartsWith("MELHORAR") || s.StartsWith("REFINAR"))?1:s.StartsWith("VENDER")?2:0;}
    static string Stats(ItemInstance i)=>i==null?"Dano 0\nVida 0 · Defesa 0":"Dano +"+i.bonusDamage.ToString("0.#")+"\nVida +"+i.bonusHealth.ToString("0.#")+" · Defesa +"+i.bonusDefense.ToString("0.#");
}
