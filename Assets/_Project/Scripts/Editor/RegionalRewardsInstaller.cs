#if UNITY_EDITOR
using UnityEditor;
public static class RegionalRewardsInstaller
{
    [MenuItem("ZomBit/Recompensas/Instalar recompensas regionais")]
    static void Install(){CompleteProgressionInstaller.Install();}
}
#endif
