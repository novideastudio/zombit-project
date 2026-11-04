using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Controle de foco de disparo: clique em um zumbi para concentrar o fogo do esquadrão.</summary>
public sealed class ActiveSkills : MonoBehaviour
{
    public float focusPickRadius = 1.6f;

    Hero hero;
    Health heroHealth;
    MissionFlow flow;
    SpriteRenderer reticle;

    void Start()
    {
        hero = GetComponent<Hero>();
        heroHealth = GetComponent<Health>();
        var go = new GameObject("FocusReticle");
        reticle = go.AddComponent<SpriteRenderer>();
        reticle.sprite = MakeRing();
        reticle.color = new Color(1f, 0.25f, 0.2f, 0.95f);
        reticle.sortingOrder = 22500;
        go.SetActive(false);
    }

    void OnDestroy()
    {
        if (reticle != null) Destroy(reticle.gameObject);
    }

    void Update()
    {
        if (hero == null || Time.timeScale == 0f) return;
        if (heroHealth != null && heroHealth.Current <= 0f) return;
        if (flow == null) flow = FindFirstObjectByType<MissionFlow>();
        if (flow != null && flow.IsFinished) return;
        HandleFocusClick();
        UpdateReticle();
    }

    void HandleFocusClick()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 w = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        w.z = 0f;
        Enemy best = null;
        float bestD = focusPickRadius * focusPickRadius;
        foreach (var e in Enemy.Alive)
        {
            if (e == null || e.IsDead) continue;
            Vector3 a = e.AimPoint; a.z = 0f;
            float d = (a - w).sqrMagnitude;
            if (d < bestD) { bestD = d; best = e; }
        }
        hero.focusTarget = best; // null = solta o foco
        foreach (var m in HeroSquad.Members)
            if (m.hero != null && m.hero != hero) m.hero.focusTarget = best;
    }

    void UpdateReticle()
    {
        var target = hero.focusTarget;
        if (target == null || target.IsDead)
        {
            hero.focusTarget = null;
            reticle.gameObject.SetActive(false);
            return;
        }
        reticle.gameObject.SetActive(true);
        Vector3 p = target.AimPoint; p.z = 0f;
        reticle.transform.position = p;
        float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 9f);
        reticle.transform.localScale = Vector3.one * (target.IsBoss ? 1.5f : 0.8f) * pulse;
    }

    static Sprite ringSprite;
    static Sprite MakeRing()
    {
        if (ringSprite != null) return ringSprite;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            bool ring = d > 12f && d < 15.5f;
            bool cross = (Mathf.Abs(x - c) < 1.2f || Mathf.Abs(y - c) < 1.2f) && d > 7f && d < 15.5f && !(d > 9f && d < 12f);
            px[y * n + x] = (ring || cross) ? Color.white : Color.clear;
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        ringSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 24f);
        return ringSprite;
    }
}
