using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class MissionFlow
{
    void FinishBossRush(bool win, bool abandoned)
    {
        if (done) return;
        done = true;

        var controller = FindFirstObjectByType<BossRushController>();
        int total = controller != null ? controller.TotalBosses : 5;
        int defeated = controller != null ? controller.BossesDefeated : 0;
        bool fullClear = win && total > 0 && defeated >= total;
        int seconds = controller != null ? Mathf.Max(0, Mathf.FloorToInt(controller.ElapsedSeconds)) : Mathf.Max(0, Mathf.FloorToInt(elapsed));
        int tokens = controller != null
            ? controller.CalculateBossTokens(fullClear)
            : defeated * (2 + Mathf.Clamp(GameSession.Difficulty, 1, 4)) + (fullClear ? 5 + Mathf.Clamp(GameSession.Difficulty, 1, 4) : 0);

        var save = GameSession.Save;
        bool newRecord = false;
        bool saved = false;
        if (save != null)
        {
            save.bossTokens = Mathf.Max(0, save.bossTokens + tokens);
            int difficulty = Mathf.Clamp(GameSession.Difficulty, 1, 4);
            int previousBosses = save.bestBossRushBossesDefeated;
            int previousDifficulty = save.bestBossRushDifficulty;
            int previousTime = save.bestBossRushClearSeconds;
            newRecord = defeated > previousBosses ||
                (defeated == previousBosses && difficulty > previousDifficulty) ||
                (fullClear && defeated == previousBosses && difficulty == previousDifficulty && (previousTime <= 0 || seconds < previousTime));

            if (defeated > previousBosses || (defeated == previousBosses && difficulty > previousDifficulty))
            {
                save.bestBossRushBossesDefeated = defeated;
                save.bestBossRushDifficulty = difficulty;
                save.bestBossRushClearSeconds = fullClear ? seconds : 0;
            }
            else if (fullClear && defeated == previousBosses && difficulty == previousDifficulty && (previousTime <= 0 || seconds < previousTime))
            {
                save.bestBossRushClearSeconds = seconds;
            }
            save.activeRun = new ActiveRun();
            if (!GameSession.DebugRun)
            {
                try { SaveSystem.Save(save); saved = true; }
                catch (Exception e) { Debug.LogWarning("Zombit: falha ao salvar o resultado do Boss Rush: " + e.Message); }
            }
        }

        Time.timeScale = 0f;
        BuildBossRushPanel(fullClear, abandoned, defeated, total, seconds, tokens, newRecord, saved);
    }

    void BuildBossRushPanel(bool fullClear, bool abandoned, int defeated, int total, int seconds,
        int tokens, bool newRecord, bool saved)
    {
        var loc = GameSession.Location;
        var canvas = UIFactory.MakeCanvas("BossRushResultCanvas", 112);
        var art = UIFactory.Img(canvas.transform, Color.white, "ResultBackdropArt");
        art.sprite = loc != null ? ArtLoader.Get(SceneBackgrounds.PathFor(loc)) : ArtLoader.Get(SceneBackgrounds.DefaultPath);
        art.preserveAspect = false;
        art.color = new Color(0.48f, 0.56f, 0.60f, 0.48f);
        UIFactory.Stretch(art.rectTransform);
        art.raycastTarget = false;
        art.transform.SetAsFirstSibling();

        var dim = UIFactory.Img(canvas.transform, new Color(0.004f, 0.009f, 0.014f, 0.89f), "Dim");
        UIFactory.Stretch(dim.rectTransform);
        dim.raycastTarget = false;

        var panel = UIKit.Frame(canvas.transform, "BossRushResultPanel", new Color(0.018f, 0.031f, 0.043f, 0.97f),
            new Color(0.91f, 0.29f, 0.12f, 0.96f), 2.5f);
        UIFactory.Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1260f, 680f));

        string titleText = fullClear ? "BOSS RUSH COMPLETED" : abandoned ? "RUN ENDED" : "SQUAD ELIMINATED";
        var title = UIFactory.Txt(panel, titleText, 52, TextAlignmentOptions.Center,
            fullClear ? new Color(1f, 0.73f, 0.32f) : new Color(1f, 0.38f, 0.29f), "ResultTitle");
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(1160f, 74f));
        title.fontStyle = FontStyles.Bold;

        string subtitleText = fullClear
            ? "All regional bosses defeated. Your Boss Tokens are secured."
            : abandoned
                ? "The run ended early. Tokens for defeated bosses are kept. No equipment is lost."
                : "Your squad was overwhelmed. Tokens for defeated bosses are kept. No equipment is lost.";
        var subtitle = UIFactory.Txt(panel, subtitleText, 22, TextAlignmentOptions.Center,
            new Color(0.78f, 0.84f, 0.87f), "ResultSubtitle");
        UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(1140f, 42f));

        string clearTime = fullClear
            ? TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"mm\:ss")
            : "—";
        ResultMetric(panel, "BOSSES DEFEATED", defeated + " / " + total, -390f);
        ResultMetric(panel, "DIFFICULTY", DifficultyLabel(GameSession.Difficulty), 0f);
        ResultMetric(panel, "CLEAR TIME", clearTime, 390f);

        var reward = UIFactory.Txt(panel, "+" + tokens.ToString("N0") + " BOSS TOKENS", 38,
            TextAlignmentOptions.Center, new Color(1f, 0.79f, 0.37f), "BossTokenReward");
        UIFactory.Place(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(1040f, 62f));
        reward.fontStyle = FontStyles.Bold;

        int bestBosses = GameSession.Save != null ? GameSession.Save.bestBossRushBossesDefeated : defeated;
        int bestDiff = GameSession.Save != null ? GameSession.Save.bestBossRushDifficulty : GameSession.Difficulty;
        int bestTime = GameSession.Save != null ? GameSession.Save.bestBossRushClearSeconds : 0;
        string bestLine = "BEST: " + bestBosses + " / " + total + "  ·  " + DifficultyLabel(bestDiff);
        if (bestTime > 0) bestLine += "  ·  " + TimeSpan.FromSeconds(bestTime).ToString(@"mm\:ss");
        if (newRecord) bestLine = "NEW PERSONAL RECORD!  ·  " + bestLine;
        var record = UIFactory.Txt(panel, bestLine, 21, TextAlignmentOptions.Center,
            newRecord ? new Color(1f, 0.73f, 0.25f) : new Color(0.60f, 0.70f, 0.74f), "Record");
        UIFactory.Place(record.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -190f), new Vector2(1080f, 40f));

        string saveStatus = GameSession.DebugRun ? "TEST RUN — progress was not saved."
            : saved ? "Progress saved." : "Save failed. Check the Unity Console.";
        var status = UIFactory.Txt(panel, saveStatus, 18, TextAlignmentOptions.Center,
            saved || GameSession.DebugRun ? new Color(0.65f, 0.75f, 0.78f) : new Color(1f, 0.35f, 0.27f), "SaveStatus");
        UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(1000f, 30f));

        var back = UIFactory.Btn(panel, "BACK TO CAMP", 23, () =>
        {
            Time.timeScale = 1f;
            GameSession.Mode = GameMode.Campaign;
            GameSession.OpenMapOnLoad = false;
            SceneManager.LoadScene(GameSession.MenuSceneIndex);
        }, new Color(0.12f, 0.17f, 0.20f), "BackToCamp");
        UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-220f, 34f), new Vector2(350f, 66f));

        var retry = UIFactory.Btn(panel, "PLAY AGAIN", 23, () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }, new Color(0.56f, 0.13f, 0.08f), "RetryBossRush");
        UIFactory.Place((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(220f, 34f), new Vector2(350f, 66f));
    }

    static string DifficultyLabel(int difficulty)
    {
        switch (difficulty)
        {
            case 1: return "EASY";
            case 2: return "NORMAL";
            case 3: return "HARD";
            case 4: return "NIGHTMARE";
            default: return "NORMAL";
        }
    }
}
