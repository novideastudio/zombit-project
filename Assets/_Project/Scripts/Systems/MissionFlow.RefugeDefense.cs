using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Dedicated result and reward settlement for Refuge Defense.</summary>
public partial class MissionFlow
{
    void FinishRefugeDefense(bool win, bool abandoned)
    {
        if (done) return;
        done = true;

        var controller = FindFirstObjectByType<RefugeDefenseController>();
        int waves = controller != null ? controller.WavesCompleted : 0;
        int kills = controller != null ? controller.Kills : 0;
        int bosses = controller != null ? controller.BossesDefeated : 0;
        int seconds = controller != null ? Mathf.Max(0, Mathf.FloorToInt(controller.ElapsedSeconds)) : Mathf.Max(0, Mathf.FloorToInt(elapsed));

        // Completed waves are banked in full; kills add a smaller bonus. Leaving early
        // does not erase already secured waves and never invokes campaign death penalties.
        int materials = waves * 12 + Mathf.FloorToInt(kills / (abandoned ? 18f : 10f)) + bosses * 3;
        if (win) materials += 25;
        materials = Mathf.Max(0, materials);

        var save = GameSession.Save;
        bool newRecord = false;
        bool saved = false;
        if (save != null)
        {
            save.defenseMaterials = Mathf.Max(0, save.defenseMaterials + materials);
            int difficulty = Mathf.Clamp(GameSession.Difficulty, 1, 4);
            int oldWave = save.bestRefugeDefenseWave;
            int oldDifficulty = save.bestRefugeDefenseDifficulty;
            // Do not record an empty attempt as a personal best just because the player
            // selected a higher difficulty. At least one full wave must be secured.
            newRecord = waves > 0 && (waves > oldWave || (waves == oldWave && difficulty > oldDifficulty));
            if (newRecord)
            {
                save.bestRefugeDefenseWave = waves;
                save.bestRefugeDefenseDifficulty = difficulty;
            }
            // Preserve any unrelated campaign run state. Special modes never create activeRun.
            if (!GameSession.DebugRun)
            {
                try { SaveSystem.Save(save); saved = true; }
                catch (Exception e) { Debug.LogWarning("Zombit: failed to save Refuge Defense result: " + e.Message); }
            }
        }

        Time.timeScale = 0f;
        BuildRefugeDefenseResult(win, abandoned, waves, kills, bosses, seconds, materials, newRecord, saved);
    }

    void BuildRefugeDefenseResult(bool win, bool abandoned, int waves, int kills, int bosses, int seconds,
        int materials, bool newRecord, bool saved)
    {
        var location = GameSession.Location;
        var canvas = UIFactory.MakeCanvas("RefugeDefenseResultCanvas", 116);
        var art = UIFactory.Img(canvas.transform, Color.white, "ResultBackdropArt");
        art.sprite = ArtLoader.Get(SceneBackgrounds.Get("military_defense").path);
        art.preserveAspect = false;
        art.color = new Color(0.46f, 0.55f, 0.57f, 0.50f);
        UIFactory.Stretch(art.rectTransform);
        art.raycastTarget = false;
        art.transform.SetAsFirstSibling();

        var dim = UIFactory.Img(canvas.transform, new Color(0.004f, 0.009f, 0.014f, 0.90f), "Dim");
        UIFactory.Stretch(dim.rectTransform);
        dim.raycastTarget = false;

        var panel = UIKit.Frame(canvas.transform, "RefugeDefenseResultPanel", new Color(0.018f, 0.031f, 0.043f, 0.97f),
            new Color(0.88f, 0.34f, 0.13f, 0.96f), 2.5f);
        UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 660f));

        string titleText = win ? "DEFESA CONCLUÍDA" : abandoned ? "DEFESA ENCERRADA" : "GERADOR DESTRUÍDO";
        var title = UIFactory.Txt(panel, titleText, 50, TextAlignmentOptions.Center,
            win ? new Color(1f, 0.75f, 0.34f) : new Color(1f, 0.40f, 0.29f), "ResultTitle");
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1160f, 70f));
        title.fontStyle = FontStyles.Bold;

        string subtitleText = win
            ? "Todas as cinco ondas foram derrotadas. O gerador e os suprimentos estão seguros."
            : abandoned
                ? "A defesa foi encerrada antes do fim. Você mantém os materiais das ondas concluídas."
                : "O gerador foi destruído. Você mantém os materiais das ondas concluídas.";
        var subtitle = UIFactory.Txt(panel, subtitleText, 21, TextAlignmentOptions.Center,
            new Color(0.78f, 0.84f, 0.87f), "ResultSubtitle");
        UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -119f), new Vector2(1140f, 38f));

        RefugeMetric(panel, "ONDAS CONCLUÍDAS", waves + " / " + RefugeDefenseController.TotalWaves, -390f);
        RefugeMetric(panel, "ZUMBIS ELIMINADOS", kills.ToString("N0"), 0f);
        RefugeMetric(panel, "CHEFES DERROTADOS", bosses.ToString("N0"), 390f);

        var reward = UIFactory.Txt(panel, "+" + materials.ToString("N0") + " MATERIAIS DE DEFESA", 36,
            TextAlignmentOptions.Center, new Color(1f, 0.80f, 0.38f), "DefenseMaterialReward");
        UIFactory.Place(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(1080f, 58f));
        reward.fontStyle = FontStyles.Bold;

        var save = GameSession.Save;
        string best = save != null
            ? "MELHOR RESULTADO: ONDA " + save.bestRefugeDefenseWave.ToString("00") + "  ·  " + DifficultyName(save.bestRefugeDefenseDifficulty)
            : "MELHOR RESULTADO: ONDA " + waves.ToString("00");
        if (newRecord) best = "NOVO RECORDE PESSOAL!  ·  " + best;
        var record = UIFactory.Txt(panel, best, 21, TextAlignmentOptions.Center,
            newRecord ? new Color(1f, 0.73f, 0.25f) : new Color(0.60f, 0.70f, 0.74f), "Record");
        UIFactory.Place(record.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -184f), new Vector2(1080f, 38f));

        string duration = TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"mm\:ss");
        var time = UIFactory.Txt(panel, "TEMPO DE DEFESA  " + duration, 17, TextAlignmentOptions.Center,
            new Color(0.65f, 0.75f, 0.78f), "DefenseTime");
        UIFactory.Place(time.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -224f), new Vector2(800f, 28f));

        string saveStatus = GameSession.DebugRun ? "MODO DE TESTE — o progresso não foi salvo."
            : saved ? "Progresso salvo. XP de campanha, ouro e equipamentos não foram alterados."
            : "Falha ao salvar. Verifique o Console do Unity.";
        var status = UIFactory.Txt(panel, saveStatus, 17, TextAlignmentOptions.Center,
            saved || GameSession.DebugRun ? new Color(0.65f, 0.75f, 0.78f) : new Color(1f, 0.35f, 0.27f), "SaveStatus");
        UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(1080f, 28f));

        var back = UIFactory.Btn(panel, "VOLTAR AO ACAMPAMENTO", 22, () =>
        {
            Time.timeScale = 1f;
            GameSession.Mode = GameMode.Campaign;
            GameSession.OpenMapOnLoad = false;
            SceneManager.LoadScene(GameSession.MenuSceneIndex);
        }, new Color(0.12f, 0.17f, 0.20f), "BackToCamp");
        UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-210f, 28f), new Vector2(340f, 64f));

        var retry = UIFactory.Btn(panel, "JOGAR NOVAMENTE", 22, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }, new Color(0.56f, 0.13f, 0.08f), "RetryRefugeDefense");
        UIFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(210f, 28f), new Vector2(340f, 64f));
    }

    void RefugeMetric(Transform parent, string label, string value, float x)
    {
        var box = UIKit.Frame(parent, "Metric_" + label.Replace(" ", ""), new Color(0.035f, 0.055f, 0.067f, 0.98f),
            new Color(0.25f, 0.34f, 0.38f, 0.85f), 1.2f);
        UIFactory.Place(box, new Vector2(0.5f, 0.5f), new Vector2(x, 12f), new Vector2(330f, 102f));
        var caption = UIFactory.Txt(box, label, 16, TextAlignmentOptions.Center,
            new Color(0.67f, 0.77f, 0.80f), "MetricLabel");
        UIFactory.Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(300f, 28f));
        var number = UIFactory.Txt(box, value, 31, TextAlignmentOptions.Center, Color.white, "MetricValue");
        UIFactory.Place(number.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(300f, 42f));
        number.fontStyle = FontStyles.Bold;
    }

    static string DifficultyName(int difficulty)
    {
        switch (difficulty)
        {
            case 1: return "FÁCIL";
            case 2: return "NORMAL";
            case 3: return "DIFÍCIL";
            case 4: return "PESADELO";
            default: return "—";
        }
    }
}
