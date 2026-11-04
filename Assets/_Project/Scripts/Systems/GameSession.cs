public static class GameSession
{
    public static GameMode Mode = GameMode.Campaign;
    public static bool IsCampaign => Mode == GameMode.Campaign && !IsRange;
    public static bool IsLastStand => Mode == GameMode.LastStand;
    public static bool IsBossRush => Mode == GameMode.BossRush;
    public static bool IsRefugeDefense => Mode == GameMode.RefugeDefense;
    public const int MenuSceneIndex = 0;
    public const int GameSceneIndex = 1;

    public static SaveData Save;
    public static SurvivorDef Survivor;
    /// <summary>Segundo sobrevivente da missao (modo dupla) ou null (solo).</summary>
    public static SurvivorDef Partner;
    /// <summary>Terceiro sobrevivente (modo trio), ou null quando a formacao nao o utiliza.</summary>
    public static SurvivorDef Third;
    public static LocationDef Location;
    public static bool OpenMapOnLoad;
    /// <summary>Abre o acampamento principal depois de sair de uma atividade livre, sem voltar ao mapa da campanha.</summary>
    public static bool OpenHomeOnLoad;
    public static bool DebugRun;
    /// <summary>True no estande de tiro (sem ondas, recompensas ou penalidades).</summary>
    public static bool IsRange => Location != null && Location.id == "range";

    /// <summary>Nivel escolhido no mapa: 1 Facil, 2 Normal, 3 Dificil, 4 Pesadelo.</summary>
    public static int Difficulty = 2;
    /// <summary>Seguro pago no mapa para a PROXIMA run (consumido pelo MissionSetup).</summary>
    public static bool InsuranceBought;

    public static void EnsureDefaults()
    {
        if (Save == null) { Save = new SaveData(); DebugRun = true; }
        if (Survivor == null) Survivor = GameDatabase.GetSurvivor(Save.selectedSurvivor);
        if (Partner != null && Survivor != null && Partner.id == Survivor.id) Partner = null;
        if (Third != null && (Partner == null || Third.id == Partner.id || (Survivor != null && Third.id == Survivor.id))) Third = null;
        if (Location == null) Location = GameDatabase.Locations[0];
    }

    public static void Reset()
    {
        Save = null; Survivor = null; Partner = null; Third = null; Location = null;
        Mode = GameMode.Campaign;
        OpenMapOnLoad = false; OpenHomeOnLoad = false; DebugRun = false;
        Difficulty = 2; InsuranceBought = false;
    }
}
