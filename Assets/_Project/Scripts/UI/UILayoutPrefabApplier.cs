using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout overlay only; gameplay, callbacks and dynamic data remain in runtime code.
public static class UILayoutPrefabApplier
{
    public const string ResourcesFolder = "UI/Screens/Layout_";
    public static bool SuppressApply { get; set; }

    public static bool HasLayout(string key)
    {
        if (key == "map" || key == "mapSkills") return false;
        return !SuppressApply && !string.IsNullOrEmpty(key) && UITheme.Current.useEditorUI &&
            Resources.Load<GameObject>(ResourcesFolder + key) != null;
    }

    public static bool Apply(RectTransform runtimeScreen, string key)
    {
        if (SuppressApply || runtimeScreen == null || string.IsNullOrEmpty(key) || !UITheme.Current.useEditorUI)
            return false;
        var prefab = Resources.Load<GameObject>(ResourcesFolder + key);
        if (prefab == null) return false;
        if (key == "map" && EditorUiBinding.FindDeep(prefab.transform, "HubProfile") == null)
        {
            Debug.LogWarning("[ZomBit UI] Layout_map desatualizado; usando layout do codigo.");
            return false;
        }
        ApplyNode(prefab.transform, runtimeScreen, true, key);
        return true;
    }

    static bool OwnedByTemplate(string key, string name)
    {
        return key == "map" && (name.StartsWith("Node_") || name.StartsWith("Enemy_") || name.StartsWith("EquipSlot_"));
    }

    static void ApplyNode(Transform source, Transform target, bool isRoot, string key)
    {
        if (source == null || target == null) return;
        var flags = source.GetComponent<UIPrefabLayoutOverride>();
        if (flags != null)
        {
            if (!isRoot) ApplyRectTransform(source as RectTransform, target as RectTransform, flags);
            ApplyImage(source.GetComponent<Image>(), target.GetComponent<Image>(), flags);
            ApplyText(source.GetComponent<TMP_Text>(), target.GetComponent<TMP_Text>(), flags);
            ApplyButton(source.GetComponent<Button>(), target.GetComponent<Button>(), flags);
            if (flags.overrideLayoutComponents) ApplyLayoutComponents(source, target);
        }
        for (int i = 0; i < source.childCount; i++)
        {
            var child = source.GetChild(i);
            if (OwnedByTemplate(key, child.name)) continue;
            int occurrence = 0;
            for (int j = 0; j < i; j++)
                if (source.GetChild(j).name == child.name) occurrence++;
            var match = FindChildOccurrence(target, child.name, occurrence);
            if (match != null) ApplyNode(child, match, false, key);
        }
    }

    static Transform FindChildOccurrence(Transform parent, string name, int occurrence)
    {
        if (parent == null) return null;
        int found = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name != name) continue;
            if (found++ == occurrence) return child;
        }
        return null;
    }

    static void ApplyRectTransform(RectTransform s, RectTransform t, UIPrefabLayoutOverride f)
    {
        if (s == null || t == null) return;
        if (f.overrideAnchorsAndPosition)
        {
            t.anchorMin = s.anchorMin;
            t.anchorMax = s.anchorMax;
            t.anchoredPosition3D = s.anchoredPosition3D;
        }
        if (f.overrideSize) t.sizeDelta = s.sizeDelta;
        if (f.overridePivot) t.pivot = s.pivot;
        if (f.overrideScaleAndRotation)
        {
            t.localScale = s.localScale;
            t.localRotation = s.localRotation;
        }
    }

    static void ApplyImage(Image s, Image t, UIPrefabLayoutOverride f)
    {
        if (s == null || t == null) return;
        if (f.overrideImageSprite && t.sprite == f.bakedImageSprite)
        {
            t.sprite = s.sprite;
            t.type = s.type;
            t.preserveAspect = s.preserveAspect;
            t.fillCenter = s.fillCenter;
            t.fillMethod = s.fillMethod;
            t.fillOrigin = s.fillOrigin;
            t.fillAmount = s.fillAmount;
            t.pixelsPerUnitMultiplier = s.pixelsPerUnitMultiplier;
            t.useSpriteMesh = s.useSpriteMesh;
        }
        if (f.overrideImageColor && t.color == f.bakedImageColor) t.color = s.color;
    }

    static void ApplyText(TMP_Text s, TMP_Text t, UIPrefabLayoutOverride f)
    {
        if (s == null || t == null) return;
        if (f.overrideTextContent && string.Equals(t.text, f.bakedTextContent, System.StringComparison.Ordinal))
            t.text = s.text;
        if (f.overrideTextColor && t.color == f.bakedTextColor) t.color = s.color;
        if (!f.overrideTextStyle) return;
        if (s.font != null) t.font = s.font;
        t.fontSize = s.fontSize;
        t.fontStyle = s.fontStyle;
        t.alignment = s.alignment;
        t.enableAutoSizing = s.enableAutoSizing;
        t.fontSizeMin = s.fontSizeMin;
        t.fontSizeMax = s.fontSizeMax;
        t.textWrappingMode = s.textWrappingMode;
        t.overflowMode = s.overflowMode;
        t.margin = s.margin;
        t.characterSpacing = s.characterSpacing;
        t.wordSpacing = s.wordSpacing;
        t.lineSpacing = s.lineSpacing;
        t.paragraphSpacing = s.paragraphSpacing;
        t.richText = s.richText;
        t.extraPadding = s.extraPadding;
        t.raycastTarget = s.raycastTarget;
        t.maskable = s.maskable;
    }

    static void ApplyButton(Button s, Button t, UIPrefabLayoutOverride f)
    {
        if (s == null || t == null) return;
        if (f.overrideButtonColors && ColorsMatch(t.colors, f.bakedButtonColors)) t.colors = s.colors;
        if (!f.overrideButtonTransition) return;
        t.transition = s.transition;
        t.spriteState = s.spriteState;
        t.navigation = s.navigation;
        if (s.animationTriggers != null && t.animationTriggers != null)
        {
            t.animationTriggers.normalTrigger = s.animationTriggers.normalTrigger;
            t.animationTriggers.highlightedTrigger = s.animationTriggers.highlightedTrigger;
            t.animationTriggers.pressedTrigger = s.animationTriggers.pressedTrigger;
            t.animationTriggers.selectedTrigger = s.animationTriggers.selectedTrigger;
            t.animationTriggers.disabledTrigger = s.animationTriggers.disabledTrigger;
        }
    }

    static bool ColorsMatch(ColorBlock a, ColorBlock b)
    {
        return a.normalColor == b.normalColor && a.highlightedColor == b.highlightedColor &&
            a.pressedColor == b.pressedColor && a.selectedColor == b.selectedColor &&
            a.disabledColor == b.disabledColor && Mathf.Approximately(a.colorMultiplier, b.colorMultiplier) &&
            Mathf.Approximately(a.fadeDuration, b.fadeDuration);
    }

    static void ApplyLayoutComponents(Transform s, Transform t)
    {
        var se = s.GetComponent<LayoutElement>();
        if (se != null)
        {
            var te = t.GetComponent<LayoutElement>();
            if (te == null) te = t.gameObject.AddComponent<LayoutElement>();
            te.ignoreLayout = se.ignoreLayout;
            te.minWidth = se.minWidth; te.minHeight = se.minHeight;
            te.preferredWidth = se.preferredWidth; te.preferredHeight = se.preferredHeight;
            te.flexibleWidth = se.flexibleWidth; te.flexibleHeight = se.flexibleHeight;
            te.layoutPriority = se.layoutPriority;
        }
        var sh = s.GetComponent<HorizontalLayoutGroup>();
        var th = t.GetComponent<HorizontalLayoutGroup>();
        if (sh != null)
        {
            if (th == null && t.GetComponent<VerticalLayoutGroup>() == null && t.GetComponent<GridLayoutGroup>() == null)
                th = t.gameObject.AddComponent<HorizontalLayoutGroup>();
            if (th != null) CopyHorizontalOrVertical(sh, th);
        }
        var sv = s.GetComponent<VerticalLayoutGroup>();
        var tv = t.GetComponent<VerticalLayoutGroup>();
        if (sv != null)
        {
            if (tv == null && t.GetComponent<HorizontalLayoutGroup>() == null && t.GetComponent<GridLayoutGroup>() == null)
                tv = t.gameObject.AddComponent<VerticalLayoutGroup>();
            if (tv != null) CopyHorizontalOrVertical(sv, tv);
        }
        var sg = s.GetComponent<GridLayoutGroup>();
        var tg = t.GetComponent<GridLayoutGroup>();
        if (sg != null)
        {
            if (tg == null && t.GetComponent<HorizontalLayoutGroup>() == null && t.GetComponent<VerticalLayoutGroup>() == null)
                tg = t.gameObject.AddComponent<GridLayoutGroup>();
            if (tg != null)
            {
                CopyLayoutGroup(sg, tg);
                tg.startCorner = sg.startCorner; tg.startAxis = sg.startAxis;
                tg.cellSize = sg.cellSize; tg.spacing = sg.spacing;
                tg.constraint = sg.constraint; tg.constraintCount = sg.constraintCount;
            }
        }
        var sf = s.GetComponent<ContentSizeFitter>();
        if (sf != null)
        {
            var tf = t.GetComponent<ContentSizeFitter>();
            if (tf == null) tf = t.gameObject.AddComponent<ContentSizeFitter>();
            tf.horizontalFit = sf.horizontalFit; tf.verticalFit = sf.verticalFit;
        }
        var sa = s.GetComponent<AspectRatioFitter>();
        if (sa != null)
        {
            var ta = t.GetComponent<AspectRatioFitter>();
            if (ta == null) ta = t.gameObject.AddComponent<AspectRatioFitter>();
            ta.aspectMode = sa.aspectMode; ta.aspectRatio = sa.aspectRatio;
        }
        var sc = s.GetComponent<CanvasGroup>();
        if (sc != null)
        {
            var tc = t.GetComponent<CanvasGroup>();
            if (tc == null) tc = t.gameObject.AddComponent<CanvasGroup>();
            tc.alpha = sc.alpha; tc.interactable = sc.interactable;
            tc.blocksRaycasts = sc.blocksRaycasts; tc.ignoreParentGroups = sc.ignoreParentGroups;
        }
    }

    static void CopyHorizontalOrVertical(HorizontalOrVerticalLayoutGroup s, HorizontalOrVerticalLayoutGroup t)
    {
        CopyLayoutGroup(s, t);
        t.spacing = s.spacing;
        t.childControlWidth = s.childControlWidth; t.childControlHeight = s.childControlHeight;
        t.childForceExpandWidth = s.childForceExpandWidth; t.childForceExpandHeight = s.childForceExpandHeight;
        t.reverseArrangement = s.reverseArrangement;
    }

    static void CopyLayoutGroup(LayoutGroup s, LayoutGroup t)
    {
        t.padding = new RectOffset(s.padding.left, s.padding.right, s.padding.top, s.padding.bottom);
        t.childAlignment = s.childAlignment;
    }
}
