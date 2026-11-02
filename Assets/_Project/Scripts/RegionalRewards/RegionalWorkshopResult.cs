public static class RegionalWorkshopResult
{
    public static string RewardsTitle(bool win)
    {
        if(!win || GameSession.DebugRun || GameSession.IsRange || !RegionalRewards.Installed || GameSession.Location==null || RegionalRewards.Get(GameSession.Location.id)==null)return "RECOMPENSAS";
        return "RECOMPENSAS · +"+UnityEngine.Mathf.Clamp(GameSession.Difficulty,1,4)+" SELOS";
    }
}
