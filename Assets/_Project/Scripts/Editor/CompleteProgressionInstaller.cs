#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CompleteProgressionInstaller
{
    sealed class Edit
    {
        public string file,find,replace;public bool method,optional;
        public Edit(string p,string f,string r,bool m,bool o){file=p;find=f;replace=r;method=m;optional=o;}
    }
    // Finds method end ignoring strings/chars/comments, including braces inside interpolated text.
    static int EndBrace(string s,int start)
    {
        int depth=0;
        for(int i=start;i<s.Length;i++)
        {
            char c=s[i];
            if(c=='/' && i+1<s.Length && s[i+1]=='/') {while(i<s.Length && s[i]!='\n')i++;continue;}
            if(c=='/' && i+1<s.Length && s[i+1]=='*') {i+=2;while(i+1<s.Length && !(s[i]=='*' && s[i+1]=='/'))i++;i++;continue;}
            if(c=='"' || c=='\'')
            {
                char q=c;i++;while(i<s.Length && s[i]!=q){if(s[i]=='\\')i++;i++;}continue;
            }
            if(c=='{')depth++;
            if(c=='}'){depth--;if(depth==0)return i;}
        }
        return -1;
    }

    [MenuItem("ZomBit/Progressao/Instalar pacote completo")]
    public static void Install()
    {
        if(Application.isPlaying){EditorUtility.DisplayDialog("Progressão","Saia do Play Mode antes de instalar.","OK");return;}
        var edits=new List<Edit>();
        edits.Add(new Edit("Assets/_Project/Scripts/Data/SaveData.cs","public class SaveData\n{","public class SaveData\n{\n    public string regionRewardsJson = \"\"; // ZOMBIT_REGIONAL_STATE_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/ItemInstance.cs","public class ItemInstance\n{","public class ItemInstance\n{\n    public string regionUniqueId = \"\"; // ZOMBIT_REGIONAL_ITEM_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/ItemInstance.cs","        if (item == null) return UIFactory.White;","        if (item == null) return UIFactory.White;\n        var regionalArt = RegionalRewards.Get(RegionalRewards.UniqueId(item));\n        if (regionalArt != null && regionalArt.icon != null) return regionalArt.icon; // ZOMBIT_REGIONAL_ICON_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/Health.cs","        float appliedDamage = Mathf.Min(Current, Mathf.Max(1f, reduced));","        float appliedDamage = Mathf.Min(Current, Mathf.Max(1f, reduced));\n        appliedDamage = RegionalEffects.FilterDamage(this, appliedDamage); // ZOMBIT_REGIONAL_DAMAGE_V1\n        if (appliedDamage <= 0f) return;",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/Enemy.cs","                    targetHealth.Take(data.damage * DamageMult * damageBoost * FuryDamage);","                    RegionalEffects.TakeDirect(targetHealth, data.damage * DamageMult * damageBoost * FuryDamage); // ZOMBIT_REGIONAL_DIRECT_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/Hero.cs","weapon.shotsPerSecond * fireRateMult * (1f + affixFireRate)","weapon.shotsPerSecond * fireRateMult * (1f + affixFireRate) * RegionalEffects.FireRate(this)",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/Hero.cs","        p.Launch(dest, dmg, critical, weapon.pierceTargets, weapon.pierceFalloff, t, missed, EffectiveRange);","        dmg *= RegionalEffects.ShotMultiplier(this, t); // ZOMBIT_REGIONAL_SHOT_V1\n        p.Launch(dest, dmg, critical, weapon.pierceTargets, weapon.pierceFalloff, t, missed, EffectiveRange);",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/SquadFormationSwap.cs","        if (hud != null) hud.ApplySeatOrder(order);","        if (hud != null) hud.ApplySeatOrder(order);\n        RegionalEffects.OnSwap(first.stats, second.stats); // ZOMBIT_REGIONAL_SWAP_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","        if (win) scrapGain = Mathf.RoundToInt(scrapGain * ScrapRules.WinBonus);","        if (win) scrapGain = Mathf.RoundToInt(scrapGain * ScrapRules.WinBonus);\n        if (win) scrapGain += RegionalRewards.ExtraScrap(scrapGain); // ZOMBIT_REGIONAL_SCRAP_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","        save.activeRun = new ActiveRun();   // missao terminada: nao conta mais como interrompida","        RegionalRewards.GrantVictory(save, loc, level, win, inv, stats, loot, drops); // ZOMBIT_REGIONAL_VICTORY_V1\n        save.activeRun = new ActiveRun();   // missao terminada: nao conta mais como interrompida",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/GameDatabase.cs","public static class GameDatabase\n{","public static class GameDatabase\n{\n    static GameDatabase(){ ProgressionRuntime.PrepareRegions(); } // ZOMBIT_PROGRESSION_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/GameDatabase.cs","public static bool IsUnlocked(LocationDef loc, SaveData save)","return loc != null;",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/GameDatabase.cs","public static bool IsDifficultyUnlocked(LocationDef loc, SaveData save, int difficulty)","return loc != null && difficulty >= 1 && difficulty <= 4;",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/Difficulty.cs","public static DifficultyDef Get(int value) => All[Mathf.Clamp(value, 1, 4) - 1];","public static DifficultyDef Get(int value) => ProgressionRuntime.BattleDifficulty(value);",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/Progression.cs","public static int Of(SaveData save, string survivorId)","return ProgressionRuntime.IndividualPower(save, string.IsNullOrEmpty(survivorId) && save != null ? save.selectedSurvivor : survivorId);",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/Progression.cs","public static int Recommended(LocationDef loc, int difficulty)","return ProgressionRuntime.Recommended(loc, difficulty);",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/Progression.cs","public static string Describe(SaveData save, string survivorId, LocationDef loc, int difficulty)","return \"\\n\" + ProgressionRuntime.Describe(save, loc, difficulty);",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Config/Balance.cs","public static Zone ZoneOf(string locationId)","return ProgressionRuntime.Zone(locationId);",true,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionSetup.cs","        Enemy.DamageDealt = 0f;","        Enemy.DamageDealt = 0f;\n        ProgressionRuntime.ConfigureBattle(loc, diff, squadSize); // ZOMBIT_PROGRESSION_COMBAT_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionSetup.cs","        if (save != null) save.MigrateFromPreviousVersions();","        if (save != null) save.MigrateFromPreviousVersions();\n        ProgressionRuntime.BeginRun(save, loc); // ZOMBIT_PROGRESSION_BEGIN_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionSetup.cs","hs.phaseSeconds = tuning.hordePhaseSeconds;","hs.phaseSeconds = ProgressionRuntime.Config.phaseSeconds;",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionSetup.cs","hs.ramp = tuning.hordeRampPerSecond;","hs.ramp = ProgressionRuntime.Config.hordeRamp;",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.Map.cs","    void OnStartMission()\n    {\n        if (selectedLoc == null) return;","    void OnStartMission()\n    {\n        if (selectedLoc == null) return;\n        if (!ConfirmProgressionRisk()) return; // ZOMBIT_PROGRESSION_GATE_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.Map.cs","        RefreshSquadSlots();   // 3o slot trava/destrava conforme o nivel (trio so no Pesadelo)","        RefreshSquadSlots();   // 3o slot trava/destrava conforme o nivel (trio so no Pesadelo)\n        RefreshProgressionPowerLine(); // ZOMBIT_PROGRESSION_PANEL_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","        int scrapGain = loot != null ? loot.scrap : 0;","        int scrapGain = loot != null ? (win ? loot.scrap : ProgressionRuntime.RetainScrap(loot.scrap)) : 0; // ZOMBIT_PROGRESSION_RETENTION_V1",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","save.scrap += Mathf.Max(0, loot.scrap / 2);","save.scrap += ProgressionRuntime.RetainScrap(loot.scrap);",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","        Time.timeScale = 0f;\n        BuildPanel(win,","        ProgressionRuntime.RecordResult(win, loc, elapsed, goldGain, scrapGain); // ZOMBIT_PROGRESSION_LOG_V1\n        Time.timeScale = 0f;\n        BuildPanel(win,",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Data/Difficulty.cs","(a sucata fica)","(ret\u00e9m {Mathf.RoundToInt(ProgressionRuntime.Config.failedScrapRetention * 100f)}% da sucata)",false,false));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","A sucata foi mantida: use na oficina (invent\u00e1rio) para melhorar seus itens.","Parte da sucata foi mantida: use na oficina (invent\u00e1rio) para melhorar seus itens.",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","Novas \u00e1reas podem ter sido desbloqueadas!","Todos os locais est\u00e3o abertos. Confira o poder recomendado.",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/Systems/MissionFlow.cs","N\u00edvel {next.name} liberado neste local.","N\u00edvel {next.name} dispon\u00edvel para um desafio maior.",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/Enemy.cs","if (HeroSquad.Count > 0 && (target == null || TargetDown()))","if (HeroSquad.Count > 0) // reavaliar frente a cada frame",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/SurvivorAbilities.cs","if (def.utility == UtilityType.Grenade)","if (def.utility == UtilityType.Grenade && def.id == \"assaulter\")",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/Combat/SurvivorAbilities.cs","w.range = 7f;","w.range = 18.5f;",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.MissionFormation.cs","if(MissionFormationLimit==3 && save.HasThird","if(save.HasThird",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.MissionFormation.cs","missionFormationIds.Count<MissionFormationLimit","missionFormationIds.Count<3",false,true));
        edits.Add(new Edit("Assets/_Project/Scripts/UI/MainMenuController.MissionFormation.cs","missionFormationIds.Count>MissionFormationLimit","missionFormationIds.Count>3",false,true));
        string root=Directory.GetParent(Application.dataPath).FullName;
        var raw=new Dictionary<string,byte[]>();var work=new Dictionary<string,string>();
        var crlf=new Dictionary<string,bool>();var bom=new Dictionary<string,bool>();var log=new StringBuilder();
        foreach(var e in edits)
        {
            string path=Path.Combine(root,e.file);
            if(!File.Exists(path))
            {
                if(e.optional){log.AppendLine("Opcional ausente: "+e.file);continue;}
                EditorUtility.DisplayDialog("Nada alterado","Arquivo obrigatório ausente: "+e.file,"OK");return;
            }
            if(!raw.ContainsKey(path))
            {
                var bytes=File.ReadAllBytes(path);raw[path]=bytes;string s=File.ReadAllText(path);
                crlf[path]=s.Contains("\r\n");work[path]=s.Replace("\r\n","\n");
                bom[path]=bytes.Length>=3 && bytes[0]==239 && bytes[1]==187 && bytes[2]==191;
            }
            string text=work[path];
            if(e.method)
            {
                int at=text.IndexOf(e.find,StringComparison.Ordinal);
                if(at<0 || text.IndexOf(e.find,at+e.find.Length,StringComparison.Ordinal)>=0){Fail(e,log);return;}
                int open=text.IndexOf('{',at+e.find.Length);int end=open>=0?EndBrace(text,open):-1;
                if(open<0 || end<0){Fail(e,log);return;}
                string body="{\n        "+e.replace+"\n    }";
                work[path]=text.Substring(0,open)+body+text.Substring(end+1);continue;
            }
            if(text.Contains(e.replace))continue;
            int hit=text.IndexOf(e.find,StringComparison.Ordinal);
            if(hit<0 || text.IndexOf(e.find,hit+e.find.Length,StringComparison.Ordinal)>=0)
            {
                if(e.optional){log.AppendLine("Opcional já diferente, preservado: "+e.file+" / "+e.find);continue;}
                Fail(e,log);return;
            }
            work[path]=text.Replace(e.find,e.replace);
        }
        if(!EditorUtility.DisplayDialog("Instalar progressão e exclusivos?","Abre todos os locais/dificuldades; troca a escala real de inimigos e economia; adiciona save de selos/itens e efeitos.\nDERROTA/ABANDONO: retém inicialmente 25% da sucata (vitória mantém tudo).\nFaça backup de projeto E saves. Ainda não testado no Unity.","Instalar","Cancelar"))return;
        string backup=Path.Combine(root,"ZomBit_Backups","CompleteProgression_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var createdAssets=new List<string>();
        try
        {
            foreach(var kv in raw)
            {
                string rel=kv.Key.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                string dst=Path.Combine(backup,rel);Directory.CreateDirectory(Path.GetDirectoryName(dst));File.WriteAllBytes(dst,kv.Value);
            }
            CreateConfig<ProgressionConfig>("Assets/_Project/Resources/Config/ProgressionConfig.asset",createdAssets);
            CreateConfig<RegionalRewardConfig>("Assets/_Project/Resources/Config/RegionalRewardConfig.asset",createdAssets);
            foreach(var kv in work)File.WriteAllText(kv.Key,crlf[kv.Key]?kv.Value.Replace("\n","\r\n"):kv.Value,new UTF8Encoding(bom[kv.Key]));
            File.WriteAllText(Path.Combine(backup,"INSTALL_LOG.txt"),log.ToString());AssetDatabase.SaveAssets();
        }
        catch(Exception ex)
        {
            foreach(var kv in raw)try{File.WriteAllBytes(kv.Key,kv.Value);}catch(Exception rollback){Debug.LogError(rollback);}
            foreach(var p in createdAssets)AssetDatabase.DeleteAsset(p);
            Debug.LogError(ex);EditorUtility.DisplayDialog("Falha","Rollback tentado. Confira Console e backup.","OK");return;
        }
        AssetDatabase.Refresh();Debug.Log("Progressão instalada.\n"+log);
        EditorUtility.DisplayDialog("Instalado","Aguarde recompilar. Reinicie Play Mode e abra a campanha. Teste primeiro em cópia do save.\nBackup: "+backup,"OK");
    }
    static void Fail(Edit e,StringBuilder log)
    {Debug.LogWarning("Incompatível: "+e.file+"\n"+e.find+"\n"+log);EditorUtility.DisplayDialog("Nada alterado","Trecho obrigatório incompatível: "+e.file+". Envie a mensagem do Console.","OK");}
    static void CreateConfig<T>(string path,List<string> created) where T:ScriptableObject
    {
        if(AssetDatabase.LoadAssetAtPath<T>(path)!=null)return;
        Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(),path);created.Add(path);
    }
}
#endif
