using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Destaque do sobrevivente principal no acampamento: halo suave de luz quente atras do corpo,
/// e uma pequena seta flutuante acima da cabeca (sem anel no chao).
/// Sem contorno poligonal e sem texto, para a arte continuar sendo o foco.
/// Desenhado por malha (sem textura), com pulsacao lenta.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
public sealed class SurvivorHighlight : MaskableGraphic
{
    // Margens (em pixels do stage) que o RectTransform tem alem da area do corpo.
    public const float PadX = 30f, PadTop = 42f, PadBottom = 26f;

    // ---- Controles de aura (ajustaveis no Inspector do Unity) ----
    [Header("Aura (halo atras do personagem)")]
    [Range(0f, 1f)] public float auraIntensity = 0.20f;
    [Range(0.2f, 1.5f)] public float auraSizeX = 0.62f;
    [Range(0.2f, 1.5f)] public float auraSizeY = 0.58f;
    [Range(0f, 1f)] public float auraPulseAmount = 0.15f;
    [Range(0f, 6f)] public float auraPulseSpeed = 2.2f;
    public Color auraColor = new Color(1f, 0.72f, 0.26f, 1f);
    [Header("Seta")]
    public bool showArrow = true;
    [Header("Etiqueta (Classe + Nivel + Poder)")]
    public bool showLabel = true;

    static readonly Color Warm = new Color(1f, 0.72f, 0.26f, 1f);
    const int Segments = 40;

    float bodyW = 200f, bodyH = 300f;
    float pulse = 1f, bob;

    public void SetBody(float width, float height) { bodyW = width; bodyH = height; SetVerticesDirty(); }

#if UNITY_EDITOR
    protected override void OnValidate() { base.OnValidate(); SetVerticesDirty(); }
#endif

    protected override void Awake() { base.Awake(); raycastTarget = false; maskable = false; }

    void Update()
    {
        float t = Time.unscaledTime;
        float amp = Mathf.Clamp01(auraPulseAmount);
        float p = (1f - amp) + amp * Mathf.Sin(t * auraPulseSpeed);
        float b = Mathf.Sin(t * 3.0f) * 3f;
        if (Mathf.Abs(p - pulse) > 0.004f || Mathf.Abs(b - bob) > 0.05f) { pulse = p; bob = b; SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0f || r.height <= 0f) return;

        // Coordenadas locais: (r.xMin, r.yMax) e o canto superior esquerdo.
        float cx = r.center.x;
        float bodyTop = r.yMax - PadTop;
        float bodyBottom = r.yMin + PadBottom;
        float bodyCy = (bodyTop + bodyBottom) * 0.5f;

        // 1) Aura quente atras do corpo (leque radial, centro mais forte).
        if (auraIntensity > 0.001f)
            Fan(vh, new Vector2(cx, bodyCy), bodyW * auraSizeX, bodyH * auraSizeY, Alpha(auraIntensity * pulse), Alpha(0f), 0);

        // 2) Seta flutuante acima da cabeca.
        if (!showArrow) return;
        float ay = bodyTop + 10f + bob;
        Color arrow = Alpha(0.95f);
        int s = vh.currentVertCount;
        vh.AddVert(new Vector3(cx - 11f, ay + 12f), arrow, Vector2.zero);
        vh.AddVert(new Vector3(cx + 11f, ay + 12f), arrow, Vector2.zero);
        vh.AddVert(new Vector3(cx, ay - 2f), arrow, Vector2.zero);
        vh.AddTriangle(s, s + 1, s + 2);
    }

    Color Alpha(float a) { Color c = auraColor; c.a = Mathf.Clamp01(a); return c; }

    static void Fan(VertexHelper vh, Vector2 c, float rx, float ry, Color inner, Color outer, float yOffset)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(c.x, c.y + yOffset), inner, Vector2.zero);
        for (int i = 0; i < Segments; i++)
        {
            float a = Mathf.PI * 2f * i / Segments;
            vh.AddVert(new Vector3(c.x + Mathf.Cos(a) * rx, c.y + yOffset + Mathf.Sin(a) * ry), outer, Vector2.zero);
        }
        for (int i = 0; i < Segments; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % Segments);
    }
}
