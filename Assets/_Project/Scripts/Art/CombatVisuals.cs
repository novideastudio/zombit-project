using UnityEngine;

/// <summary>Shared access to the optional CombatVisualProfile asset. Missing asset = built-in defaults.</summary>
public static class CombatVisuals
{
    static CombatVisualProfile cached;
    static bool loaded;
    static int laneCounter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { cached = null; loaded = false; laneCounter = 0; }

    public static CombatVisualProfile Profile
    {
        get
        {
            if (!loaded)
            {
                cached = Resources.Load<CombatVisualProfile>("Config/CombatVisualProfile");
                loaded = true;
            }
            return cached;
        }
    }

    public static Color CharacterGrade(bool enemy)
    {
        var p = Profile;
        if (p == null) return enemy ? new Color(0.92f, 0.86f, 0.82f, 1f) : new Color(0.94f, 0.88f, 0.82f, 1f);
        if (!p.gradeCharacters) return Color.white;
        return enemy ? p.zombieTint : p.heroTint;
    }

    public static bool AlwaysShowShadows { get { var p = Profile; return p == null || p.alwaysShowShadows; } }
    public static float ShadowOpacityMult { get { var p = Profile; return p == null ? 1.45f : p.shadowOpacityMult; } }
    public static float ShadowAspectMult { get { var p = Profile; return p == null ? 1.2f : p.shadowAspectMult; } }

    /// <summary>Shadow width from the visible character height, so a long weapon pose does not stretch the shadow.</summary>
    public static float ShadowWidth(Sprite sprite, float legacyWidth, float scaleY)
    {
        var p = Profile;
        bool byHeight = p == null || p.shadowByHeight;
        if (!byHeight || sprite == null) return Mathf.Max(0.25f, legacyWidth);
        float perHeight = p == null ? 0.62f : p.shadowWidthPerHeight;
        return Mathf.Max(0.25f, SpriteMetrics.WorldHeight(sprite) * scaleY * perHeight);
    }

    public static Color ShadowColor(float alpha) => new Color(0.02f, 0.008f, 0.008f, Mathf.Clamp01(alpha));

    public static bool PolishRain { get { var p = Profile; return p == null || p.polishRain; } }
    public static bool RainSplashes { get { var p = Profile; return p == null || p.rainSplashes; } }
    public static int RainSplashCount { get { var p = Profile; return p == null ? 22 : p.rainSplashCount; } }

    /// <summary>Vertical offset for a newly spawned zombie around the road centre. Bosses stay centred.</summary>
    public static float NextLaneOffset(Tier tier)
    {
        var p = Profile;
        bool enabled = p == null || p.spreadZombieLane;
        float amplitude = p == null ? 0.30f : p.laneSpreadY;
        if (!enabled || amplitude <= 0f || tier == Tier.Boss) return 0f;
        laneCounter++;
        float frac = (laneCounter * 0.61803398875f) % 1f;
        return (frac - 0.5f) * 2f * amplitude * 0.9f + Random.Range(-0.1f, 0.1f) * amplitude;
    }
}
