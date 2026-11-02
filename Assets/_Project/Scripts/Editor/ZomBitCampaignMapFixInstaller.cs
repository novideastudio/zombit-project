#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ZomBitCampaignMapFixInstaller
{
    sealed class Edit
    {
        public string file,find,replace;
        public Edit(string p,string f,string r){file=p;find=f;replace=r;}
    }
    [MenuItem("ZomBit/Campanha/Corrigir mapa completo e detalhes (V2)")]
    static void Apply()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Mapa V2","Saia do Play Mode primeiro.","OK");return;}
        var edits=new List<Edit>();
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","        if(Time.unscaledTime<nextSearch)return;\n        nextSearch=Time.unscaledTime+.15f;","        // Update geometry every LateUpdate so controller callbacks cannot leave a stale layout."));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","        Hide(panel,\"TitleBar/CloseDetails\");","        var close=R(panel,\"TitleBar/CloseDetails\");\n        if(close!=null){close.gameObject.SetActive(true);Place(close,new Vector4(510,7,48,48));Art(close,theme.goldFrame,true);close.SetAsLastSibling();}"));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","Place(R(panel,\"TitleBar/DName\"),new Vector4(96,0,470,64));Label(R(panel,\"TitleBar\"),\"DName\",40);","Place(R(panel,\"TitleBar/DName\"),new Vector4(96,0,400,64));Label(R(panel,\"TitleBar\"),\"DName\",37);"));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","        if(panel.gameObject.activeSelf)Place(R(stage,\"MapArea\"),theme.mapRect);","        SyncMapGeometry(); // ZOMBIT_MAP_GEOMETRY_V2"));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","bool selected=pin!=null && pin.gameObject.activeSelf;","bool selected=panel.gameObject.activeSelf && pin!=null && pin.gameObject.activeSelf;"));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/ZomBitCampaignReferenceSkin.cs","    [ContextMenu(\"Reaplicar layout (Play Mode)\")]","    // Keep the map viewport and marker coordinates consistent in both states.\n    void SyncMapGeometry()\n    {\n        var map=R(stage,\"MapArea\");var details=R(stage,\"PcCampaignDetails\");\n        if(map==null || theme==null || details==null)return;\n        bool open=details.gameObject.activeSelf;\n        Vector4 q=theme.mapRect;\n        if(!open)\n        {\n            // Full stage width; preserve the configured header/footer baseline.\n            q.x=0;q.z=Mathf.Max(1,stage.rect.width);\n        }\n        Place(map,q);\n        var inner=R(map,\"MapInner\");\n        var markers=inner!=null?R(inner,\"MapMarkerLayer\"):null;\n        var art=inner!=null?inner.Find(\"CityMapArt\")?.GetComponent<Image>():null;\n        if(markers==null || inner==null)return;\n        // Fit image and marker layer to exactly the same rendered rectangle.\n        Vector2 area=inner.rect.size;\n        if(area.x<=0 || area.y<=0)return;\n        Vector2 rendered=area;\n        if(art!=null && art.sprite!=null)\n        {\n            art.preserveAspect=true;\n            float aspect=art.sprite.rect.width/Mathf.Max(1,art.sprite.rect.height);\n            if(area.x/area.y>aspect)rendered.x=area.y*aspect;\n            else rendered.y=area.x/aspect;\n        }\n        markers.anchorMin=markers.anchorMax=markers.pivot=new Vector2(.5f,.5f);\n        markers.anchoredPosition=Vector2.zero;markers.sizeDelta=rendered;markers.localScale=Vector3.one;\n        float baseW=Mathf.Max(1,theme.mapRect.z),baseH=Mathf.Max(1,theme.mapRect.w);\n        foreach(var entry in theme.locations ?? new ZomBitCampaignReferenceTheme.Entry[0])\n        {\n            if(entry==null || string.IsNullOrEmpty(entry.id))continue;\n            var node=R(markers,\"Node_\"+entry.id);if(node==null)continue;\n            node.anchorMin=node.anchorMax=new Vector2(entry.rect.x/baseW,1-entry.rect.y/baseH);\n            node.pivot=new Vector2(0,1);node.anchoredPosition=Vector2.zero;\n            node.sizeDelta=new Vector2(entry.rect.z,entry.rect.w);node.localScale=Vector3.one;\n        }\n    }\n\n    [ContextMenu(\"Reaplicar layout (Play Mode)\")]"));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs","        BuildPcCampaignRulesModal();\n        SetPcCampaignDetails(true);","        BuildPcCampaignRulesModal();\n        SetPcCampaignDetails(false); // ZOMBIT_MAP_START_CLOSED_V2"));
        string root=Directory.GetParent(Application.dataPath).FullName;
        var raw=new Dictionary<string,byte[]>();var work=new Dictionary<string,string>();
        var crlf=new Dictionary<string,bool>();var bom=new Dictionary<string,bool>();
        foreach(var e in edits)
        {
            string path=Path.Combine(root,e.file);
            if(!File.Exists(path)){EditorUtility.DisplayDialog("Nada alterado","Arquivo ausente: "+e.file+". Esta correção exige o pacote visual anterior instalado.","OK");return;}
            if(!raw.ContainsKey(path))
            {
                raw[path]=File.ReadAllBytes(path);
                string s=File.ReadAllText(path);crlf[path]=s.Contains("\r\n");work[path]=s.Replace("\r\n","\n");
                var bytes=raw[path];bom[path]=bytes.Length>=3 && bytes[0]==239 && bytes[1]==187 && bytes[2]==191;
            }
            if(work[path].Contains(e.replace))continue;
            int at=work[path].IndexOf(e.find,StringComparison.Ordinal);
            if(at<0 || work[path].IndexOf(e.find,at+e.find.Length,StringComparison.Ordinal)>=0)
            {
                Debug.LogWarning("Mapa V2: trecho incompatível em "+e.file+"\n"+e.find);
                EditorUtility.DisplayDialog("Nada alterado","Versão incompatível: "+e.file+". Consulte o Console. Nenhum arquivo foi alterado.","OK");return;
            }
            work[path]=work[path].Replace(e.find,e.replace);
        }
        if(!EditorUtility.DisplayDialog("Aplicar correção V2?","Corrige mapa completo, marcadores relativos, imagem sem deformação e restaura X. Salva backup dos dois scripts fora de Assets.","Aplicar","Cancelar"))return;
        string backup=Path.Combine(root,"ZomBit_Backups","CampaignMapV2_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        try
        {
            foreach(var kv in raw)
            {
                string rel=kv.Key.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                string dest=Path.Combine(backup,rel);Directory.CreateDirectory(Path.GetDirectoryName(dest));File.WriteAllBytes(dest,kv.Value);
            }
            foreach(var kv in work)
                File.WriteAllText(kv.Key,crlf[kv.Key]?kv.Value.Replace("\n","\r\n"):kv.Value,new UTF8Encoding(bom[kv.Key]));
        }
        catch(Exception e)
        {
            foreach(var kv in raw)try{File.WriteAllBytes(kv.Key,kv.Value);}catch(Exception rollback){Debug.LogError(rollback);}
            Debug.LogError(e);EditorUtility.DisplayDialog("Falha","Tentativa de restauração executada. Confira o Console.","OK");return;
        }
        AssetDatabase.Refresh();EditorUtility.DisplayDialog("Correção aplicada","Aguarde recompilar e reabra a campanha. Deve iniciar sem detalhes. Clique num local para abrir e no X para fechar.\nBackup: "+backup,"OK");
    }
}
#endif
