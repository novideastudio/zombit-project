using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]public class CommerceOffer{public ItemInstance item;public int price,level;public bool sold;}
[Serializable]public class CommerceStock{public string survivor,day;public int generatedLevel;public List<CommerceOffer> offers=new List<CommerceOffer>();}
[Serializable]public class CommerceState
{
    public int version=1;public string seed="",claimedWeek="";
    public List<CommerceStock> stocks=new List<CommerceStock>();public List<ItemInstance> pending=new List<ItemInstance>();
}
public static class TimedCommerce
{
    static readonly FieldInfo storeField=typeof(SaveData).GetField("commerceJson"),levelField=typeof(ItemInstance).GetField("requiredLevel");
    static DailyCommerceConfig config;
    public static bool Installed=>storeField!=null && levelField!=null;
    public static DailyCommerceConfig Config
    {
        get{if(config==null)config=Resources.Load<DailyCommerceConfig>("Config/DailyCommerceConfig");
            if(config==null){config=ScriptableObject.CreateInstance<DailyCommerceConfig>();config.hideFlags=HideFlags.HideAndDontSave;}return config;}
    }
    public static string Day=>DateTime.UtcNow.ToString("yyyy-MM-dd");
    public static string Week
    {get{var d=DateTime.UtcNow.Date;return d.AddDays(-(((int)d.DayOfWeek+6)%7)).ToString("yyyy-MM-dd");}}
    public static int Level(SaveData save,string id)
    {return save==null?1:Mathf.Clamp(save.GetProgress(id).level,1,Mathf.Max(1,Config.maxLevel));}
    public static int Required(ItemInstance item)=>item!=null && levelField!=null?Mathf.Max(0,(int)levelField.GetValue(item)):0;
    public static bool CanEquip(ItemInstance item,int level)=>item!=null && Required(item)<=level;
    public static bool CanEquip(SaveData save,string survivor,string uid)
    {
        if(save==null || save.inventory==null)return false;
        foreach(var it in save.inventory)if(it!=null && it.uid==uid)return CanEquip(it,Level(save,string.IsNullOrEmpty(survivor)?save.selectedSurvivor:survivor));
        return false;
    }
    public static CommerceState Read(SaveData save)
    {
        var s=new CommerceState();string json=save!=null && storeField!=null?storeField.GetValue(save) as string:null;
        if(!string.IsNullOrEmpty(json))s=JsonUtility.FromJson<CommerceState>(json)??s;
        if(s.stocks==null)s.stocks=new List<CommerceStock>();if(s.pending==null)s.pending=new List<ItemInstance>();return s;
    }
    static void Write(SaveData save,CommerceState s){if(!Installed)throw new InvalidOperationException("Instale o pacote da loja primeiro.");storeField.SetValue(save,JsonUtility.ToJson(s));}
    static int Hash(string s){unchecked{uint h=2166136261;foreach(char c in s){h^=c;h*=16777619;}return (int)(h&0x7fffffff);}}
    static System.Random RandomFor(CommerceState s,SaveData save,string key)
    {
        if(string.IsNullOrEmpty(s.seed))s.seed=Guid.NewGuid().ToString("N");
        return new System.Random(Hash(s.seed+"|"+ZombitCloud.Uid+"|"+save.slot+"|"+key));
    }
    public static CommerceStock EnsureDaily(SaveData save)
    {
        if(save==null || !Installed)throw new InvalidOperationException("Carregue o save e instale o pacote da loja.");
        var s=Read(save);string today=Day;string id=save.selectedSurvivor;CommerceStock stock=null;
        foreach(var x in s.stocks)if(x!=null && x.survivor==id){stock=x;break;}
        // A clock rollback does not regenerate an older stock. Future-clock manipulation is not server-protected.
        if(stock!=null && string.CompareOrdinal(stock.day,today)>=0 && stock.offers!=null && stock.offers.Count>0)return stock;
        var rng=RandomFor(s,save,"shop|"+id+"|"+today);
        var next=new CommerceStock{survivor=id,day=today,generatedLevel=Level(save,id)};
        var slots=new List<ItemSlot>{ItemSlot.Arma,ItemSlot.Capacete,ItemSlot.Colete,ItemSlot.Calca,ItemSlot.Botas,ItemSlot.Mochila};
        for(int i=slots.Count-1;i>0;i--){int j=rng.Next(i+1);var t=slots[i];slots[i]=slots[j];slots[j]=t;}
        foreach(var slot in slots)
        {
            int level=rng.Next(Mathf.Max(1,next.generatedLevel-2),next.generatedLevel+1);var item=Create(rng,slot,level);
            float[] rarityPrice={.8f,1,1.6f,2.8f,5};
            int price=Mathf.Max(1,Mathf.RoundToInt((Config.basePrice+level*Config.pricePerLevel)*rarityPrice[(int)item.rarity]*(.9f+(float)rng.NextDouble()*.2f)));
            item.sellValue=Mathf.Max(1,price/5);next.offers.Add(new CommerceOffer{item=item,level=level,price=price});
        }
        string before=storeField.GetValue(save) as string;
        if(stock!=null)s.stocks.Remove(stock);s.stocks.Add(next);Write(save,s);
        try{SaveSystem.Save(save);}catch{storeField.SetValue(save,before);throw;}
        return next;
    }
    static Rarity RollRarity(System.Random r,int level)
    {
        double x=r.NextDouble();
        if(level>=30 && x<.02)return Rarity.Lendario;
        if(level>=15 && x<.10)return Rarity.Epico;
        if(level>=5 && x<.32)return Rarity.Raro;
        if(x<.70)return Rarity.Incomum;return Rarity.Comum;
    }
    static ItemInstance Create(System.Random rng,ItemSlot slot,int level)
    {
        var rarity=RollRarity(rng,level);float[] factors={.8f,1,1.2f,1.45f,1.75f};
        float f=factors[(int)rarity]*(.9f+(float)rng.NextDouble()*.2f);
        string name=SlotName(slot);float damage=slot==ItemSlot.Arma?(.8f+level*.6f)*f:0;
        float hp=slot==ItemSlot.Arma?0:(4+level*2)*f;float defense=slot==ItemSlot.Arma?0:(.3f+level*.2f)*f;
        var item=new ItemInstance{uid=Guid.NewGuid().ToString("N"),name=name+(slot==ItemSlot.Calca || slot==ItemSlot.Botas?" táticas":slot==ItemSlot.Mochila?" tática":" tático"),description="Equipamento de nível "+level+". Requer personagem no nível "+level+" ou acima.",slot=slot,rarity=rarity,
            bonusDamage=(float)Math.Round(damage,1),bonusHealth=Mathf.Round(hp),bonusDefense=(float)Math.Round(defense,1),upgradeLevel=0,sellValue=10+level*5};
        if(levelField!=null)levelField.SetValue(item,level);
        if(rarity>=Rarity.Raro)
        {
            var type=slot==ItemSlot.Arma?AffixType.Damage:AffixType.Regen;
            item.affixes.Add(new ItemAffix{type=type,value=type==AffixType.Damage?.02f:.001f});
        }
        return item;
    }
    public static string SlotName(ItemSlot s)
    {switch(s){case ItemSlot.Arma:return "Módulo";case ItemSlot.Capacete:return "Capacete";case ItemSlot.Colete:return "Colete";case ItemSlot.Calca:return "Calças";case ItemSlot.Botas:return "Botas";default:return "Mochila";}}
    public static string Buy(SaveData save,string survivor,string day,string uid)
    {
        if(save==null || !Installed)return "Nenhum jogo carregado.";
        if(save.selectedSurvivor!=survivor)return "O personagem selecionado mudou. Reabra a loja.";
        var stock=EnsureDaily(save);if(stock.day!=day)return "As ofertas foram renovadas. Confira a loja novamente.";
        var state=Read(save);CommerceOffer offer=null;
        foreach(var s in state.stocks)if(s.survivor==survivor && s.day==day)foreach(var o in s.offers)if(o.item.uid==uid){offer=o;break;}
        if(offer==null || offer.sold)return "Oferta indisponível ou já comprada.";
        if(offer.level>Level(save,survivor))return "Seu nível atual não permite comprar este item.";
        if(save.gold<offer.price)return "Ouro insuficiente.";
        if(save.inventory==null)save.inventory=new List<ItemInstance>();if(save.inventory.Count>=Config.inventoryCapacity)return "Mochila cheia. Libere espaço antes de comprar.";
        string before=storeField.GetValue(save) as string;int gold=save.gold;offer.sold=true;save.gold-=offer.price;save.inventory.Add(offer.item);Write(save,state);
        try{SaveSystem.Save(save);return "Comprado: "+offer.item.name+" · Nv. "+offer.level;}
        catch(Exception e){save.inventory.Remove(offer.item);save.gold=gold;storeField.SetValue(save,before);Debug.LogError(e);return "Falha ao salvar a compra; memória restaurada. Confira os backups se houve erro de disco.";}
    }
    static SaveData giftCacheOwner;static string giftCacheJson,giftCacheWeek;static bool giftCacheValue;
    public static bool GiftAvailable(SaveData save)
    {
        if(save==null || !Installed)return false;string json=storeField.GetValue(save) as string,week=Week;
        if(giftCacheOwner==save && giftCacheJson==json && giftCacheWeek==week)return giftCacheValue;
        giftCacheOwner=save;giftCacheJson=json;giftCacheWeek=week;
        giftCacheValue=string.CompareOrdinal(Read(save).claimedWeek??"",week)<0;return giftCacheValue;
    }
    public static string ClaimGift(SaveData save)
    {
        if(save==null || !Installed)return "Carregue um jogo e instale o pacote.";
        string week=Week;var state=Read(save);
        if(string.CompareOrdinal(state.claimedWeek??"",week)>=0)return "Você já recebeu os presentes desta semana.";
        var rng=RandomFor(state,save,"gift|"+week);int cap=Level(save,save.selectedSurvivor);
        var items=new List<ItemInstance>();for(int i=0;i<5;i++)items.Add(Create(rng,(ItemSlot)rng.Next(0,6),rng.Next(Mathf.Max(1,cap-2),cap+1)));
        state.claimedWeek=week;if(save.inventory==null)save.inventory=new List<ItemInstance>();
        bool room=save.inventory.Count+5<=Config.inventoryCapacity;
        string old=storeField.GetValue(save) as string;
        if(room)save.inventory.AddRange(items);else state.pending.AddRange(items);Write(save,state);
        try{SaveSystem.Save(save);return room?"Você recebeu 5 equipamentos. Confira a mochila!":"Seus 5 equipamentos foram reservados. Resgate os pendentes pela loja quando houver espaço.";}
        catch(Exception e){if(room)foreach(var it in items)save.inventory.Remove(it);storeField.SetValue(save,old);Debug.LogError(e);return "Falha ao salvar presentes; tente novamente após verificar o Console.";}
    }
    public static string ClaimPending(SaveData save)
    {
        if(save==null || !Installed)return "Nenhum save disponível.";
        var s=Read(save);if(s.pending.Count==0)return "Nenhum presente pendente.";
        if(save.inventory==null)save.inventory=new List<ItemInstance>();int count=Mathf.Min(Mathf.Max(0,Config.inventoryCapacity-save.inventory.Count),s.pending.Count);
        if(count==0)return "Mochila cheia.";
        string old=storeField.GetValue(save) as string;var moved=s.pending.GetRange(0,count);s.pending.RemoveRange(0,count);save.inventory.AddRange(moved);Write(save,s);
        try{SaveSystem.Save(save);return count+" presentes resgatados.";}catch(Exception e){foreach(var i in moved)save.inventory.Remove(i);storeField.SetValue(save,old);Debug.LogError(e);return "Falha ao salvar o resgate.";}
    }
}
