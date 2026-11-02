using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public static class RegionalRewards
{
    static readonly FieldInfo stateField=typeof(SaveData).GetField("regionRewardsJson"),uniqueField=typeof(ItemInstance).GetField("regionUniqueId");
    static RegionalRewardConfig config;
    public static string LastMessage="";
    public static bool Installed=>stateField!=null && uniqueField!=null;
    public static RegionalRewardConfig Config
    {
        get{if(config==null)config=Resources.Load<RegionalRewardConfig>("Config/RegionalRewardConfig");
            if(config==null){config=ScriptableObject.CreateInstance<RegionalRewardConfig>();config.hideFlags=HideFlags.HideAndDontSave;}return config;}
    }
    public static RegionalRewardConfig.Region Get(string id){foreach(var r in Config.regions)if(r!=null && r.id==id)return r;return null;}
    public static string UniqueId(ItemInstance item)=>item!=null && uniqueField!=null?uniqueField.GetValue(item) as string:null;
    public static bool Has(PlayerStats ps,string id){if(ps!=null)foreach(var it in ps.EquippedList())if(UniqueId(it)==id)return true;return false;}
    public static bool HasSaved(SaveData save,string survivor,string id)
    {if(save==null)return false;var items=save.GetProgress(survivor).equipped;if(items!=null)foreach(var it in items)if(UniqueId(it)==id)return true;return false;}
    public static RegionalRewardState Read(SaveData save)
    {
        var s=new RegionalRewardState();string json=save!=null && stateField!=null?stateField.GetValue(save) as string:null;
        if(!string.IsNullOrEmpty(json))s=JsonUtility.FromJson<RegionalRewardState>(json)??s;
        if(s.records==null)s.records=new List<RegionalRecord>();if(s.pending==null)s.pending=new List<ItemInstance>();return s;
    }
    public static void Write(SaveData save,RegionalRewardState state){if(!Installed || save==null)throw new InvalidOperationException("Instale o pacote primeiro.");stateField.SetValue(save,JsonUtility.ToJson(state));}
    public static RegionalRecord Record(RegionalRewardState s,string id)
    {foreach(var r in s.records)if(r!=null && r.id==id)return r;var n=new RegionalRecord{id=id};s.records.Add(n);return n;}
    public static ItemInstance Create(RegionalRewardConfig.Region r)
    {
        float roll=UnityEngine.Random.Range(.95f,1.05f);
        var item=new ItemInstance{uid=Guid.NewGuid().ToString("N"),name=r.itemName,description=r.description,slot=r.slot,rarity=Rarity.Epico,
            bonusDamage=(float)Math.Round(r.damage*roll,1),bonusHealth=Mathf.Round(r.health*roll),bonusDefense=(float)Math.Round(r.defense*roll,1),sellValue=1};
        if(uniqueField!=null)uniqueField.SetValue(item,r.id);return item;
    }
    static bool TeamHas(string id){foreach(var m in HeroSquad.Members)if(m!=null && m.Alive && Has(m.stats,id))return true;return false;}
    public static int ExtraScrap(int amount)=>Installed && !GameSession.DebugRun && !GameSession.IsRange && TeamHas("shopping")?Mathf.RoundToInt(Mathf.Max(0,amount)*Config.shoppingScrapBonus):0;
    static void Store(ItemInstance item,RegionalRewardState s,Inventory inv,List<ItemInstance> drops)
    {if(inv!=null && inv.Add(item)){if(drops!=null)drops.Add(item);}else s.pending.Add(item);}
    public static void GrantVictory(SaveData save,LocationDef loc,int difficulty,bool win,Inventory inv,PlayerStats primary,LootDropper loot,List<ItemInstance> displayedDrops)
    {
        if(!Installed || !win || GameSession.DebugRun || GameSession.IsRange || save==null || loc==null)return;
        var r=Get(loc.id);if(r==null)return;RegionalRewardState state;
        try{state=Read(save);}catch(Exception e){LastMessage="Recompensa regional não concedida: dados inválidos. Preserve seu backup.";Debug.LogError(e);return;}
        var record=Record(state,loc.id);
        record.seals+=Mathf.Clamp(difficulty,1,4);record.wins++;
        if(difficulty>=2){record.blueprint=true;record.title=true;}if(difficulty>=4)record.frame=true;
        float chance=difficulty<=1?Config.easyDropChance:difficulty==2?Config.normalDropChance:difficulty==3?Config.hardDropChance:Config.nightmareDropChance;
        if(UnityEngine.Random.value<Mathf.Clamp01(chance))Store(Create(r),state,inv,displayedDrops);
        if(!RegionalEffects.AnyFallen && HeroSquad.Count>0 && HeroSquad.AliveCount==HeroSquad.Count && TeamHas("industrial") && loot!=null && loot.pool!=null && loot.pool.Length>0)
        {var it=LootTable.Roll(loot.pool,loc.difficulty,difficulty,Balance.LootSource.Completion);if(it!=null)Store(it,state,inv,displayedDrops);}
        if(inv!=null && primary!=null && GameSession.Survivor!=null)inv.SaveTo(save,GameSession.Survivor.id,primary);
        Write(save,state);LastMessage="+"+Mathf.Clamp(difficulty,1,4)+" selos de "+r.name+". Projeto: "+(record.blueprint?"liberado":"vença no Normal+");
    }
    public static string Craft(SaveData save,string id)
    {
        if(!Installed || save==null)return "Carregue um jogo e instale o pacote.";
        var r=Get(id);if(r==null)return "Região inválida.";var state=Read(save);var rec=Record(state,id);
        if(!rec.blueprint)return "Vença no Normal ou acima para obter o projeto.";
        if(rec.seals<r.sealsCost || save.gold<r.goldCost || save.scrap<r.scrapCost)return "Recursos insuficientes.";
        if(save.inventory==null)save.inventory=new List<ItemInstance>();if(save.inventory.Count>=60)return "Mochila cheia.";
        string old=stateField.GetValue(save) as string;int gold=save.gold,scrap=save.scrap;var item=Create(r);
        rec.seals-=Mathf.Max(0,r.sealsCost);save.gold-=Mathf.Max(0,r.goldCost);save.scrap-=Mathf.Max(0,r.scrapCost);save.inventory.Add(item);Write(save,state);
        try{SaveSystem.Save(save);return "Fabricado: "+r.itemName;}catch(Exception e){save.inventory.Remove(item);save.gold=gold;save.scrap=scrap;stateField.SetValue(save,old);Debug.LogError(e);return "Falha ao salvar; memória restaurada. Confira o backup se houver falha de disco.";}
    }
    public static string Claim(SaveData save)
    {
        if(!Installed || save==null)return "Carregue um jogo.";var s=Read(save);if(s.pending.Count==0)return "Nenhum item pendente.";
        if(save.inventory==null)save.inventory=new List<ItemInstance>();if(save.inventory.Count>=60)return "Mochila cheia.";
        string old=stateField.GetValue(save) as string;var item=s.pending[0];s.pending.RemoveAt(0);save.inventory.Add(item);Write(save,s);
        try{SaveSystem.Save(save);return "Resgatado: "+item.name;}catch(Exception e){save.inventory.Remove(item);stateField.SetValue(save,old);Debug.LogError(e);return "Falha ao salvar resgate.";}
    }
    public static string EquipCosmetic(SaveData save,string id,bool frame)
    {
        if(!Installed || save==null)return "Carregue um jogo.";var s=Read(save);var rec=Record(s,id);var r=Get(id);
        if(r==null || (frame?!rec.frame:!rec.title))return "Cosmético bloqueado.";
        string old=stateField.GetValue(save) as string;if(frame)s.selectedFrame=id;else s.selectedTitle=r.title;Write(save,s);
        try{SaveSystem.Save(save);return "Cosmético selecionado para os próximos combates.";}catch(Exception e){stateField.SetValue(save,old);Debug.LogError(e);return "Falha ao salvar cosmético.";}
    }
}
