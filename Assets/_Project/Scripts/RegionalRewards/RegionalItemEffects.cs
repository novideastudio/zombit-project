using UnityEngine;
public static class RegionalEffects
{
    static Health directTarget;public static bool AnyFallen;
    public static void ResetRun(){directTarget=null;AnyFallen=false;}
    public static void TakeDirect(Health target,float amount)
    {if(target==null)return;var old=directTarget;directTarget=target;try{target.Take(amount);}finally{directTarget=old;}}
    public static float FilterDamage(Health h,float amount){var fx=h!=null?h.GetComponent<RegionalItemEffects>():null;return fx!=null?fx.FilterDamage(amount,directTarget==h):amount;}
    public static float FireRate(Hero hero){var fx=hero!=null?hero.GetComponent<RegionalItemEffects>():null;return fx!=null?fx.FireRateMultiplier:1;}
    public static float ShotMultiplier(Hero hero,Enemy enemy)
    {return !GameSession.DebugRun && !GameSession.IsRange && hero!=null && enemy!=null && enemy.data!=null && enemy.data.tier!=Tier.Common && RegionalRewards.Has(hero.GetComponent<PlayerStats>(),"centro")?1+RegionalRewards.Config.eliteShotBonus:1;}
    public static void OnSwap(PlayerStats a,PlayerStats b){foreach(var ps in new[]{a,b}){var fx=ps!=null?ps.GetComponent<RegionalItemEffects>():null;if(fx!=null)fx.Swapped();}}
}
public sealed class RegionalItemEffects : MonoBehaviour
{
    PlayerStats stats;Hero hero;Health hp;MissionFlow flow;
    float previous,lastDamage,healStart=-1,shield,shieldUntil,nextShield,swapUntil,nextSwap,reloadUntil;
    int hits;bool hadBoots,recovered,wasReloading;
    public float FireRateMultiplier=>RegionalRewards.Has(stats,"metro") && Time.time<swapUntil?1+RegionalRewards.Config.swapFireRate:1;
    void Start(){stats=GetComponent<PlayerStats>();hero=GetComponent<Hero>();hp=GetComponent<Health>();flow=FindFirstObjectByType<MissionFlow>();lastDamage=Time.time;if(hp!=null){previous=hp.Current;hp.Changed+=Changed;hp.Died+=Fell;}}
    void Fell(){RegionalEffects.AnyFallen=true;}
    void OnDestroy(){if(hp!=null){hp.Changed-=Changed;hp.Died-=Fell;}}
    void Changed(float current,float max)
    {
        var c=RegionalRewards.Config;
        if(current<previous){lastDamage=Time.time;healStart=-1;recovered=false;
            if(current>0 && current<max*c.emergencyThreshold && Time.time>=nextShield && RegionalRewards.Has(stats,"hospital"))
            {shield=max*c.emergencyShield;shieldUntil=Time.time+c.emergencyDuration;nextShield=Time.time+c.emergencyCooldown;}}
        previous=current;
    }
    public float FilterDamage(float amount,bool direct)
    {
        if(stats==null)return amount;var c=RegionalRewards.Config;
        if(direct)
        {
            var front=HeroSquad.FrontMember();if(front!=null && front.stats==stats && RegionalRewards.Has(stats,"residencial"))amount*=1-Mathf.Clamp01(c.frontReduction);
            if(RegionalRewards.Has(stats,"delegacia")){if(hits>=Mathf.Max(1,c.interventionHits)){amount*=1-Mathf.Clamp01(c.interventionReduction);hits=0;}else hits++;}else hits=0;
            if(Time.time<reloadUntil && RegionalRewards.Has(stats,"base"))amount*=1-Mathf.Clamp01(c.reloadReduction);
        }
        if(Time.time<shieldUntil && RegionalRewards.Has(stats,"hospital")){float absorb=Mathf.Min(amount,Mathf.Max(0,shield));amount-=absorb;shield-=absorb;}
        return Mathf.Max(0,amount);
    }
    public void Swapped(){if(!RegionalRewards.Has(stats,"metro") || Time.time<nextSwap)return;var c=RegionalRewards.Config;swapUntil=Time.time+c.swapDuration;nextSwap=Time.time+c.swapCooldown;}
    void Update()
    {
        if(hp==null || hp.Current<=0 || Time.timeScale<=0 || (flow!=null && flow.IsFinished))return;var c=RegionalRewards.Config;bool boots=RegionalRewards.Has(stats,"suburbio");
        if(boots && !hadBoots){lastDamage=Time.time;healStart=-1;recovered=false;}hadBoots=boots;
        if(boots && !recovered && Time.time-lastDamage>=c.bootsDelay){if(healStart<0)healStart=Time.time;if(Time.time-healStart<c.bootsDuration)hp.Heal(hp.max*c.bootsHealPerSecond*Time.deltaTime);else recovered=true;}
        if(!RegionalRewards.Has(stats,"hospital"))shield=0;
        if(hero!=null){bool reload=hero.Reloading;if(wasReloading && !reload && RegionalRewards.Has(stats,"base"))reloadUntil=Time.time+c.reloadProtection;wasReloading=reload;}
    }
}
