using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss Rush: cinco chefes regionais em sequencia, com escalonamento por confronto,
/// intervalo curto e cura parcial da equipe. Recompensas sao liquidadas por MissionFlow.
/// </summary>
public sealed class BossRushController : MonoBehaviour
{
    public const int BaseTokensPerBoss = 2;
    public const int ClearBonusBase = 5;
    public const float IntermissionSeconds = 8f;
    public const float IntermissionHealFraction = 0.12f;

    static readonly string[] ThemeOrder = { "escola", "delegacia", "hospital", "centro", "base" };
    static readonly string[] BossTitles =
    {
        "MUTANT PRINCIPAL", "CORRUPTED COMMANDER", "PATIENT ZERO", "INFECTION TITAN", "ALPHA EXPERIMENT"
    };

    sealed class BossTarget
    {
        public string theme;
        public string title;
        public EnemyEntry entry;
    }

    readonly List<BossTarget> sequence = new List<BossTarget>();
    Transform hero;
    Transform[] spawnPoints;
    Enemy activeBoss;
    Canvas hudCanvas;
    public Canvas LayoutCanvas => hudCanvas;
    TMP_Text roundText, bossText, statusText, rewardText;
    float elapsed;
    float hudTimer;
    float intermissionRemaining;
    float baseBossHealthMult = 1f;
    float baseDamageMult = 1f;
    float baseSpeedMult = 1f;
    bool initialised;
    bool sequenceStarted;
    bool signalled;

    public int BossesDefeated { get; private set; }
    public int TotalBosses => sequence.Count;
    public int CurrentBossNumber { get; private set; }
    public float ElapsedSeconds => elapsed;
    public bool IsIntermission => intermissionRemaining > 0f;

    public void Initialise(Transform heroTarget, Transform[] points)
    {
        hero = heroTarget;
        spawnPoints = points;
        initialised = true;
    }

    void OnEnable() => GameEvents.EnemyKilled += OnEnemyKilled;
    void OnDisable() => GameEvents.EnemyKilled -= OnEnemyKilled;

    void Start()
    {
        if (!GameSession.IsBossRush) { Destroy(this); return; }
        if (!initialised || hero == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Zombit Boss Rush: faltam o heroi ou os pontos de nascimento.");
            GameEvents.MissionFailed?.Invoke();
            return;
        }

        BuildSequence();
        if (sequence.Count == 0)
        {
            Debug.LogError("Zombit Boss Rush: nenhum chefe regional foi encontrado nos Resources/Config/Enemies.");
            GameEvents.MissionFailed?.Invoke();
            return;
        }

        baseBossHealthMult = Enemy.BossHealthMult;
        baseDamageMult = Enemy.DamageMult;
        baseSpeedMult = Enemy.SpeedMult;
        GameEvents.HordeMode = false;
        GameEvents.HordeTimeLeft = -1f;
        GameEvents.WaveTimeLeft = -1f;
        GameEvents.HordeFury = false;

        BuildHud();
        RefreshHud();
        StartCoroutine(RunSequence());
    }

    void Update()
    {
        if (signalled || !sequenceStarted) return;
        if (Time.timeScale > 0f) elapsed += Time.deltaTime;
        RefreshHudThrottled();
    }

    void RefreshHudThrottled()
    {
        hudTimer -= Time.unscaledDeltaTime;
        if (hudTimer > 0f) return;
        hudTimer = 0.2f;
        RefreshHud();
    }

    void BuildSequence()
    {
        sequence.Clear();
        for (int i = 0; i < ThemeOrder.Length; i++)
        {
            var roster = EnemyRoster.Get(ThemeOrder[i]);
            if (roster == null || roster.boss == null || roster.boss.sprite == null || roster.boss.data == null)
            {
                Debug.LogWarning("Zombit Boss Rush: chefe ausente no tema '" + ThemeOrder[i] + "'. Esta etapa sera ignorada.");
                continue;
            }
            sequence.Add(new BossTarget
            {
                theme = ThemeOrder[i],
                title = BossTitles[i],
                entry = roster.boss
            });
        }
    }

    IEnumerator RunSequence()
    {
        sequenceStarted = true;
        for (int i = 0; i < sequence.Count; i++)
        {
            if (MissionEnded()) yield break;

            CurrentBossNumber = i + 1;
            GameEvents.WaveChanged?.Invoke(CurrentBossNumber, sequence.Count);
            Enemy.BossHealthMult = baseBossHealthMult * (1f + 0.22f * i);
            Enemy.DamageMult = baseDamageMult * (1f + 0.10f * i);
            Enemy.SpeedMult = baseSpeedMult * (1f + 0.035f * i);
            Enemy.FuryDamage = 1f;
            Enemy.FurySpeed = 1f;

            var point = FindSpawnPoint();
            if (point == null)
            {
                Debug.LogError("Zombit Boss Rush: nenhum ponto de spawn valido.");
                GameEvents.MissionFailed?.Invoke();
                yield break;
            }

            Vector3 pos = point.position;
            pos.x = Mathf.Max(pos.x, CameraFramer.RightSpawnX(pos.x));
            activeBoss = EnemyFactory.Spawn(sequence[i].entry, pos, hero);
            if (activeBoss == null)
            {
                Debug.LogError("Zombit Boss Rush: falha ao criar o chefe " + sequence[i].title + ".");
                GameEvents.MissionFailed?.Invoke();
                yield break;
            }
            RefreshHud();

            while (activeBoss != null && !activeBoss.IsDead)
            {
                if (MissionEnded()) yield break;
                yield return null;
            }

            // O evento de morte conta o chefe uma unica vez. Aguarda o corpo sair da arena.
            while (Enemy.Alive.Count > 0)
            {
                if (MissionEnded()) yield break;
                yield return null;
            }
            activeBoss = null;

            if (i < sequence.Count - 1)
            {
                HealSurvivingSquad();
                intermissionRemaining = IntermissionSeconds;
                while (intermissionRemaining > 0f)
                {
                    if (MissionEnded()) yield break;
                    if (Time.timeScale > 0f) intermissionRemaining = Mathf.Max(0f, intermissionRemaining - Time.deltaTime);
                    RefreshHudThrottled();
                    yield return null;
                }
            }
        }

        if (!MissionEnded())
        {
            signalled = true;
            GameEvents.MissionComplete?.Invoke();
        }
    }

    Transform FindSpawnPoint()
    {
        if (spawnPoints == null) return null;
        for (int i = 0; i < spawnPoints.Length; i++) if (spawnPoints[i] != null) return spawnPoints[i];
        return null;
    }

    void OnEnemyKilled(Enemy enemy)
    {
        if (enemy == null || enemy != activeBoss || !enemy.IsBoss) return;
        BossesDefeated = Mathf.Min(sequence.Count, BossesDefeated + 1);
        activeBoss = enemy;
        RefreshHud();
    }

    void HealSurvivingSquad()
    {
        foreach (var member in HeroSquad.Members)
        {
            if (member == null || !member.Alive || member.health == null) continue;
            member.health.Heal(member.health.max * IntermissionHealFraction);
        }
    }

    bool MissionEnded()
    {
        var flow = FindFirstObjectByType<MissionFlow>();
        return signalled || (flow != null && flow.IsFinished);
    }

    public int CalculateBossTokens(bool fullClear)
    {
        int perBoss = BaseTokensPerBoss + Mathf.Clamp(GameSession.Difficulty, 1, 4);
        int tokens = BossesDefeated * perBoss;
        if (fullClear) tokens += ClearBonusBase + Mathf.Clamp(GameSession.Difficulty, 1, 4);
        return Mathf.Max(0, tokens);
    }

    void BuildHud()
    {
        hudCanvas = UIFactory.MakeCanvas("BossRushHUD", 86);
        var panel = UIKit.Frame(hudCanvas.transform, "BossRushStatus",
            new Color(0.018f, 0.032f, 0.043f, 0.95f), new Color(0.88f, 0.30f, 0.12f, 0.95f), 2f);
        UIFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(680f, 112f));

        roundText = UIFactory.Txt(panel, "BOSS 01 / 05", 25, TextAlignmentOptions.Center,
            new Color(1f, 0.68f, 0.27f), "Round");
        UIFactory.Place(roundText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(640f, 34f));
        roundText.fontStyle = FontStyles.Bold;

        bossText = UIFactory.Txt(panel, "MUTANT PRINCIPAL", 31, TextAlignmentOptions.Center,
            Color.white, "BossName");
        UIFactory.Place(bossText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -3f), new Vector2(640f, 42f));
        bossText.fontStyle = FontStyles.Bold;

        rewardText = UIFactory.Txt(panel, "0 / 5 DEFEATED  ·  0 BOSS TOKENS", 17, TextAlignmentOptions.Center,
            new Color(0.77f, 0.84f, 0.87f), "BossProgress");
        UIFactory.Place(rewardText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(640f, 28f));

        statusText = UIFactory.Txt(hudCanvas.transform, "DEFEAT EVERY MUTANT", 17, TextAlignmentOptions.Center,
            new Color(0.88f, 0.91f, 0.92f), "BossStatus");
        UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -137f), new Vector2(680f, 30f));
    
        if (hudCanvas != null) UILayoutPrefabApplier.Apply(hudCanvas.transform as RectTransform, "bossRushHud");
    }

    void RefreshHud()
    {
        if (roundText == null) return;
        int current = Mathf.Clamp(CurrentBossNumber, 1, Mathf.Max(1, sequence.Count));
        roundText.text = "BOSS " + current.ToString("00") + " / " + sequence.Count.ToString("00");
        if (CurrentBossNumber > 0 && CurrentBossNumber <= sequence.Count)
            bossText.text = sequence[CurrentBossNumber - 1].title;
        rewardText.text = BossesDefeated + " / " + sequence.Count + " DEFEATED  ·  " + CalculateBossTokens(false) + " BOSS TOKENS";
        if (intermissionRemaining > 0f)
            statusText.text = "NEXT TARGET IN " + Mathf.CeilToInt(intermissionRemaining) + " SECONDS  ·  SQUAD RECOVERED 12% HP";
        else if (BossesDefeated >= sequence.Count && sequence.Count > 0)
            statusText.text = "ALL TARGETS ELIMINATED";
        else
            statusText.text = "DEFEAT EVERY MUTANT  ·  NO CAMPAIGN XP OR EQUIPMENT LOSS";
    }
}
