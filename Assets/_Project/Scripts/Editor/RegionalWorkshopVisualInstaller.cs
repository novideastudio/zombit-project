#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class RegionalWorkshopVisualInstaller
{
    sealed class Edit{public string file,find,replace;public bool optional;public Edit(string p,string f,string r,bool o){file=p;find=f;replace=r;optional=o;}}
    [MenuItem("ZomBit/Recompensas/Integrar oficina na Mochila e Campanha")]
    static void Apply()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Oficina","Saia do Play Mode primeiro.","OK");return;}
        if(typeof(SaveData).GetField("regionRewardsJson")==null || typeof(ItemInstance).GetField("regionUniqueId")==null)
        {EditorUtility.DisplayDialog("Instale o gameplay primeiro","Este pacote exige as recompensas regionais ou o pacote integrado de progressão já instalados.","OK");return;}
        var edits=new List<Edit>();
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.SharedInventory.cs","        sharedInventoryView.Finish(invCategory,invSurvivorId,scroll);","        sharedInventoryView.Finish(invCategory,invSurvivorId,scroll);\n        BuildRegionalInventoryEntry(); // REGIONAL_VISUAL_INVENTORY_V2",false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.Map.cs","        selectedDiff = SuggestedDifficulty(loc, save);\n        RefreshDifficultyUI();","        selectedDiff = SuggestedDifficulty(loc, save);\n        RefreshDifficultyUI();\n        BuildRegionalCampaignEntry(); // REGIONAL_VISUAL_MAP_V2",false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","var rew = UIFactory.Txt(c.transform, \"RECOMPENSAS\", 38","var rew = UIFactory.Txt(c.transform, RegionalWorkshopResult.RewardsTitle(win), 38",true));
        string root=Directory.GetParent(Application.dataPath).FullName;
        var raw=new Dictionary<string,byte[]>();var work=new Dictionary<string,string>();var crlf=new Dictionary<string,bool>();var bom=new Dictionary<string,bool>();
        var log=new StringBuilder();
        foreach(var e in edits)
        {
            string path=Path.Combine(root,e.file);
            if(!File.Exists(path)){if(e.optional){log.AppendLine("Opcional ausente: "+e.file);continue;}Fail(e);return;}
            if(!work.ContainsKey(path))
            {
                var bytes=File.ReadAllBytes(path);raw[path]=bytes;string text=File.ReadAllText(path);
                crlf[path]=text.Contains("\r\n");work[path]=text.Replace("\r\n","\n");bom[path]=bytes.Length>=3 && bytes[0]==239 && bytes[1]==187 && bytes[2]==191;
            }
            if(work[path].Contains(e.replace))continue;
            int at=work[path].IndexOf(e.find,StringComparison.Ordinal);
            if(at<0 || work[path].IndexOf(e.find,at+e.find.Length,StringComparison.Ordinal)>=0)
            {if(e.optional){log.AppendLine("Opcional incompatível, preservado: "+e.file);continue;}Fail(e);return;}
            work[path]=work[path].Replace(e.find,e.replace);
        }
        if(!EditorUtility.DisplayDialog("Integrar a oficina?","Adiciona acesso no cabeçalho da Mochila e cartão de exclusivo no lugar de Itens Possíveis da campanha. Salva backup dos controladores. A substituição da UI pelo ZIP já remove o botão da Home.","Aplicar","Cancelar"))return;
        string backup=Path.Combine(root,"ZomBit_Backups","WorkshopVisual_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        try
        {
            foreach(var kv in raw)
            {
                string rel=kv.Key.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);string dest=Path.Combine(backup,rel);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));File.WriteAllBytes(dest,kv.Value);
            }
            foreach(var kv in work)File.WriteAllText(kv.Key,crlf[kv.Key]?kv.Value.Replace("\n","\r\n"):kv.Value,new UTF8Encoding(bom[kv.Key]));
            File.WriteAllText(Path.Combine(backup,"INSTALL_LOG.txt"),log.ToString());
        }
        catch(Exception ex)
        {
            foreach(var kv in raw)try{File.WriteAllBytes(kv.Key,kv.Value);}catch(Exception rollback){Debug.LogError(rollback);}
            Debug.LogError(ex);EditorUtility.DisplayDialog("Falha","Tentativa de restauração executada. Confira Console e backup.","OK");return;
        }
        AssetDatabase.Refresh();Debug.Log("Integração visual da oficina\n"+log);
        EditorUtility.DisplayDialog("Aplicado","Aguarde recompilar e reinicie Play Mode. Abra Mochila > Oficina Regional, ou Campanha > local > Ver na oficina.\nBackup: "+backup,"OK");
    }
    static void Fail(Edit e){Debug.LogWarning("Oficina visual: trecho não encontrado em "+e.file+"\n"+e.find);EditorUtility.DisplayDialog("Nenhum controlador alterado","Versão incompatível: "+e.file+". Envie a mensagem do Console.","OK");}
}
#endif
