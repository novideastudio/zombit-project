#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ZomBitVisualPresetFinal
{
    [MenuItem("ZomBit/Combate/Salvar configuracao visual da captura")]
    static void Apply()
    {
        if(Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Configuracao visual","Saia do Play Mode para salvar os valores definitivamente.","OK");return;
        }
        const string path="Assets/_Project/Resources/Config/CombatVisualProfile.asset";
        var profile=AssetDatabase.LoadAssetAtPath<CombatVisualProfile>(path);
        if(profile==null)
        {EditorUtility.DisplayDialog("Nenhum arquivo alterado","Perfil nao encontrado em: "+path,"OK");return;}
        if(!EditorUtility.DisplayDialog("Salvar configuracao?","Desativa Grade Characters e neutraliza as duas cores. Aplica sombras, dispersao e chuva conforme a captura. Nao altera alcance da torreta.","Salvar","Cancelar"))return;
        string backup=Path.Combine("ZomBit_Backups","VisualPreset_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backup);File.Copy(path,Path.Combine(backup,"CombatVisualProfile.asset"),true);
        Undo.RecordObject(profile,"Configuracao visual aprovada");
        profile.gradeCharacters=false;
        profile.heroTint=Color.white;profile.zombieTint=Color.white;
        profile.alwaysShowShadows=true;
        profile.shadowOpacityMult=1.38f;
        profile.shadowByHeight=true;
        profile.shadowWidthPerHeight=1.5f;
        profile.shadowAspectMult=.737f;
        profile.spreadZombieLane=true;
        profile.laneSpreadY=.152f;
        profile.polishRain=false;
        profile.rainSplashes=true;
        profile.rainSplashCount=24;
        EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();Selection.activeObject=profile;
        EditorUtility.DisplayDialog("Configuracao salva","Valores gravados no asset. Reinicie o Play Mode para conferir.\nBackup: "+backup+"\nNao execute novamente o preset antigo de sombras, pois ele altera estes valores.","OK");
    }
}
#endif
