#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ZomBitCampaignReferenceInstaller
{
    const string Art="Assets/_Project/Art/CampaignReferenceGeneric/";
    const string Asset="Assets/_Project/Resources/Config/CampaignReferenceTheme.asset";
    [MenuItem("ZomBit/Campanha/Preparar visual da referencia")]
    static void Prepare()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Campanha","Saia do Play Mode primeiro.","OK");return;}
        string[] names={"frame_dark","frame_gold","frame_red","map","formation","info","start","coin","gear"};
        foreach(string name in names)
        {
            string path=Art+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null){Debug.LogError("CampaignReference: PNG não encontrado: "+path);return;}
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spriteBorder=name.StartsWith("frame_")?new Vector4(20,20,20,20):Vector4.zero;
            importer.SaveAndReimport();
        }
        var theme=AssetDatabase.LoadAssetAtPath<ZomBitCampaignReferenceTheme>(Asset);
        if(theme==null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Asset));
            AssetDatabase.Refresh();theme=ScriptableObject.CreateInstance<ZomBitCampaignReferenceTheme>();
            AssetDatabase.CreateAsset(theme,Asset);
        }
        Undo.RecordObject(theme,"Preparar tema campanha");
        if(theme.normalFrame==null)theme.normalFrame=Load("frame_dark");
        if(theme.goldFrame==null)theme.goldFrame=Load("frame_gold");
        if(theme.selectedFrame==null)theme.selectedFrame=Load("frame_red");
        if(theme.mapIcon==null)theme.mapIcon=Load("map");
        if(theme.rulesIcon==null)theme.rulesIcon=Load("info");
        if(theme.formationIcon==null)theme.formationIcon=Load("formation");
        if(theme.startIcon==null)theme.startIcon=Load("start");
        if(theme.coinIcon==null)theme.coinIcon=Load("coin");
        if(theme.gearIcon==null)theme.gearIcon=Load("gear");
        EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();Selection.activeObject=theme;
        EditorUtility.DisplayDialog("Tema preparado","Abra MainMenu e Campanha para testar.\nPara trocar sprites e posições, selecione o asset CampaignReferenceTheme no Project.\nNenhum script da campanha foi sobrescrito.","OK");
    }
    static Sprite Load(string name){return AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");}
}
#endif
