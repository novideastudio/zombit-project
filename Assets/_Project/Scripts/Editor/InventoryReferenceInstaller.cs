#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class InventoryReferenceInstaller
{
    const string ThemePath="Assets/_Project/Resources/Config/InventoryReferenceTheme.asset";
    const string Art="Assets/_Project/Art/InventoryReferenceGeneric/";
    [MenuItem("ZomBit/Mochila/Aplicar layout da referencia")]
    static void Apply()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Mochila","Saia do Play Mode antes de instalar.","OK");return;}
        const string source="Assets/_Project/Scripts/UI/MainMenuController.SharedInventory.cs";
        const string find="        Show(\"mapInventory\");sharedInventoryView.Fit.Apply();";
        const string replace="        Show(\"mapInventory\");sharedInventoryView.Fit.Apply();\n        ApplyInventoryReferenceLayout(); // INVENTORY_REFERENCE_LAYOUT_V1";
        if(!File.Exists(source)){EditorUtility.DisplayDialog("Nada alterado","Controlador compartilhado não encontrado.","OK");return;}
        string original=File.ReadAllText(source),work=original.Replace("\r\n","\n");
        bool present=work.Contains(replace);int at=work.IndexOf(find,StringComparison.Ordinal);
        if(!present && (at<0 || work.IndexOf(find,at+find.Length,StringComparison.Ordinal)>=0))
        {Debug.LogWarning("Mochila: trecho esperado em "+source+"\n"+find);EditorUtility.DisplayDialog("Nada alterado","Versão incompatível. Envie a mensagem do Console.","OK");return;}
        if(!EditorUtility.DisplayDialog("Aplicar design da mochila?","Reorganiza a Mochila do menu, mantendo ações e dados. Usa artes genéricas substituíveis no Inspector. Não adiciona acessório equipável, atributo de discrição nem compra de moedas. Faça backup do projeto.","Aplicar","Cancelar"))return;
        string backup=Path.Combine("ZomBit_Backups","InventoryReference_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(backup);File.Copy(source,Path.Combine(backup,"MainMenuController.SharedInventory.cs"),true);
        string[] names={"frame","header","slot","gold","red","green","orange","coin","gear","bag","weapon","tool"};
        foreach(var name in names)
        {
            var importer=AssetImporter.GetAtPath(Art+name+".png") as TextureImporter;
            if(importer==null){EditorUtility.DisplayDialog("Cancelado","Imagem ausente: "+Art+name+".png. Controlador não alterado.","OK");return;}
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            bool frame=name=="frame" || name=="header" || name=="slot" || name=="gold" || name=="red" || name=="green" || name=="orange";
            importer.spriteBorder=frame?new Vector4(20,20,20,20):Vector4.zero;importer.SaveAndReimport();
        }
        var theme=AssetDatabase.LoadAssetAtPath<InventoryReferenceTheme>(ThemePath);
        bool created=theme==null;
        try
        {
            if(created){Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));AssetDatabase.Refresh();theme=ScriptableObject.CreateInstance<InventoryReferenceTheme>();AssetDatabase.CreateAsset(theme,ThemePath);}
            Undo.RecordObject(theme,"Tema da mochila");
            if(theme.panel==null)theme.panel=Load("frame");if(theme.sectionHeader==null)theme.sectionHeader=Load("header");
            if(theme.slot==null)theme.slot=Load("slot");if(theme.gold==null)theme.gold=Load("gold");
            if(theme.red==null)theme.red=Load("red");if(theme.green==null)theme.green=Load("green");if(theme.orange==null)theme.orange=Load("orange");
            if(theme.coinIcon==null)theme.coinIcon=Load("coin");if(theme.scrapIcon==null)theme.scrapIcon=Load("gear");
            if(theme.bagIcon==null)theme.bagIcon=Load("bag");if(theme.weaponIcon==null)theme.weaponIcon=Load("weapon");if(theme.workshopIcon==null)theme.workshopIcon=Load("tool");
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
            if(!present)
            {
                bool crlf=original.Contains("\r\n");string output=work.Replace(find,replace);
                var bytes=File.ReadAllBytes(source);bool bom=bytes.Length>=3 && bytes[0]==239 && bytes[1]==187 && bytes[2]==191;
                File.WriteAllText(source,crlf?output.Replace("\n","\r\n"):output,new UTF8Encoding(bom));
            }
        }
        catch(Exception e)
        {
            File.Copy(Path.Combine(backup,"MainMenuController.SharedInventory.cs"),source,true);
            if(created)AssetDatabase.DeleteAsset(ThemePath);Debug.LogError(e);EditorUtility.DisplayDialog("Falha","Controlador restaurado do backup. Confira o Console.","OK");return;
        }
        AssetDatabase.Refresh();Selection.activeObject=theme;
        EditorUtility.DisplayDialog("Tema aplicado","Aguarde recompilar, reinicie Play Mode e abra Mochila.\nEdite InventoryReferenceTheme no Inspector para trocar artes.\nBackup: "+backup,"OK");
    }
    static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
}
#endif
