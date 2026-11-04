#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DailyCommerceInstaller
{
    sealed class Edit{public string file,find,replace,mode;public bool optional;public Edit(string p,string f,string r,string m,bool o){file=p;find=f;replace=r;mode=m;optional=o;}}
    static int EndBrace(string s,int start)
    {
        int depth=0;
        for(int i=start;i<s.Length;i++)
        {
            char c=s[i];
            if(c=='/' && i+1<s.Length && s[i+1]=='/') {while(i<s.Length && s[i]!='\n')i++;continue;}
            if(c=='/' && i+1<s.Length && s[i+1]=='*') {i+=2;while(i+1<s.Length && !(s[i]=='*' && s[i+1]=='/'))i++;i++;continue;}
            if(c=='"' || c=='\''){char q=c;i++;while(i<s.Length && s[i]!=q){if(s[i]=='\\')i++;i++;}continue;}
            if(c=='{')depth++;if(c=='}'){depth--;if(depth==0)return i;}
        }
        return -1;
    }
    [MenuItem("ZomBit/Loja/Instalar loja e presentes")]
    static void Install()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Loja","Saia do Play Mode antes de instalar.","OK");return;}
        var edits=new List<Edit>();
        edits.Add(new Edit("Assets/_Project/Scripts/Data/SaveData.cs","public class SaveData\n{","public class SaveData\n{\n    public string commerceJson = \"\"; // DAILY_COMMERCE_STATE_V1","text",false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/ItemInstance.cs","public class ItemInstance\n{","public class ItemInstance\n{\n    public int requiredLevel = 0; // DAILY_COMMERCE_LEVEL_V1","text",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcReferenceHome.cs","RefPcButton(stage,\"gifts\",15,259,189,75,()=>Toast(\"Presentes: em breve.\"));","RefPcButton(stage,\"gifts\",15,259,189,75,()=>ClaimWeeklyGiftFromHome(stage));\n        BindWeeklyGiftButton(stage); // WEEKLY_GIFTS_BUTTON_V1","text",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcReferenceHome.cs","        RefPcApplyStrongSelection();","        RefPcApplyStrongSelection();\n        RefreshWeeklyGiftVisibility(); // WEEKLY_GIFTS_REFRESH_V1","text",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.Inventory.cs","Button CreateItemTile(","ItemTileStyling.Apply(b.transform, item); // DAILY_COMMERCE_RARITY_V1","beforeReturn",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.Inventory.cs","void EquipMapItem(","DAILY_COMMERCE_MENU_LEVEL_V1","equipGuard",false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/Inventory.cs","public void Equip(ItemInstance item, PlayerStats target)","\n        if (!TimedCommerce.CanEquip(item, target != null ? target.Level : 0)) return; // DAILY_COMMERCE_RUN_LEVEL_V1","entry",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/InventoryReferenceLayout.cs","words.StartsWith(\"MELHORAR\")","(words.StartsWith(\"MELHORAR\") || words.StartsWith(\"REFINAR\"))","text",true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/InventoryReferenceLayout.cs","return s.StartsWith(\"MELHORAR\")?1:","return (s.StartsWith(\"MELHORAR\") || s.StartsWith(\"REFINAR\"))?1:","text",true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/InventoryReferenceLayout.cs","        var iconBox=Box(body,\"ItemFrame\",20,76,142,142,theme.gold);","        var iconBox=Box(body,\"ItemFrame\",20,76,142,142,theme.gold);\n        ItemTileStyling.Apply(iconBox, item); // DAILY_COMMERCE_DETAILS_RARITY_V1","text",true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.SharedInventory.cs","        Show(\"mapInventory\");sharedInventoryView.Fit.Apply();","        Show(\"mapInventory\");sharedInventoryView.Fit.Apply();\n        CommerceUiPolish.ShortenRefineButtons(screen); // DAILY_COMMERCE_REFINE_CAPTION_V1","text",false));
        string root=Directory.GetParent(Application.dataPath).FullName;
        var raw=new Dictionary<string,byte[]>();var work=new Dictionary<string,string>();var crlf=new Dictionary<string,bool>();var bom=new Dictionary<string,bool>();var log=new StringBuilder();
        foreach(var e in edits)
        {
            string path=Path.Combine(root,e.file);
            if(!File.Exists(path)){if(e.optional){log.AppendLine("Opcional ausente: "+e.file);continue;}Fail(e);return;}
            if(!raw.ContainsKey(path)){var b=File.ReadAllBytes(path);raw[path]=b;string text=File.ReadAllText(path);crlf[path]=text.Contains("\r\n");work[path]=text.Replace("\r\n","\n");bom[path]=b.Length>=3 && b[0]==239 && b[1]==187 && b[2]==191;}
            string s=work[path];
            if(e.mode=="text")
            {
                if(s.Contains(e.replace))continue;int at=s.IndexOf(e.find,StringComparison.Ordinal);
                if(at<0 || s.IndexOf(e.find,at+e.find.Length,StringComparison.Ordinal)>=0){if(e.optional){log.AppendLine("Opcional diferente, preservado: "+e.file+" / "+e.find);continue;}Fail(e);return;}
                work[path]=s.Replace(e.find,e.replace);continue;
            }
            int sig=s.IndexOf(e.find,StringComparison.Ordinal);
            if(sig<0 || s.IndexOf(e.find,sig+e.find.Length,StringComparison.Ordinal)>=0){Fail(e);return;}
            int open=s.IndexOf('{',sig+e.find.Length),end=open>=0?EndBrace(s,open):-1;
            if(open<0 || end<0){Fail(e);return;}
            string body=s.Substring(open+1,end-open-1);
            if(body.Contains(e.replace))continue;
            if(e.mode=="beforeReturn")
            {
                int ret=body.LastIndexOf("return b;",StringComparison.Ordinal);if(ret<0){Fail(e);return;}
                body=body.Insert(ret,e.replace+"\n        ");
            }
            if(e.mode=="entry")body=e.replace+body;
            if(e.mode=="equipGuard")
            {
                if(body.Contains("DAILY_COMMERCE_MENU_LEVEL_V1"))continue;
                var match=Regex.Match(s.Substring(sig,open-sig),@"EquipMapItem\s*\(\s*string\s+(\w+)");
                if(!match.Success){Fail(e);return;}string uid=match.Groups[1].Value;
                body="\n        if (!TimedCommerce.CanEquip(GameSession.Save, invSurvivorId, "+uid+")) { Toast(\"Nível insuficiente para equipar este item.\"); return; } // DAILY_COMMERCE_MENU_LEVEL_V1\n"+body;
            }
            work[path]=s.Substring(0,open+1)+body+s.Substring(end);
        }
        if(!EditorUtility.DisplayDialog("Instalar correções da loja?","Adiciona estoque por dia/personagem, presentes semanais por save e nível mínimo dos itens gerados.\nUse uma cópia do projeto e dos saves. O ZIP já substituiu a tela da loja; esse backup cobre os controladores alterados pelo comando.","Instalar","Cancelar"))return;
        string backup=Path.Combine(root,"ZomBit_Backups","DailyCommerce_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));const string assetPath="Assets/_Project/Resources/Config/DailyCommerceConfig.asset";bool created=false;
        try
        {
            foreach(var kv in raw){string rel=kv.Key.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);string dest=Path.Combine(backup,rel);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.WriteAllBytes(dest,kv.Value);}
            if(AssetDatabase.LoadAssetAtPath<DailyCommerceConfig>(assetPath)==null){Directory.CreateDirectory(Path.GetDirectoryName(assetPath));AssetDatabase.Refresh();AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DailyCommerceConfig>(),assetPath);created=true;}
            foreach(var kv in work)File.WriteAllText(kv.Key,crlf[kv.Key]?kv.Value.Replace("\n","\r\n"):kv.Value,new UTF8Encoding(bom[kv.Key]));
            AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(backup,"INSTALL_LOG.txt"),log.ToString());
        }
        catch(Exception ex)
        {
            foreach(var kv in raw)try{File.WriteAllBytes(kv.Key,kv.Value);}catch(Exception rollback){Debug.LogError(rollback);}
            if(created)AssetDatabase.DeleteAsset(assetPath);Debug.LogError(ex);EditorUtility.DisplayDialog("Falha","Rollback tentado; confira Console e backup.","OK");return;
        }
        AssetDatabase.Refresh();Debug.Log("Loja/presentes instalados.\n"+log);EditorUtility.DisplayDialog("Instalado","Aguarde recompilar e reinicie Play Mode.\nAbra a loja e confira personagem/nível e data UTC.\nBackup: "+backup,"OK");
    }
    static void Fail(Edit e){Debug.LogWarning("Loja: incompatível em "+e.file+"\n"+e.find);EditorUtility.DisplayDialog("Nenhum controlador alterado","Versão incompatível: "+e.file+". Envie a primeira mensagem do Console.","OK");}
}
#endif
