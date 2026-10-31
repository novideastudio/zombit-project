using UnityEngine;

[CreateAssetMenu(menuName="Zombit/Visual/Combat Visual Profile")]
public sealed class CombatVisualProfile : ScriptableObject
{
    [Header("Characters vs scenery (multiplicative grade)")]
    public bool gradeCharacters = true;
    public Color heroTint = new Color(0.94f, 0.88f, 0.82f, 1f);
    public Color zombieTint = new Color(0.92f, 0.86f, 0.82f, 1f);

    [Header("Contact shadows")]
    public bool alwaysShowShadows = true;
    [Range(0.5f, 3f)] public float shadowOpacityMult = 1.45f;
    public bool shadowByHeight = true;
    [Range(0.2f, 1.5f)] public float shadowWidthPerHeight = 0.62f;
    [Range(0.5f, 2f)] public float shadowAspectMult = 1.2f;

    [Header("Zombie lane spread")]
    public bool spreadZombieLane = true;
    [Range(0f, 0.8f)] public float laneSpreadY = 0.30f;

    [Header("Rain polish")]
    public bool polishRain = true;
    public bool rainSplashes = true;
    [Range(0, 60)] public int rainSplashCount = 22;
}
