using UnityEngine;

/// <summary>
/// Cuspe do vomitado: globo lento e visivel. Nao persegue o heroi, para o jogador poder
/// focar o cuspidor (ou arrastar a formacao) antes do impacto.
/// </summary>
public sealed class SpitProjectile : MonoBehaviour
{
    const float Speed = 5.2f;
    const float HitRadius = 0.72f;
    const float ArcHeight = 0.55f;
    const float MaxLife = 2.2f;

    Vector3 start;
    Vector3 end;
    float damage;
    Health intended;
    float age;
    float flight;
    SpriteRenderer sr;
    TrailRenderer trail;

    static Sprite globSprite;
    static Material trailMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        globSprite = null;
        trailMaterial = null;
    }

    public static void Spawn(Vector3 from, Vector3 to, float dmg, Health victim)
    {
        var go = new GameObject("SpitGlob");
        go.transform.position = from;
        var spit = go.AddComponent<SpitProjectile>();
        spit.start = from;
        spit.end = to;
        spit.damage = dmg;
        spit.intended = victim;
        float dist = Mathf.Max(0.4f, Vector3.Distance(from, to));
        spit.flight = Mathf.Clamp(dist / Speed, 0.35f, MaxLife);
    }

    void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = GlobSprite();
        sr.color = new Color(0.55f, 1f, 0.32f, 0.95f);
        sr.sortingOrder = 21000;
        transform.localScale = Vector3.one * 0.28f;

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.18f;
        trail.minVertexDistance = 0.04f;
        trail.startWidth = 0.11f;
        trail.endWidth = 0.02f;
        trail.numCapVertices = 0;
        trail.autodestruct = false;
        trail.alignment = LineAlignment.View;
        trail.sortingOrder = 20990;
        trail.sharedMaterial = TrailMat();
        trail.startColor = new Color(0.45f, 0.95f, 0.28f, 0.7f);
        trail.endColor = new Color(0.2f, 0.45f, 0.08f, 0f);
        trail.Clear();
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / flight);
        Vector3 p = Vector3.Lerp(start, end, t);
        p.y += Mathf.Sin(t * Mathf.PI) * ArcHeight;
        transform.position = p;
        transform.Rotate(0f, 0f, -280f * Time.deltaTime);
        float pulse = 0.24f + 0.08f * Mathf.Sin(Time.time * 14f);
        transform.localScale = Vector3.one * pulse;

        if (t < 1f && age < MaxLife) return;

        TryHit();
        Destroy(gameObject);
    }

    void TryHit()
    {
        Vector3 impact = end;
        if (intended != null && intended.Current > 0f)
        {
            Vector3 chest = intended.transform.position + Vector3.up * 0.95f;
            chest.z = 0f;
            impact.z = 0f;
            if ((chest - impact).sqrMagnitude <= HitRadius * HitRadius)
            {
                intended.Take(damage);
                PixelCombatVFX.SpitImpact(chest);
                return;
            }
        }
        PixelCombatVFX.SpitImpact(transform.position);
    }

    static Sprite GlobSprite()
    {
        if (globSprite != null) return globSprite;
        const int n = 10;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            name = "Zombit_SpitGlob", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        var px = new Color[n * n];
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x - c) / c, dy = (y - c) / c;
            float d = Mathf.Sqrt(dx * dx + dy * dy * 1.15f);
            px[y * n + x] = d < 0.92f ? Color.white : Color.clear;
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        globSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        globSprite.hideFlags = HideFlags.HideAndDontSave;
        return globSprite;
    }

    static Material TrailMat()
    {
        if (trailMaterial != null) return trailMaterial;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
            trailMaterial = new Material(shader) { name = "Zombit_SpitTrail", hideFlags = HideFlags.HideAndDontSave };
        return trailMaterial;
    }
}
