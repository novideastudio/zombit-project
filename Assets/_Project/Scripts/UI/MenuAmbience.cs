using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ambiente do menu: o fundo permanece imovel e sem particulas de reflexo/folhas.
/// Somente uma luz suave e poucas brasas ficam animadas junto a fogueira.
/// </summary>
public sealed class MenuAmbience : MonoBehaviour
{
    public Vector2 fire = new Vector2(0.50f, 0.34f);
    [Range(0f, 0.05f)] public float breathZoom = 0f; // mantido por compatibilidade; nunca escala a arte.
    [Range(0f, 1f)] public float glowStrength = 0.20f;
    [Range(0, 20)] public int emberCount = 8;

    sealed class Ember
    {
        public RectTransform rt;
        public Image img;
        public float age, life, speed, drift, phase, x0;
    }

    RectTransform art;
    RectTransform glowRt;
    Image glowImg;
    Texture2D tex;
    Sprite glowSprite;
    readonly List<Ember> embers = new List<Ember>();

    public static MenuAmbience Attach(RectTransform artRect) => Attach(artRect, new Vector2(0.50f, 0.34f));

    public static MenuAmbience Attach(RectTransform artRect, Vector2 firePosition)
    {
        if (artRect == null) return null;
        var ambience = artRect.gameObject.AddComponent<MenuAmbience>();
        ambience.fire = new Vector2(Mathf.Clamp01(firePosition.x), Mathf.Clamp01(firePosition.y));
        ambience.Build(artRect);
        return ambience;
    }

    void Build(RectTransform artRect)
    {
        art = artRect;
        const int size = 64;
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[size * size];
        float mid = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - mid) / mid;
            float dy = (y - mid) / mid;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - distance), 1.8f));
        }
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        glowSprite.hideFlags = HideFlags.HideAndDontSave;

        glowImg = NewImage("CampfireGlow", new Color(1f, 0.48f, 0.12f, glowStrength));
        glowRt = glowImg.rectTransform;
        glowRt.anchorMin = glowRt.anchorMax = fire;
        glowRt.sizeDelta = new Vector2(470f, 320f);

        // Brasas pequenas e localizadas: nenhum movimento na imagem, na agua ou nas arvores.
        for (int i = 0; i < emberCount; i++)
        {
            var img = NewImage("CampfireEmber", Color.clear);
            var ember = new Ember { rt = img.rectTransform, img = img };
            float particleSize = Random.Range(3.5f, 6.5f);
            ember.rt.sizeDelta = new Vector2(particleSize, particleSize);
            Respawn(ember, true);
            embers.Add(ember);
        }
    }

    Image NewImage(string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(art, false);
        var image = go.AddComponent<Image>();
        image.sprite = glowSprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    void Respawn(Ember ember, bool initial)
    {
        ember.life = Random.Range(1.8f, 3.2f);
        ember.age = initial ? Random.Range(0f, ember.life) : 0f;
        ember.speed = Random.Range(0.045f, 0.085f);
        ember.drift = Random.Range(-0.009f, 0.009f);
        ember.phase = Random.Range(0f, 6.28f);
        ember.x0 = fire.x + Random.Range(-0.018f, 0.018f);
    }

    void Update()
    {
        // A escala da arte nunca e alterada. Somente o halo local da fogueira pulsa.
        float time = Time.unscaledTime;
        if (glowImg != null)
        {
            float flicker = Mathf.PerlinNoise(time * 3.1f, 11.3f) * 0.6f
                          + Mathf.PerlinNoise(time * 6.4f, 3.7f) * 0.4f;
            var color = glowImg.color;
            color.a = glowStrength * (0.82f + 0.28f * flicker);
            glowImg.color = color;
            float scale = 0.98f + 0.035f * flicker;
            glowRt.localScale = new Vector3(scale, scale, 1f);
        }

        float delta = Time.unscaledDeltaTime;
        for (int i = 0; i < embers.Count; i++)
        {
            var ember = embers[i];
            ember.age += delta;
            if (ember.age >= ember.life) Respawn(ember, false);
            float progress = ember.age / ember.life;
            float x = ember.x0 + ember.drift * ember.age + Mathf.Sin(time * 2.4f + ember.phase) * 0.002f;
            float y = fire.y - 0.012f + ember.speed * ember.age;
            ember.rt.anchorMin = ember.rt.anchorMax = new Vector2(x, y);
            ember.rt.anchoredPosition = Vector2.zero;
            float alpha = Mathf.Clamp01(Mathf.Min(progress * 5f, (1f - progress) * 2f));
            var color = new Color(1f, Mathf.Lerp(0.68f, 0.30f, progress), 0.08f, alpha * 0.72f);
            ember.img.color = color;
        }
    }

    void OnDestroy()
    {
        if (glowSprite != null) Destroy(glowSprite);
        if (tex != null) Destroy(tex);
    }
}
