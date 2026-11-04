#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class CommerceNavigationAudit
{
    [MenuItem("ZomBit/Loja/Auditar botoes Voltar na cena")]
    static void Audit()
    {
        var report=new StringBuilder("BOTÕES DE VOLTAR — estado atual da cena, sem remoção automática\n");
        int count=0;
        foreach(var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var t=b.GetComponentInChildren<TMP_Text>(true);string text=t!=null?t.text.Trim().ToUpperInvariant():"";
            if(!text.Contains("VOLTAR") && text!="BACK" && !b.name.Contains("Back"))continue;
            string path=b.name;for(var p=b.transform.parent;p!=null;p=p.parent)path=p.name+"/"+path;
            var rt=b.transform as RectTransform;report.AppendLine(path+" | "+text+" | size="+(rt!=null?rt.rect.size.ToString():"n/a")+" | active="+b.gameObject.activeInHierarchy);count++;
        }
        Debug.Log(report.ToString()+"Total: "+count);
        EditorUtility.DisplayDialog("Auditoria de navegação",count+" candidatos listados no Console. O relatório inclui telas inativas e modais; não significa que sejam duplicatas visíveis.","OK");
    }
}
#endif
