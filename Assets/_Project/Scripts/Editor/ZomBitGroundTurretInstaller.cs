#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ZomBitGroundTurretInstaller
{
    [MenuItem("ZomBit/Combate/Aplicar sombras e alcance da torreta")]
    static void Apply()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Combate","Saia do Play Mode.","OK");return;}
        string file="Assets/_Project/Scripts/Combat/SurvivorAbilities.cs";
        if(!File.Exists(file)){EditorUtility.DisplayDialog("Cancelado","SurvivorAbilities.cs não encontrado.","OK");return;}
        string src=File.ReadAllText(file),find="w.range = 7f;",replacement="w.range = 18.5f; // ZOMBIT_TURRET_LONG_RANGE";
        bool installed=src.Contains(replacement);
        int at=src.IndexOf(find,StringComparison.Ordinal);
        if(!installed && (at<0 || src.IndexOf(find,at+find.Length,StringComparison.Ordinal)>=0))
        {EditorUtility.DisplayDialog("Cancelado","Trecho de alcance incompatível. Nenhum arquivo alterado pelo comando.","OK");return;}
        if(!EditorUtility.DisplayDialog("Aplicar ajustes?","Torreta: alcance 7 -> 18,5, mantendo dano/cadência.\nSombras: perfil global mais largo, escuro e achatado (heróis e zumbis).\nO YSort do ZIP já deve estar instalado; guarde seu backup anterior à extração.","Aplicar","Cancelar"))return;
        string backup=Path.Combine("ZomBit_Backups","GroundTurret_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backup);
        File.Copy(file,Path.Combine(backup,"SurvivorAbilities.cs"),true);
        string profilePath="Assets/_Project/Resources/Config/CombatVisualProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<CombatVisualProfile>(profilePath);
        if(profile!=null)
        {
            if(File.Exists(profilePath))File.Copy(profilePath,Path.Combine(backup,"CombatVisualProfile.asset"),true);
            Undo.RecordObject(profile,"Sombras de contato");
            profile.alwaysShowShadows=true;profile.shadowByHeight=true;
            profile.shadowWidthPerHeight=.78f;profile.shadowOpacityMult=1.9f;profile.shadowAspectMult=.85f;
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        }
        else Debug.LogWarning("Perfil visual não encontrado. YSort usa os padrões existentes de CombatVisuals; ajustes globais não foram aplicados.");
        if(!installed)File.WriteAllText(file,src.Replace(find,replacement));
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Aplicado","Aguarde recompilar. Teste o contato dos pés e a torreta no estande e nas ondas.\nBackup dos arquivos alterados pelo comando: "+backup,"OK");
    }
}
#endif
