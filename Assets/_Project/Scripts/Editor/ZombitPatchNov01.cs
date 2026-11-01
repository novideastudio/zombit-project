#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Aplica as correcoes de 01/11 (ordem da campanha, botoes Regras/Formacao, alvo dos zumbis, granada).
// Faz backup em <projeto>/ZomBit_Backups, e segura: se algum trecho nao for encontrado, o arquivo nao e alterado.
public static class ZombitPatchNov01
{
    sealed class Edit
    {
        public readonly string File, Find, Replace;
        public Edit(string file, string find, string replace)
        {
            File = file;
            Find = find.Replace("\r\n", "\n");
            Replace = replace.Replace("\r\n", "\n");
        }
    }

    static List<Edit> BuildEdits()
    {
        var e = new List<Edit>();
        e.Add(new Edit("Assets/_Project/Scripts/Data/GameDatabase.cs", "        new LocationDef { id = \"residencial\", name = \"Bairro Residencial\", theme = \"escola\", recommendedLevel = 1, difficulty = 1, waves = 3,\n            countMult = 0.8f, goldReward = 60, mapX = 0.446364f, mapY = 0.721948f,\n            description = \"Ruas silenciosas tomadas por poucos infectados. Um bom lugar para come\u00e7ar.\" },\n        new LocationDef { id = \"suburbio\", name = \"Sub\u00farbio\", theme = \"escola\", hasBoss = true, recommendedLevel = 3, difficulty = 1, waves = 3,\n            countMult = 1.0f, goldReward = 120, mapX = 0.156524f, mapY = 0.906905f, requires = new[] { \"residencial\" },\n            description = \"Casas e fazendas abandonadas. Um mutante enorme guarda a escola local.\" },", "        new LocationDef { id = \"suburbio\", name = \"Sub\u00farbio\", theme = \"escola\", hasBoss = true, recommendedLevel = 3, difficulty = 1, waves = 3,\n            countMult = 1.0f, goldReward = 120, mapX = 0.156524f, mapY = 0.906905f,\n            description = \"Casas e fazendas abandonadas. Um mutante enorme guarda a escola local.\" },\n        new LocationDef { id = \"residencial\", name = \"Bairro Residencial\", theme = \"escola\", recommendedLevel = 1, difficulty = 1, waves = 3,\n            countMult = 0.8f, goldReward = 60, mapX = 0.446364f, mapY = 0.721948f, requires = new[] { \"suburbio\" },\n            description = \"Ruas silenciosas tomadas por poucos infectados.\" },"));
        e.Add(new Edit("Assets/_Project/Scripts/Data/GameDatabase.cs", "mapX = 1.356885f, mapY = 0.027127f, requires = new[] { \"suburbio\" },", "mapX = 1.356885f, mapY = 0.027127f, requires = new[] { \"residencial\" },"));
        e.Add(new Edit("Assets/_Project/Scripts/Combat/Enemy.cs", "// Modo dupla: se o alvo caiu, passa a perseguir o sobrevivente vivo mais a frente.", "// Dupla/trio: o alvo e sempre o sobrevivente vivo que esta mais a frente (reavaliado a cada frame, inclusive apos trocar a formacao)."));
        e.Add(new Edit("Assets/_Project/Scripts/Combat/Enemy.cs", "if (HeroSquad.Count > 0 && (target == null || TargetDown()))", "if (HeroSquad.Count > 0) // reavalia todo frame: quem esta na frente toma o dano"));
        e.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs", "\"MissionRules\",\"REGRAS DA MISS\u00c3O\",\"button\",20,780,550,44,()=>{pcCampaignRules.SetActive(true);},18);", "\"MissionRules\",\"REGRAS DA MISS\u00c3O\",\"gold\",20,772,269,62,()=>{pcCampaignRules.SetActive(true);},20);"));
        e.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs", "PcCampIcon(rules.transform,\"info\",14,10,24);", "PcCampIcon(rules.transform,\"info\",12,16,30);PcCampShiftLabel(rules,44f);"));
        e.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs", "\"ChooseFormation\",\"\",\"button\",520,784,38,36,OpenMissionFormation,12);", "\"ChooseFormation\",\"FORMA\u00c7\u00c3O\",\"gold\",301,772,269,62,OpenMissionFormation,20);"));
        e.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs", "PcCampIcon(pcCampaignFormation.transform,\"formation\",7,6,24);", "PcCampIcon(pcCampaignFormation.transform,\"formation\",12,16,30);PcCampShiftLabel(pcCampaignFormation,44f);"));
        e.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.PcCampaign.cs", "\"StartMission\",\"INICIAR MISS\u00c3O\",\"red\",20,840,550,88,OnStartMission,30);", "\"StartMission\",\"INICIAR MISS\u00c3O\",\"red\",20,846,550,88,OnStartMission,30);"));
        e.Add(new Edit("Assets/_Project/Scripts/Combat/SurvivorAbilities.cs", "if (def.utility == UtilityType.Grenade)", "if (def.utility == UtilityType.Grenade && def.id == \"assaulter\")"));
        return e;
    }

    static int Count(string s, string sub)
    {
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    static string Short(string s)
    {
        s = s.Replace("\n", " ").Trim();
        return s.Length > 80 ? s.Substring(0, 80) + "..." : s;
    }

    [MenuItem("ZomBit/Correcoes/Aplicar correcoes de 01-11")]
    public static void Apply()
    {
        string root = Directory.GetParent(Application.dataPath).FullName;
        string backupDir = Path.Combine(root, "ZomBit_Backups", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        var byFile = new Dictionary<string, List<Edit>>();
        foreach (var edit in BuildEdits())
        {
            if (!byFile.ContainsKey(edit.File)) byFile[edit.File] = new List<Edit>();
            byFile[edit.File].Add(edit);
        }

        var report = new StringBuilder();
        int changedFiles = 0, failedFiles = 0;

        foreach (var kv in byFile)
        {
            string full = Path.Combine(root, kv.Key);
            report.AppendLine(kv.Key);
            if (!File.Exists(full)) { report.AppendLine("  ARQUIVO NAO ENCONTRADO"); failedFiles++; continue; }

            byte[] raw = File.ReadAllBytes(full);
            bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
            int skip = bom ? 3 : 0;
            string text = new UTF8Encoding(false).GetString(raw, skip, raw.Length - skip);
            bool crlf = text.Contains("\r\n");
            string work = text.Replace("\r\n", "\n");

            bool ok = true, changed = false;
            var lines = new StringBuilder();
            foreach (var edit in kv.Value)
            {
                if (work.Contains(edit.Replace)) { lines.AppendLine("  ja aplicado: " + Short(edit.Find)); continue; }
                int count = Count(work, edit.Find);
                if (count != 1)
                {
                    lines.AppendLine("  FALHOU (" + count + " ocorrencias): " + Short(edit.Find));
                    ok = false;
                    break;
                }
                work = work.Replace(edit.Find, edit.Replace);
                changed = true;
                lines.AppendLine("  ok: " + Short(edit.Find));
            }
            report.Append(lines.ToString());

            if (!ok) { report.AppendLine("  -> arquivo NAO alterado"); failedFiles++; continue; }
            if (!changed) continue;

            string backupPath = Path.Combine(backupDir, kv.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
            File.Copy(full, backupPath, true);

            string output = crlf ? work.Replace("\n", "\r\n") : work;
            File.WriteAllText(full, output, new UTF8Encoding(bom));
            changedFiles++;
        }

        AssetDatabase.Refresh();
        string summary = "Arquivos alterados: " + changedFiles + " | com falha: " + failedFiles +
                         (changedFiles > 0 ? "\nBackups em: " + backupDir : "");
        Debug.Log("[ZomBit Patch 01-11]\n" + summary + "\n\n" + report);
        EditorUtility.DisplayDialog("ZomBit - correcoes", summary + "\n\nDetalhes no Console.", "OK");
    }
}
#endif
