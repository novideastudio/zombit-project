using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime do modo Last Stand: cronometro, estagios, contagem de chefes e extracao.
/// Recompensas sao liquidadas pelo MissionFlow, nunca aqui, para evitar salvar duas vezes.
/// </summary>
public class LastStandController : MonoBehaviour
{
    public const float StageSeconds = 180f;
    public const float ExtractionSeconds = 8f;

    public float ElapsedSeconds { get; private set; }
    public int Stage => 1 + Mathf.FloorToInt(ElapsedSeconds / StageSeconds);
    public int Kills { get; private set; }
    public int BossesDefeated { get; private set; }
    public bool IsExtracting { get; private set; }
    public float ExtractionRemaining { get; private set; }

    Canvas hudCanvas;
    public Canvas LayoutCanvas => hudCanvas;
    TMP_Text timeText, stageText, statsText, tokenText, statusText;
    Button extractionButton;
    float refreshTimer;
    bool missionSignalled;

    void OnEnable() => GameEvents.EnemyKilled += OnEnemyKilled;
    void OnDisable() => GameEvents.EnemyKilled -= OnEnemyKilled;

    void Start()
    {
        if (!GameSession.IsLastStand) { Destroy(this); return; }
        BuildHud();
        RefreshHud();
    }

    void Update()
    {
        if (!GameSession.IsLastStand || missionSignalled) return;

        if (Time.timeScale > 0f)
        {
            ElapsedSeconds += Time.deltaTime;
            if (IsExtracting)
            {
                ExtractionRemaining = Mathf.Max(0f, ExtractionRemaining - Time.deltaTime);
                if (ExtractionRemaining <= 0f)
                {
                    missionSignalled = true;
                    GameEvents.MissionComplete?.Invoke();
                }
            }
        }

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.2f;
            RefreshHud();
        }
    }

    void OnEnemyKilled(Enemy enemy)
    {
        if (enemy == null) return;
        Kills++;
        if (enemy.IsBoss) BossesDefeated++;
    }

    /// <summary>Fichas por tempo, estagios e chefes; a extracao concluida concede um bonus pequeno.</summary>
    public int CalculateResistanceTokens(bool evacuated)
    {
        int minutes = Mathf.FloorToInt(ElapsedSeconds / 60f);
        int stageBonus = Mathf.Max(0, Stage - 1) * 2;
        int bossBonus = BossesDefeated * 4;
        int total = Mathf.Max(0, minutes + stageBonus + bossBonus);
        if (evacuated && total > 0) total += Mathf.Max(1, Mathf.RoundToInt(total * 0.15f));
        return total;
    }

    public void ToggleExtraction()
    {
        if (missionSignalled || (FindFirstObjectByType<MissionFlow>()?.IsFinished ?? false)) return;
        IsExtracting = !IsExtracting;
        ExtractionRemaining = IsExtracting ? ExtractionSeconds : 0f;
        RefreshHud();
    }

    void BuildHud()
    {
        hudCanvas = UIFactory.MakeCanvas("LastStandHUD", 85);
        var panel = UIKit.Frame(hudCanvas.transform, "LastStandStatus",
            new Color(0.018f, 0.035f, 0.045f, 0.94f), new Color(0.88f, 0.30f, 0.12f, 0.95f), 2f);
        UIFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(620f, 104f));

        timeText = UIFactory.Txt(panel, "00:00", 34, TextAlignmentOptions.Center, Color.white, "Elapsed");
        UIFactory.Place(timeText.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(170f, 52f));
        timeText.fontStyle = FontStyles.Bold;

        stageText = UIFactory.Txt(panel, "STAGE 01", 25, TextAlignmentOptions.Center, new Color(1f, 0.68f, 0.27f), "Stage");
        UIFactory.Place(stageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(200f, 36f));
        stageText.fontStyle = FontStyles.Bold;

        statsText = UIFactory.Txt(panel, "0 KILLS  ·  0 BOSSES", 17, TextAlignmentOptions.Center, new Color(0.72f, 0.82f, 0.86f), "Stats");
        UIFactory.Place(statsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(260f, 28f));

        tokenText = UIFactory.Txt(panel, "0 RESISTANCE TOKENS", 18, TextAlignmentOptions.Center, new Color(1f, 0.82f, 0.38f), "TokenPreview");
        UIFactory.Place(tokenText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(190f, 52f));
        tokenText.fontStyle = FontStyles.Bold;

        statusText = UIFactory.Txt(hudCanvas.transform, "SURVIVE AS LONG AS YOU CAN", 16,
            TextAlignmentOptions.Center, new Color(0.84f, 0.89f, 0.91f), "Status");
        UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(560f, 28f));

        extractionButton = UIFactory.Btn(hudCanvas.transform, "REQUEST EXTRACTION", 22,
            ToggleExtraction, new Color(0.54f, 0.13f, 0.08f), "ExtractionButton");
        UIFactory.Place((RectTransform)extractionButton.transform, new Vector2(0.5f, 1f),
            new Vector2(0f, -164f), new Vector2(300f, 58f));
    
        if (hudCanvas != null) UILayoutPrefabApplier.Apply(hudCanvas.transform as RectTransform, "lastStandHud");
    }

    void RefreshHud()
    {
        if (timeText == null) return;
        var elapsed = TimeSpan.FromSeconds(Mathf.Max(0, ElapsedSeconds));
        timeText.text = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"h\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");
        stageText.text = "STAGE " + Stage.ToString("00");
        statsText.text = Kills.ToString("N0") + " KILLS  ·  " + BossesDefeated + " BOSSES";
        tokenText.text = CalculateResistanceTokens(false).ToString("N0") + " TOKENS";

        if (IsExtracting)
        {
            statusText.text = "EXTRACTION IN " + Mathf.CeilToInt(ExtractionRemaining) + " SECONDS — HOLD THE LINE";
            extractionButton.GetComponentInChildren<TMP_Text>().text = "CANCEL EXTRACTION";
            UIFactory.SetButtonColor(extractionButton, new Color(0.20f, 0.30f, 0.34f));
        }
        else
        {
            statusText.text = "SURVIVE AS LONG AS YOU CAN · EXTRACT TO BANK YOUR TOKENS";
            extractionButton.GetComponentInChildren<TMP_Text>().text = "REQUEST EXTRACTION";
            UIFactory.SetButtonColor(extractionButton, new Color(0.54f, 0.13f, 0.08f));
        }
    }
}
