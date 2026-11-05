using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Saves ONLINE. Nada de progresso e gravado em disco: os slots vivem na memoria, carregados da conta no login
/// e enviados ao servidor (ZombitCloud.QueueSave). A API publica continua sincrona para o resto do jogo.
/// Sem conta (cena de teste aberta direto) o save existe so enquanto o jogo estiver aberto.
/// </summary>
public static class SaveSystem
{
    public const int SlotCount = 3;
    static readonly Dictionary<int, string> store = new Dictionary<int, string>();
    static readonly Dictionary<int, long> revisions = new Dictionary<int, long>();
    static bool warnedOffline;

    public static bool Exists(int slot) => store.ContainsKey(slot);

    public static void Save(SaveData d)
    {
        d.ticks = DateTime.UtcNow.Ticks;
        d.lastSaved = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        string json = JsonUtility.ToJson(d, false);
        store[d.slot] = json;
        if (!ZombitCloud.IsReady && !warnedOffline)
        {
            warnedOffline = true;
            Debug.LogWarning("Zombit: sem conta online ativa; o save fica apenas na memória desta sessão.");
        }
        ZombitCloud.QueueSave(d.slot, json, d.ticks);
    }

    // ---- usado pela sincronizacao com a nuvem (ZombitCloud) ----
    public static string ReadRawJson(int slot) { string j; return store.TryGetValue(slot, out j) ? j : null; }
    public static long RevisionOf(int slot) { long r; return revisions.TryGetValue(slot, out r) ? r : 0L; }
    public static void SetRevision(int slot, long rev) { if (rev <= 0) revisions.Remove(slot); else revisions[slot] = rev; }

    [Serializable] class TicksProbe { public long ticks; }
    public static long TicksOf(string json)
    {
        try { return JsonUtility.FromJson<TicksProbe>(json).ticks; }
        catch (Exception) { return 0L; }
    }

    public static void ImportFromCloud(int slot, string json, long revision)
    {
        store[slot] = json;
        SetRevision(slot, revision);
    }
    public static void ImportFromCloud(int slot, string json) => ImportFromCloud(slot, json, RevisionOf(slot));

    public static void DeleteLocalOnly(int slot) { store.Remove(slot); revisions.Remove(slot); }
    public static void ClearAll() { store.Clear(); revisions.Clear(); }
    public static void AdoptLegacyFiles() { /* removido: nao ha mais saves locais */ }

    /// <summary>Apaga os JSON antigos (zombit_*slot*.json/.bak/.tmp) uma unica vez. Voce autorizou descartar os saves de teste.</summary>
    public static void PurgeLegacyLocalFiles()
    {
        if (PlayerPrefs.GetInt("zombit_legacy_purged", 0) == 1) return;
        try
        {
            string dir = Application.persistentDataPath;
            int n = 0;
            foreach (var f in Directory.GetFiles(dir, "zombit_*slot*.json*"))
            {
                try { File.Delete(f); n++; } catch (Exception) { }
            }
            Debug.Log("Zombit: " + n + " arquivo(s) de save antigo removido(s).");
        }
        catch (Exception e) { Debug.LogWarning("Zombit: limpeza dos saves antigos falhou: " + e.Message); }
        PlayerPrefs.SetInt("zombit_legacy_purged", 1);
        PlayerPrefs.Save();
    }

    static SaveData TryRead(string json, int slot)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var d = JsonUtility.FromJson<SaveData>(json);
        if (d == null) return null;
        d.slot = slot;
        d.MigrateFromPreviousVersions();
        if (DeathPenalty.ResolveInterruptedRun(d))
        {
            try { Save(d); }
            catch (Exception e) { Debug.LogWarning("Zombit: falha ao gravar a penalidade de missao interrompida: " + e.Message); }
        }
        return d;
    }

    public static SaveData Load(int slot)
    {
        try { return TryRead(ReadRawJson(slot), slot); }
        catch (Exception e)
        {
            Debug.LogWarning("Falha ao carregar slot " + slot + ": " + e.Message);
            return null;
        }
    }

    public static void Delete(int slot)
    {
        DeleteLocalOnly(slot);
        ZombitCloud.QueueDelete(slot);
    }

    public static int FirstEmptySlot()
    {
        for (int i = 1; i <= SlotCount; i++) if (!Exists(i)) return i;
        return -1;
    }

    public static SaveData LoadMostRecent()
    {
        SaveData best = null;
        for (int i = 1; i <= SlotCount; i++)
        {
            var d = Load(i);
            if (d != null && (best == null || d.ticks > best.ticks)) best = d;
        }
        return best;
    }
}
