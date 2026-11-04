using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Refuge Defense: five escalating enemy waves attack a physical generator target.
/// Match-only field supplies build temporary turrets, barricades and mines; the
/// persistent Defense Materials currency is awarded by MissionFlow after the run.
/// </summary>
public sealed class RefugeDefenseController : MonoBehaviour
{
    public const int TotalWaves = 5;
    public const float IntermissionSeconds = 8f;
    const float GeneratorX = -5.65f; // fica atras da linha de sobreviventes; zumbis chegam pela direita
    const float TurretRange = 11.5f;
    const float TurretInterval = 0.95f;
    const int InitialFieldSupplies = 65;

    sealed class TurretState
    {
        public GameObject visual;
        public float nextShot;
    }

    static RefugeDefenseController instance;
    static Sprite generatorSprite;
    static Sprite turretSprite;
    static Sprite barricadeSprite;
    static Sprite mineSprite;

    public static Transform DefenseTarget => instance != null ? instance.generatorTransform : null;
    public static Health GeneratorHealth => instance != null ? instance.generatorHealth : null;

    public int WavesCompleted { get; private set; }
    public int Kills { get; private set; }
    public int BossesDefeated { get; private set; }
    public int FieldSupplies { get; private set; }
    public float ElapsedSeconds { get; private set; }
    public float GeneratorIntegrity => generatorHealth != null && generatorHealth.max > 0f
        ? Mathf.Clamp01(generatorHealth.Current / generatorHealth.max) : 0f;

    Transform hero;
    Transform[] spawnPoints;
    Transform generatorTransform;
    Health generatorHealth;
    SpriteRenderer generatorRenderer;
    SpriteRenderer barricadeRenderer;
    float barrierCurrent;
    float barrierMax;
    float nextMineTime;
    float baseEnemyHealth;
    float baseEnemyDamage;
    float baseGeneratorMax;
    float refreshTimer;
    bool initialized;
    bool missionSignalled;
    Roster roster;

    readonly List<TurretState> turrets = new List<TurretState>();
    readonly List<GameObject> mineVisuals = new List<GameObject>();
    int mineCharges;

    Canvas hudCanvas;
    public Canvas LayoutCanvas => hudCanvas;
    TMP_Text waveText;
    TMP_Text generatorText;
    TMP_Text barrierText;
    TMP_Text killsText;
    TMP_Text suppliesText;
    TMP_Text statusText;
    Button turretButton;
    Button barricadeButton;
    Button mineButton;
    Button repairButton;

    public void Initialise(Transform heroTransform, Transform[] points)
    {
        if (initialized) return;
        initialized = true;
        instance = this;
        hero = heroTransform;
        spawnPoints = points;
        FieldSupplies = InitialFieldSupplies;
        CreateGenerator();
    }

    void OnEnable() => GameEvents.EnemyKilled += OnEnemyKilled;
    void OnDisable() => GameEvents.EnemyKilled -= OnEnemyKilled;

    void Start()
    {
        if (!GameSession.IsRefugeDefense)
        {
            Destroy(this);
            return;
        }
        if (!initialized) Initialise(hero, spawnPoints);
        if (generatorHealth == null)
        {
            Debug.LogError("Zombit: Refuge Defense generator could not be initialized.");
            return;
        }

        roster = EnemyRoster.Get(GameSession.Location != null ? GameSession.Location.theme : "base");
        if (roster == null || roster.commons.Count == 0)
        {
            Debug.LogError("Zombit: Refuge Defense has no common enemies configured.");
            GameEvents.MissionFailed?.Invoke();
            return;
        }

        baseEnemyHealth = Enemy.HealthMult;
        baseEnemyDamage = Enemy.DamageMult;
        BuildHud();
        RefreshHud();
        StartCoroutine(RunWaves());
    }

    void Update()
    {
        if (!GameSession.IsRefugeDefense || missionSignalled || Time.timeScale <= 0f) return;
        ElapsedSeconds += Time.deltaTime;
        UpdateTurrets();
        UpdateMines();

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.2f;
            RefreshHud();
        }
    }

    void OnDestroy()
    {
        if (generatorHealth != null) generatorHealth.Died -= OnGeneratorDestroyed;
        if (generatorHealth != null) generatorHealth.Changed -= OnGeneratorChanged;
        if (instance == this) instance = null;
    }

    public static void ResetRuntime()
    {
        instance = null;
    }

    void CreateGenerator()
    {
        var save = GameSession.Save;
        int upgrade = save != null ? Mathf.Clamp(save.refugeGeneratorLevel, 0, 5) : 0;
        int barricadeUpgrade = save != null ? Mathf.Clamp(save.refugeBarricadeLevel, 0, 5) : 0;
        baseGeneratorMax = 620f * (1f + upgrade * 0.18f);

        float groundY = GameTuning.Current.HeroFeetY(0);
        var generator = new GameObject("Refuge_Generator");
        generator.transform.position = new Vector3(GeneratorX, groundY, 0f);
        generator.transform.localScale = Vector3.one * 1.15f;
        generatorTransform = generator.transform;
        generatorHealth = generator.AddComponent<Health>();
        generatorHealth.Init(baseGeneratorMax);
        generatorHealth.Died += OnGeneratorDestroyed;
        generatorHealth.Changed += OnGeneratorChanged;
        generatorRenderer = generator.AddComponent<SpriteRenderer>();
        generatorRenderer.sprite = GetGeneratorSprite();
        generatorRenderer.sortingOrder = 20;
        generatorRenderer.color = Color.white;

        var shield = new GameObject("Refuge_BarricadeVisual");
        shield.transform.position = new Vector3(GeneratorX + 1.05f, groundY, 0.01f);
        shield.transform.localScale = new Vector3(1f, 1f, 1f);
        barricadeRenderer = shield.AddComponent<SpriteRenderer>();
        barricadeRenderer.sprite = GetBarricadeSprite();
        barricadeRenderer.sortingOrder = 18;
        shield.transform.localScale = new Vector3(1.10f, 1.10f, 1f);
        barrierMax = 95f * (1f + barricadeUpgrade * 0.22f);
        barrierCurrent = barrierMax;
        RefreshBarricadeVisual();
    }

    IEnumerator RunWaves()
    {
        int difficulty = Mathf.Clamp(GameSession.Difficulty, 1, 4);
        float difficultyCount = 0.78f + (difficulty - 1) * 0.18f;
        int squadSize = Mathf.Max(1, HeroSquad.Count);
        float squadCount = HeroSquad.HordeMultFor(squadSize);

        for (int wave = 1; wave <= TotalWaves; wave++)
        {
            if (missionSignalled) yield break;
            Enemy.HealthMult = baseEnemyHealth * (1f + (wave - 1) * 0.13f);
            Enemy.DamageMult = baseEnemyDamage * (1f + (wave - 1) * 0.10f);
            GameEvents.WaveChanged?.Invoke(wave, TotalWaves);
            SetStatus("ZUMBIS SE APROXIMANDO — PROTEJA O GERADOR");

            int commonCount = Mathf.CeilToInt((10 + wave * 4) * difficultyCount * squadCount);
            int eliteCount = wave >= 2 ? Mathf.CeilToInt((wave + 0.5f) * (0.8f + (difficulty - 1) * 0.15f)) : 0;
            var queue = new List<EnemyEntry>(commonCount + eliteCount + 1);
            for (int i = 0; i < commonCount; i++)
                if (roster.commons.Count > 0) queue.Add(roster.commons[UnityEngine.Random.Range(0, roster.commons.Count)]);
            for (int i = 0; i < eliteCount; i++)
                if (roster.elites.Count > 0) queue.Add(roster.elites[UnityEngine.Random.Range(0, roster.elites.Count)]);

            // Deterministic shuffle keeps each wave mixed without producing the same common/elite block.
            for (int i = queue.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                EnemyEntry temp = queue[i]; queue[i] = queue[j]; queue[j] = temp;
            }

            for (int i = 0; i < queue.Count; i++)
            {
                if (missionSignalled || GeneratorIntegrity <= 0f) yield break;
                SpawnEnemy(queue[i]);
                yield return new WaitForSeconds(Mathf.Max(0.28f, 0.72f - wave * 0.045f));
            }

            if (wave == TotalWaves && roster.boss != null)
            {
                SetStatus("CHEFE SE APROXIMANDO — ATAQUE FINAL");
                yield return new WaitForSeconds(1.2f);
                SpawnEnemy(roster.boss);
            }

            while (!missionSignalled && GeneratorIntegrity > 0f && Enemy.Alive.Count > 0)
                yield return null;

            if (missionSignalled || GeneratorIntegrity <= 0f) yield break;
            WavesCompleted = wave;
            if (wave >= TotalWaves)
            {
                missionSignalled = true;
                SetStatus("BASE PROTEGIDA — TODAS AS ONDAS FORAM DERROTADAS");
                yield return new WaitForSeconds(0.6f);
                GameEvents.MissionComplete?.Invoke();
                yield break;
            }

            FieldSupplies += 25;
            SetStatus("ONDA CONCLUÍDA — +25 SUPRIMENTOS · PREPARE A DEFESA");
            RefreshHud();
            yield return new WaitForSeconds(IntermissionSeconds);
        }
    }

    void SpawnEnemy(EnemyEntry entry)
    {
        if (entry == null || entry.data == null || entry.sprite == null || generatorTransform == null) return;
        Vector3 position = new Vector3(9f, GameTuning.Current.ZombieFeetY(Mathf.Max(1, HeroSquad.Count)), 0f);
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int startIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            for (int offset = 0; offset < spawnPoints.Length; offset++)
            {
                Transform candidate = spawnPoints[(startIndex + offset) % spawnPoints.Length];
                if (candidate == null) continue;
                position = candidate.position;
                break;
            }
        }
        position.x = Mathf.Max(position.x, CameraFramer.RightSpawnX(position.x));
        position.x += UnityEngine.Random.Range(-0.15f, 0.15f);
        EliteMod mod = entry.tier == Tier.Elite ? EnemyFactory.RandomMod() : EliteMod.None;
        EnemyFactory.Spawn(entry, position, generatorTransform, mod);
    }

    void OnEnemyKilled(Enemy enemy)
    {
        if (!GameSession.IsRefugeDefense || enemy == null) return;
        Kills++;
        if (enemy.IsBoss) BossesDefeated++;
    }

    void OnGeneratorChanged(float current, float max)
    {
        if (generatorRenderer != null)
        {
            float ratio = max > 0f ? current / max : 0f;
            generatorRenderer.color = ratio <= 0.25f ? new Color(1f, 0.48f, 0.38f) : Color.white;
        }
        RefreshHud();
    }

    void OnGeneratorDestroyed()
    {
        if (missionSignalled) return;
        missionSignalled = true;
        SetStatus("GERADOR DESTRUÍDO — A BASE FOI PERDIDA");
        GameEvents.MissionFailed?.Invoke();
    }

    /// <summary>True when damage was handled by the generator's temporary barricade layer.</summary>
    public static bool TryReceiveGeneratorDamage(Health target, float damage)
    {
        if (!GameSession.IsRefugeDefense || instance == null || target == null || target != instance.generatorHealth)
            return false;
        instance.ReceiveGeneratorDamage(damage);
        return true;
    }

    void ReceiveGeneratorDamage(float damage)
    {
        float remaining = Mathf.Max(0f, damage);
        if (barrierCurrent > 0f && remaining > 0f)
        {
            float absorbed = Mathf.Min(barrierCurrent, remaining);
            barrierCurrent -= absorbed;
            remaining -= absorbed;
            RefreshBarricadeVisual();
        }
        if (remaining > 0f && generatorHealth != null && generatorHealth.Current > 0f)
            RegionalEffects.TakeDirect(generatorHealth, remaining);
        RefreshHud();
    }

    void UpdateTurrets()
    {
        if (turrets.Count == 0) return;
        int level = GameSession.Save != null ? GameSession.Save.refugeTurretLevel : 0;
        float damage = 30f * (1f + Mathf.Clamp(level, 0, 5) * 0.20f);
        for (int i = 0; i < turrets.Count; i++)
        {
            var turret = turrets[i];
            if (turret == null || turret.visual == null || Time.time < turret.nextShot) continue;
            Enemy target = FindNearestEnemy(turret.visual.transform.position, TurretRange);
            if (target == null) continue;
            turret.nextShot = Time.time + TurretInterval;
            target.Hit(damage, false);
            Vector3 from = turret.visual.transform.position + Vector3.up * 0.65f;
            PixelCombatVFX.Muzzle(from, (target.AimPoint - from).normalized);
        }
    }

    void UpdateMines()
    {
        if (mineCharges <= 0 || generatorTransform == null || Time.time < nextMineTime) return;
        Enemy trigger = null;
        float nearest = 3.5f;
        for (int i = 0; i < Enemy.Alive.Count; i++)
        {
            var enemy = Enemy.Alive[i];
            if (enemy == null || enemy.IsDead) continue;
            float dx = Mathf.Abs(enemy.transform.position.x - generatorTransform.position.x);
            if (dx < nearest) { nearest = dx; trigger = enemy; }
        }
        if (trigger == null) return;

        mineCharges--;
        nextMineTime = Time.time + 0.55f;
        Vector3 impact = trigger.AimPoint;
        PixelCombatVFX.Explosion(impact, 2.4f, true);
        for (int i = Enemy.Alive.Count - 1; i >= 0; i--)
        {
            var enemy = Enemy.Alive[i];
            if (enemy == null || enemy.IsDead) continue;
            if (Mathf.Abs(enemy.transform.position.x - impact.x) <= 2.2f)
                enemy.Hit(115f + (GameSession.Difficulty - 1) * 18f, false);
        }
        RefreshMineVisuals();
        RefreshHud();
    }

    Enemy FindNearestEnemy(Vector3 from, float range)
    {
        Enemy best = null;
        float bestSq = range * range;
        for (int i = 0; i < Enemy.Alive.Count; i++)
        {
            var enemy = Enemy.Alive[i];
            if (enemy == null || enemy.IsDead || enemy.isTrainingDummy) continue;
            float sq = (enemy.transform.position - from).sqrMagnitude;
            if (sq <= bestSq) { bestSq = sq; best = enemy; }
        }
        return best;
    }

    bool SpendSupplies(int amount)
    {
        if (missionSignalled || FieldSupplies < amount) return false;
        FieldSupplies -= amount;
        RefreshHud();
        return true;
    }

    void BuildTurret()
    {
        if (turrets.Count >= 3) { SetStatus("LIMITE DE 3 TORRETAS ATINGIDO"); return; }
        if (!SpendSupplies(25)) { SetStatus("SUPRIMENTOS INSUFICIENTES"); return; }
        float groundY = GameTuning.Current.HeroFeetY(0);
        float x = 0.85f + turrets.Count * 1.50f; // posicionadas a frente da equipe
        var go = new GameObject("Refuge_AutoTurret_" + (turrets.Count + 1));
        go.transform.position = new Vector3(x, groundY, -0.02f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetTurretSprite();
        sr.sortingOrder = 25;
        go.transform.localScale = Vector3.one * 1.0f;
        go.transform.position = new Vector3(x, groundY + sr.sprite.bounds.size.y * 0.5f, -0.02f);
        turrets.Add(new TurretState { visual = go, nextShot = Time.time + 0.3f });
        SetStatus("TORRETA AUTOMÁTICA INSTALADA");
        RefreshHud();
    }

    void ReinforceBarricade()
    {
        if (!SpendSupplies(20)) { SetStatus("SUPRIMENTOS INSUFICIENTES"); return; }
        float added = 115f + (GameSession.Save != null ? GameSession.Save.refugeBarricadeLevel * 22f : 0f);
        barrierMax += added;
        barrierCurrent = Mathf.Min(barrierMax, barrierCurrent + added);
        SetStatus("BARREIRA REFORÇADA · " + Mathf.RoundToInt(barrierCurrent) + " PV DE PROTEÇÃO");
        RefreshBarricadeVisual();
        RefreshHud();
    }

    void DeployMines()
    {
        if (!SpendSupplies(15)) { SetStatus("SUPRIMENTOS INSUFICIENTES"); return; }
        mineCharges += 2;
        RefreshMineVisuals();
        SetStatus("2 MINAS INSTALADAS");
        RefreshHud();
    }

    void RepairGenerator()
    {
        if (generatorHealth == null || generatorHealth.Current <= 0f) return;
        if (generatorHealth.Current >= generatorHealth.max - 0.1f) { SetStatus("O GERADOR JÁ ESTÁ COM INTEGRIDADE MÁXIMA"); return; }
        if (!SpendSupplies(20)) { SetStatus("SUPRIMENTOS INSUFICIENTES"); return; }
        generatorHealth.Heal(generatorHealth.max * 0.24f);
        SetStatus("GERADOR REPARADO · +24% DE INTEGRIDADE");
        RefreshHud();
    }

    void RefreshBarricadeVisual()
    {
        if (barricadeRenderer == null) return;
        float ratio = barrierMax > 0f ? Mathf.Clamp01(barrierCurrent / barrierMax) : 0f;
        barricadeRenderer.enabled = barrierCurrent > 0.5f;
        barricadeRenderer.color = ratio > 0.55f ? Color.white : ratio > 0.2f
            ? new Color(1f, 0.69f, 0.31f, 0.95f) : new Color(1f, 0.34f, 0.25f, 0.85f);
    }

    void RefreshMineVisuals()
    {
        for (int i = 0; i < mineVisuals.Count; i++) if (mineVisuals[i] != null) Destroy(mineVisuals[i]);
        mineVisuals.Clear();
        float groundY = GameTuning.Current.HeroFeetY(0);
        for (int i = 0; i < Mathf.Min(mineCharges, 6); i++)
        {
            var go = new GameObject("Refuge_Mine_" + i);
            go.transform.position = new Vector3(GeneratorX + 1.35f + i * 0.38f, groundY + 0.03f, -0.03f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetMineSprite();
            sr.sortingOrder = 19;
            go.transform.localScale = Vector3.one * 0.60f;
            mineVisuals.Add(go);
        }
    }

    void BuildHud()
    {
        hudCanvas = UIFactory.MakeCanvas("RefugeDefenseHUD", 88);
        var panel = UIKit.Frame(hudCanvas.transform, "RefugeStatusPanel", new Color(0.018f, 0.035f, 0.045f, 0.95f),
            new Color(0.82f, 0.34f, 0.13f, 0.95f), 2f);
        UIFactory.Place(panel, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(900f, 112f));

        waveText = UIFactory.Txt(panel, "ONDA 01 / 05", 24, TextAlignmentOptions.Center,
            new Color(1f, 0.72f, 0.32f), "Wave");
        UIFactory.Place(waveText.rectTransform, new Vector2(0f, 0.5f), new Vector2(120f, 16f), new Vector2(230f, 34f));
        waveText.fontStyle = FontStyles.Bold;

        generatorText = UIFactory.Txt(panel, "GERADOR  100%", 22, TextAlignmentOptions.Center,
            Color.white, "GeneratorIntegrity");
        UIFactory.Place(generatorText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 16f), new Vector2(250f, 34f));
        generatorText.fontStyle = FontStyles.Bold;

        barrierText = UIFactory.Txt(panel, "BARREIRA  95", 17, TextAlignmentOptions.Center,
            new Color(0.63f, 0.82f, 0.89f), "BarrierIntegrity");
        UIFactory.Place(barrierText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(230f, 26f));

        killsText = UIFactory.Txt(panel, "0 ABATES  ·  0 CHEFES", 17, TextAlignmentOptions.Center,
            new Color(0.73f, 0.82f, 0.85f), "Kills");
        UIFactory.Place(killsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-120f, 16f), new Vector2(230f, 34f));

        suppliesText = UIFactory.Txt(panel, "65 SUPRIMENTOS", 18, TextAlignmentOptions.Center,
            new Color(1f, 0.80f, 0.38f), "Supplies");
        UIFactory.Place(suppliesText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-120f, -21f), new Vector2(230f, 28f));
        suppliesText.fontStyle = FontStyles.Bold;

        statusText = UIFactory.Txt(hudCanvas.transform, "PROTEJA O GERADOR", 17, TextAlignmentOptions.Center,
            new Color(0.86f, 0.91f, 0.92f), "Status");
        UIFactory.Place(statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -139f), new Vector2(900f, 30f));

        // Melhorias em coluna fixa a direita: nunca cobrem os cartões dos sobreviventes.
        var buildPanel = UIKit.Frame(hudCanvas.transform, "DefenseBuildPanel", new Color(0.018f, 0.035f, 0.045f, 0.96f),
            new Color(0.82f, 0.34f, 0.13f, 0.92f), 1.6f);
        UIFactory.Place(buildPanel, new Vector2(1f, 0f), new Vector2(-16f, 150f), new Vector2(340f, 350f));

        var title = UIFactory.Txt(buildPanel, "MELHORIAS DA DEFESA", 19, TextAlignmentOptions.Center,
            new Color(1f, 0.76f, 0.36f), "DefenseBuildTitle");
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(312f, 32f));
        title.fontStyle = FontStyles.Bold;

        turretButton = UIFactory.Btn(buildPanel, "CONSTRUIR TORRETA  ·  25", 15, BuildTurret,
            new Color(0.15f, 0.25f, 0.29f), "BuildTurret");
        UIFactory.Place((RectTransform)turretButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(308f, 58f));
        barricadeButton = UIFactory.Btn(buildPanel, "REFORÇAR BARREIRA  ·  20", 15, ReinforceBarricade,
            new Color(0.15f, 0.25f, 0.29f), "ReinforceBarricade");
        UIFactory.Place((RectTransform)barricadeButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(308f, 58f));
        mineButton = UIFactory.Btn(buildPanel, "INSTALAR MINAS  ·  15", 15, DeployMines,
            new Color(0.15f, 0.25f, 0.29f), "DeployMines");
        UIFactory.Place((RectTransform)mineButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(308f, 58f));
        repairButton = UIFactory.Btn(buildPanel, "REPARAR GERADOR  ·  20", 15, RepairGenerator,
            new Color(0.48f, 0.18f, 0.10f), "RepairGenerator");
        UIFactory.Place((RectTransform)repairButton.transform, new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(308f, 58f));

        AddDefenseButtonIcon(turretButton, "Art/Refuge/turret");
        AddDefenseButtonIcon(barricadeButton, "Art/Refuge/barricade");
        AddDefenseButtonIcon(mineButton, "Art/Refuge/mine");
        AddDefenseButtonIcon(repairButton, "Art/Refuge/generator");
    
        if (hudCanvas != null) UILayoutPrefabApplier.Apply(hudCanvas.transform as RectTransform, "refugeDefenseHud");
    }

    void RefreshHud()
    {
        if (waveText == null) return;
        int currentWave = Mathf.Min(TotalWaves, WavesCompleted + 1);
        waveText.text = "ONDA " + currentWave.ToString("00") + " / " + TotalWaves.ToString("00");
        float ratio = GeneratorIntegrity;
        generatorText.text = "GERADOR  " + Mathf.RoundToInt(ratio * 100f) + "%";
        generatorText.color = ratio > 0.55f ? Color.white : ratio > 0.25f
            ? new Color(1f, 0.72f, 0.32f) : new Color(1f, 0.36f, 0.28f);
        barrierText.text = "BARREIRA  " + Mathf.CeilToInt(barrierCurrent) + " / " + Mathf.CeilToInt(barrierMax);
        killsText.text = Kills.ToString("N0") + " ABATES  ·  " + BossesDefeated + " CHEFES";
        suppliesText.text = FieldSupplies.ToString("N0") + " SUPRIMENTOS";

        if (turretButton != null)
        {
            turretButton.interactable = FieldSupplies >= 25 && turrets.Count < 3;
            if (turrets.Count >= 3) turretButton.GetComponentInChildren<TMP_Text>().text = "LIMITE DE TORRETAS";
        }
        if (barricadeButton != null) barricadeButton.interactable = FieldSupplies >= 20;
        if (mineButton != null) mineButton.interactable = FieldSupplies >= 15;
        if (repairButton != null) repairButton.interactable = FieldSupplies >= 20 && generatorHealth != null && generatorHealth.Current < generatorHealth.max - 0.1f;
    }

    void SetStatus(string value)
    {
        if (statusText != null) statusText.text = value;
    }

    Sprite GetGeneratorSprite()
    {
        if (generatorSprite == null) generatorSprite = ArtLoader.Get("Art/Refuge/generator");
        if (generatorSprite == null) generatorSprite = BuildPixelSprite(false);
        return generatorSprite;
    }
    Sprite GetTurretSprite()
    {
        if (turretSprite == null) turretSprite = ArtLoader.Get("Art/Refuge/turret");
        if (turretSprite == null) turretSprite = BuildPixelSprite(true);
        return turretSprite;
    }
    Sprite GetBarricadeSprite()
    {
        if (barricadeSprite == null) barricadeSprite = ArtLoader.Get("Art/Refuge/barricade");
        if (barricadeSprite == null) barricadeSprite = BuildBarricadePixelSprite();
        return barricadeSprite;
    }
    Sprite GetMineSprite()
    {
        if (mineSprite == null) mineSprite = ArtLoader.Get("Art/Refuge/mine");
        if (mineSprite == null) mineSprite = BuildMinePixelSprite();
        return mineSprite;
    }

    void AddDefenseButtonIcon(Button button, string resourcePath)
    {
        if (button == null) return;
        var sprite = ArtLoader.Get(resourcePath);
        if (sprite == null) return;
        var icon = UIFactory.Img(button.transform, Color.white, "FacilityIcon");
        icon.sprite = sprite;icon.preserveAspect=true;icon.raycastTarget=false;
        UIFactory.Place(icon.rectTransform,new Vector2(0f,0.5f),new Vector2(10f,0f),new Vector2(38f,38f));
        icon.transform.SetAsFirstSibling();
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.Center;
            label.margin = new Vector4(35f,0f,6f,0f);
            label.enableAutoSizing = true;label.fontSizeMin=12f;label.fontSizeMax=16f;
        }
    }

    static Sprite BuildPixelSprite(bool turret)
    {
        int w = turret ? 32 : 64, h = turret ? 32 : 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = turret ? "Zombit_RefugeTurret" : "Zombit_RefugeGenerator",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        Color dark = new Color(0.055f, 0.075f, 0.08f, 1f);
        Color steel = new Color(0.30f, 0.39f, 0.42f, 1f);
        Color light = new Color(0.58f, 0.68f, 0.69f, 1f);
        Color orange = new Color(0.96f, 0.36f, 0.12f, 1f);
        Color glow = new Color(1f, 0.75f, 0.28f, 1f);
        if (!turret)
        {
            Rect(px, w, h, 7, 4, 57, 47, dark);
            Rect(px, w, h, 10, 7, 54, 44, steel);
            Rect(px, w, h, 15, 14, 49, 37, dark);
            Rect(px, w, h, 18, 17, 46, 34, light);
            Rect(px, w, h, 23, 20, 41, 31, orange);
            Rect(px, w, h, 28, 23, 36, 28, glow);
            Rect(px, w, h, 25, 47, 39, 53, dark);
            Rect(px, w, h, 30, 53, 34, 62, steel);
            Rect(px, w, h, 2, 2, 62, 5, dark);
            Rect(px, w, h, 4, 0, 60, 2, dark);
        }
        else
        {
            Rect(px, w, h, 4, 4, 28, 13, dark);
            Rect(px, w, h, 7, 7, 25, 12, steel);
            Rect(px, w, h, 10, 13, 22, 24, light);
            Rect(px, w, h, 8, 22, 24, 26, dark);
            Rect(px, w, h, 13, 24, 19, 31, steel);
            Rect(px, w, h, 18, 26, 31, 29, dark);
            Rect(px, w, h, 27, 25, 31, 30, orange);
            Rect(px, w, h, 1, 2, 30, 5, dark);
        }
        tex.SetPixels(px); tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
        sprite.name = tex.name + "_Sprite"; sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    static Sprite BuildBarricadePixelSprite()
    {
        const int w = 48, h = 48;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { name = "Zombit_RefugeBarricade", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color[w * h]; for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        Color dark = new Color(0.08f, 0.07f, 0.06f, 1f), wood = new Color(0.43f, 0.27f, 0.16f, 1f), light = new Color(0.67f, 0.43f, 0.23f, 1f);
        Rect(px, w, h, 4, 2, 44, 40, dark); Rect(px, w, h, 7, 5, 41, 37, wood);
        Rect(px, w, h, 7, 15, 41, 19, light); Rect(px, w, h, 7, 27, 41, 31, light);
        Rect(px, w, h, 13, 0, 17, 44, dark); Rect(px, w, h, 31, 0, 35, 44, dark);
        tex.SetPixels(px); tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
        sprite.name = "Zombit_RefugeBarricade_Sprite"; sprite.hideFlags = HideFlags.HideAndDontSave; return sprite;
    }

    static Sprite BuildMinePixelSprite()
    {
        const int w = 16, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { name = "Zombit_RefugeMine", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color[w * h]; for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        Color dark = new Color(0.06f, 0.07f, 0.07f, 1f), steel = new Color(0.38f, 0.43f, 0.44f, 1f), orange = new Color(1f, 0.40f, 0.12f, 1f);
        Rect(px, w, h, 2, 3, 14, 12, dark); Rect(px, w, h, 4, 5, 12, 10, steel); Rect(px, w, h, 6, 7, 10, 9, orange);
        tex.SetPixels(px); tex.Apply(false, true);
        var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 32f);
        sprite.name = "Zombit_RefugeMine_Sprite"; sprite.hideFlags = HideFlags.HideAndDontSave; return sprite;
    }

    static void Rect(Color[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color color)
    {
        x0 = Mathf.Clamp(x0, 0, width); x1 = Mathf.Clamp(x1, 0, width);
        y0 = Mathf.Clamp(y0, 0, height); y1 = Mathf.Clamp(y1, 0, height);
        for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++) pixels[y * width + x] = color;
    }
}
