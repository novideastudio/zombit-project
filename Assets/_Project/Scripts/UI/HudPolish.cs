using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime HUD polish for missions: metal frames over existing plates and top-right order (backpack before BACK).
/// It only changes visuals/positions and is safe to run repeatedly after HUD rebuilds.
/// </summary>
public sealed class HudPolish : MonoBehaviour
{
    const string FrameName = "PcHudFrame";
    static Sprite frameSprite;
    static bool frameLoadAttempted;

    HudController hud;
    Canvas hudCanvas;
    RectTransform voltar;
    float nextRun;
    readonly Vector3[] voltarCorners = new Vector3[4];
    readonly Vector3[] backpackCorners = new Vector3[4];

    void Update()
    {
        if (GameSession.IsRange) { Destroy(this); return; }
        if (Time.unscaledTime < nextRun) return;
        nextRun = Time.unscaledTime + 0.5f;

        if (hud == null)
        {
            hud = FindFirstObjectByType<HudController>();
            if (hud == null) return;
            hudCanvas = hud.GetComponentInParent<Canvas>();
        }
        ApplyFrames();
        ArrangeTopRight();
    }

    static Sprite Frame()
    {
        if (frameSprite != null) return frameSprite;
        if (frameLoadAttempted) return null;
        frameLoadAttempted = true;
        var tex = Resources.Load<Texture2D>("Art/PCHudUI/hud_frame");
        if (tex == null) return null;
        frameSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        return frameSprite;
    }

    void ApplyFrames()
    {
        if (Frame() == null) return;
        EnsureFrame(hud.transform.Find("HudPlate_Wave") as RectTransform);
        EnsureFrame(hud.transform.Find("HudPlate_Backpack") as RectTransform);
        for (int i = 0; i < 3; i++) EnsureFrame(hud.transform.Find("HudSquadCard_" + i) as RectTransform);
        var posture = GameObject.Find("CombatPostureToggle");
        if (posture != null) EnsureFrame(posture.transform as RectTransform);
        EnsureFrame(FindBackButton());
    }

    static void EnsureFrame(RectTransform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;
        var existing = target.Find(FrameName);
        if (existing != null)
        {
            if (existing.GetSiblingIndex() != target.childCount - 1) existing.SetAsLastSibling();
            return;
        }
        var go = new GameObject(FrameName, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(target, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-2f, -2f);
        rt.offsetMax = new Vector2(2f, 2f);
        var image = go.AddComponent<Image>();
        image.sprite = Frame();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
    }

    RectTransform FindBackButton()
    {
        if (voltar != null) return voltar;
        if (hudCanvas == null) return null;
        foreach (var text in hudCanvas.GetComponentsInChildren<TMP_Text>(false))
        {
            if (text == null || string.IsNullOrEmpty(text.text)) continue;
            if (text.text.ToUpperInvariant().IndexOf("VOLTAR", System.StringComparison.Ordinal) < 0) continue;
            Transform t = text.transform;
            while (t.parent != null && t.parent != hud.transform && t.parent != hudCanvas.transform) t = t.parent;
            voltar = t as RectTransform;
            if (voltar != null) return voltar;
        }
        return null;
    }

    void ArrangeTopRight()
    {
        var backpack = hud.transform.Find("HudPlate_Backpack") as RectTransform;
        var back = FindBackButton();
        if (backpack == null || back == null || backpack == back) return;
        if (!backpack.gameObject.activeInHierarchy || !back.gameObject.activeInHierarchy) return;

        backpack.GetWorldCorners(backpackCorners);
        back.GetWorldCorners(voltarCorners);
        float backpackCenter = (backpackCorners[0].x + backpackCorners[2].x) * 0.5f;
        float backCenter = (voltarCorners[0].x + voltarCorners[2].x) * 0.5f;
        if (backpackCenter <= backCenter) return;   // already: backpack, then BACK

        float scale = hudCanvas != null ? Mathf.Max(0.01f, hudCanvas.scaleFactor) : 1f;
        float gap = 8f * scale;
        float backWidth = voltarCorners[2].x - voltarCorners[0].x;
        float newBackRight = backpackCorners[2].x;
        float newBackpackRight = newBackRight - backWidth - gap;
        back.anchoredPosition += new Vector2((newBackRight - voltarCorners[2].x) / scale, 0f);
        backpack.anchoredPosition += new Vector2((newBackpackRight - backpackCorners[2].x) / scale, 0f);
    }
}
