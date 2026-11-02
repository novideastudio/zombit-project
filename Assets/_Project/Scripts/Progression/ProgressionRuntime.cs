using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class ProgressionRuntime
{
    static ProgressionConfig config;
    public static ProgressionConfig Config
    {
        get
        {
            if(config==null)config=Resources.Load<ProgressionConfig>("Config/ProgressionConfig");
            if(config==null){config=ScriptableObject.CreateInstance<ProgressionConfig>();config.hideFlags=HideFlags.HideAndDontSave;}
            return config;
        }
    }
    public static ProgressionConfig.Region Region(string id)
    {foreach(var r in Config.regions)if(r!=null && r.id==id)return r;return null;}
    public static int Order(string id)
    {for(int i=0;i<Config.regions.Length;i++)if(Config.regions[i].id==id)return i;return 99;}
    public static void PrepareRegions()
    {
        foreach(var loc in GameDatabase.Locations)
        {
            var r=Region(loc.id);if(r==null)continue;
            loc.requires=new string[0];loc.recommendedLevel=r.level;loc.waves=r.waves;loc.countMult=r.spawn;
            loc.goldReward=Mathf.RoundToInt(r.goldReward);
            loc.difficulty=Mathf.Clamp(1+Order(loc.id)/2,1,5);
            if(loc.id=="industrial")loc.hasBoss=true;
        }
        GameDatabase.Locations.Sort((a,b)=>Order(a.id).CompareTo(Order(b.id)));
    }
    public static DifficultyDef BattleDifficulty(int value)
    {
        int i=Mathf.Clamp(value,1,4)-1;var source=Difficulties.All[i];var d=new DifficultyDef();
        foreach(var field in typeof(DifficultyDef).GetFields(BindingFlags.Public|BindingFlags.Instance))field.SetValue(d,field.GetValue(source));
        d.zombieHealth=Config.tierHp[i];d.zombieDamage=Config.tierDamage[i];
        d.countMult=Config.tierSpawn[i];d.hordeRate=Config.tierSpawn[i];return d;
    }
    public static void ConfigureBattle(LocationDef loc,DifficultyDef diff,int squadSize)
    {
        if(GameSession.IsRange || Region(loc!=null?loc.id:null)==null)return;
        var r=Region(loc.id);float squadHp=HeroSquad.ZombieHealthMultFor(squadSize);
        Enemy.HealthMult=r.hp*diff.zombieHealth*squadHp;
        Enemy.BossHealthMult=Balance.BossHealthBase*r.hp*diff.zombieHealth*squadHp;
        Enemy.DamageMult=r.damage*diff.zombieDamage;
        diff.eliteShare=Mathf.Clamp(diff.eliteShare+Order(loc.id)*Config.eliteSharePerRegion,0,.3f);
        diff.spitterShare=Mathf.Clamp(diff.spitterShare+Order(loc.id)*Config.spitterSharePerRegion,0,.35f);
    }
    public static Balance.Zone Zone(string id)
    {
        var r=Region(id);if(r==null)return new Balance.Zone(.4f,.12f,.45f);
        float gain=Mathf.Pow(Mathf.Max(1,Config.rewardGrowth),Order(id))/Mathf.Max(.1f,r.spawn);
        return new Balance.Zone(Config.baseXp*gain,Config.baseGold*gain,Config.baseScrap*gain);
    }
    public static int RetainScrap(int amount)=>Mathf.RoundToInt(Mathf.Max(0,amount)*Mathf.Clamp01(Config.failedScrapRetention));
    public static int Recommended(LocationDef loc,int difficulty)
    {var r=Region(loc!=null?loc.id:null);return r==null?100:Mathf.RoundToInt(r.power*Config.tierPower[Mathf.Clamp(difficulty,1,4)-1]);}
    public static List<string> ActiveIds(SaveData save,int difficulty)
    {
        var list=new List<string>();if(save==null)return list;
        foreach(string id in new[]{save.selectedSurvivor,save.selectedPartner,difficulty>=HeroSquad.TrioDifficulty?save.selectedThird:""})
        {
            if(string.IsNullOrEmpty(id) || list.Contains(id))continue;
            foreach(var d in GameDatabase.Survivors)if(d.id==id){list.Add(id);break;}
        }
        return list;
    }
    struct Estimate{public float dps,ehp,support;}
    static Estimate EstimateFor(SaveData save,string id)
    {
        SurvivorDef def=null;foreach(var d in GameDatabase.Survivors)if(d.id==id)def=d;
        if(save==null || def==null)return default;
        var p=save.GetProgress(id);var sk=SkillTreeSystem.GetBonuses(id,p);var a=AffixBonuses.Sum(p.equipped);
        var w=WeaponCatalog.ForSurvivor(def);
        float hp=100+20*(Mathf.Max(1,p.level)-1),damage=2*(Mathf.Max(1,p.level)-1),defense=Mathf.Max(0,p.level-1);
        if(p.equipped!=null)foreach(var item in p.equipped)
        {if(item==null)continue;hp+=item.bonusHealth;damage+=item.bonusDamage;defense+=item.bonusDefense;}
        hp*=def.healthMult*Mathf.Max(.1f,1+sk.healthPercent);defense+=sk.defenseFlat;
        float shot=(w.damage+damage*4/Mathf.Max(.1f,w.shotsPerSecond))*def.damageMult*(1+sk.damagePercent)*(1+a.damage);
        float crit=Mathf.Clamp01(def.critChance+sk.critChance+a.critChance);
        shot*=1+crit*(Mathf.Max(1,def.critMult*(1+sk.critDamagePercent+a.critDamage))-1);
        float accuracy=Mathf.Clamp01(w.accuracy+def.accuracyBonus+sk.accuracyBonus+a.accuracy);
        shot*=accuracy+(1-accuracy)*.6f;
        float rate=w.shotsPerSecond*def.fireRateMult*(1+sk.fireRatePercent)*(1+a.fireRate);
        float dps=shot*Mathf.Max(1,w.magazineSize)/(Mathf.Max(1,w.magazineSize)/Mathf.Max(.1f,rate)+Mathf.Max(0,w.reloadTime));
        // Limited credit for situational utility; not an exact combat simulation.
        if(def.utility==UtilityType.Grenade)dps*=1.12f+Mathf.Clamp(sk.abilityPowerPercent,0,1)*.12f;
        if(def.utility==UtilityType.Turret)dps*=1.25f+Mathf.Clamp(sk.abilityPowerPercent+sk.turretFireRatePercent,0,1)*.2f;
        float ehp=hp*(1+Mathf.Max(0,defense)/Health.DefenseHalfPoint);
        ehp*=1+Mathf.Clamp(a.regen*8,0,.25f);
        if(RegionalRewards.HasSaved(save,id,"suburbio"))ehp*=1.025f;
        if(RegionalRewards.HasSaved(save,id,"residencial"))ehp*=1.04f;
        if(RegionalRewards.HasSaved(save,id,"hospital"))ehp*=1.08f;
        if(RegionalRewards.HasSaved(save,id,"delegacia"))ehp*=1.03f;
        if(RegionalRewards.HasSaved(save,id,"base"))ehp*=1.03f;
        if(RegionalRewards.HasSaved(save,id,"centro"))dps*=1.04f;
        if(RegionalRewards.HasSaved(save,id,"metro"))dps*=1.03f;
        return new Estimate{dps=Mathf.Max(1,dps),ehp=Mathf.Max(1,ehp),support=def.utility==UtilityType.FieldMedic?Mathf.Min(.2f,.10f*(1+sk.healingPercent)):0};
    }
    public static int IndividualPower(SaveData save,string id)
    {
        var e=EstimateFor(save,id);
        return Score(e.dps,e.ehp,e.support);
    }
    static int Score(float dps,float ehp,float support)
    {
        float index=Mathf.Pow(Mathf.Max(.01f,dps)/Mathf.Max(1,Config.referenceDps),.6f)*
            Mathf.Pow(Mathf.Max(.01f,ehp)/Mathf.Max(1,Config.referenceEhp),.4f)*(1+support);
        return Mathf.Max(1,Mathf.RoundToInt(100*Mathf.Pow(index,Config.scoreExponent)));
    }
    public static int TeamPower(SaveData save,int difficulty)
    {
        var ids=ActiveIds(save,difficulty);if(ids.Count==0)return 0;
        float dps=0,ehp=0,weights=0,support=0;
        for(int i=0;i<ids.Count;i++)
        {
            var e=EstimateFor(save,ids[i]);float weight=i==0?1:i==1?.55f:.3f;
            dps+=e.dps;ehp+=e.ehp*weight;weights+=weight;support=Mathf.Max(support,e.support);
        }
        return Score(dps/HeroSquad.HordeMultFor(ids.Count),ehp/weights,support);
    }
    public static string Risk(int have,int need)
    {
        float ratio=(float)have/Mathf.Max(1,need);
        return ratio<Config.extremeBelow?"AMEAÇA EXTREMA":ratio<Config.highBelow?"AMEAÇA ALTA":ratio<Config.challengingBelow?"DESAFIADOR":"ACIMA DA RECOMENDAÇÃO";
    }
    public static string Describe(SaveData save,LocationDef loc,int difficulty)
    {
        int have=TeamPower(save,difficulty),need=Recommended(loc,difficulty);
        return "Poder da equipe: "+have+" / recomendado: "+need+" · "+Risk(have,need)+"\nEstimativa de build, não garantia de vitória.";
    }
    static int startPower;
    public static void BeginRun(SaveData save,LocationDef loc)
    {startPower=TeamPower(save,GameSession.Difficulty);}
    public static void RecordResult(bool win,LocationDef loc,float seconds,int gold,int scrap)
    {
        if(GameSession.DebugRun || GameSession.IsRange || loc==null)return;
        try
        {
            string line=System.DateTime.UtcNow.ToString("o")+","+loc.id+","+GameSession.Difficulty+","+startPower+","+Recommended(loc,GameSession.Difficulty)+","+
                (win?1:0)+","+seconds.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+","+gold+","+scrap+","+HeroSquad.AliveCount+"\n";
            string path=System.IO.Path.Combine(Application.persistentDataPath,"zombit_progression_playtest.csv");
            if(!System.IO.File.Exists(path))System.IO.File.WriteAllText(path,"utc,region,difficulty,startPower,recommended,win,seconds,gold,scrap,alive\n");
            System.IO.File.AppendAllText(path,line);
        }
        catch(System.Exception e){Debug.LogWarning("Telemetry local: "+e.Message);}
    }

}
