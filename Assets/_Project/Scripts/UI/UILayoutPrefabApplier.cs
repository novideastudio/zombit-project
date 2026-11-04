using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout overlay only; gameplay, callbacks and dynamic data remain in runtime code.
public static class UILayoutPrefabApplier
{
    public const string ScreenResourcesPrefix = UIResourcePaths.ScreenPrefix;
    public static bool SuppressApply { get; set; }

    public struct ApplyReport
    {
        public string screenKey;
        public int matchedNodes;
        public int nodesWithOverrides;
        public int runtimeCreatedNodes;
        public int unmatchedNodes;
        public int skippedTemplateNodes;
        public string Summary => "Screen_" + screenKey + ": " + matchedNodes + " elementos correspondidos, " +
            nodesWithOverrides + " com overrides, " + runtimeCreatedNodes + " novos elementos, " +
            unmatchedNodes + " sem correspondencia e " + skippedTemplateNodes + " itens dinamicos ignorados.";
    }

    static ApplyReport lastReport;
    static ApplyReport workingReport;
    public static ApplyReport LastReport => lastReport;
    public static string GetLastApplyReport() => lastReport.Summary;

    public static bool HasLayout(string key)
    {
        return !SuppressApply && !string.IsNullOrEmpty(key) && UITheme.Current.useEditorUI &&
            Resources.Load<GameObject>(ScreenResourcesPrefix + key) != null;
    }

    /// <summary>
    /// Recalcula os LayoutGroups/Fitters de uma tela após aplicar overrides visuais.
    /// Pode ser chamado tanto no Editor quanto em Play Mode; não instancia serviços
    /// de runtime e não altera a lógica dos controladores.
    /// </summary>
    public static void ReapplyFits(RectTransform root)
    {
        if (root == null) return;

        Canvas.ForceUpdateCanvases();

        // Rebuild de baixo para cima: primeiro os elementos internos, depois os pais.
        var rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = rects.Length - 1; i >= 0; i--)
        {
            var rect = rects[i];
            if (rect == null) continue;

            var layoutGroup = rect.GetComponent<LayoutGroup>();
            var contentFitter = rect.GetComponent<ContentSizeFitter>();
            var aspectFitter = rect.GetComponent<AspectRatioFitter>();
            if (layoutGroup != null || contentFitter != null || aspectFitter != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        Canvas.ForceUpdateCanvases();
    }

    // Sobrecargas para chamadas existentes que possuam uma referência mais genérica.
    public static void ReapplyFits(Transform root)
    {
        ReapplyFits(root as RectTransform);
    }

    public static void ReapplyFits(GameObject root)
    {
        ReapplyFits(root != null ? root.transform as RectTransform : null);
    }

    // Compatibilidade quando o chamador possui uma referencia Component generica.
    // Evita CS0117/CS1503 em controladores e ferramentas que nao armazenam RectTransform.
    public static void ReapplyFits(Component root)
    {
        ReapplyFits(root != null ? root.transform as RectTransform : null);
    }

    public static void ReapplyFits()
    {
        Canvas.ForceUpdateCanvases();
    }

    public static bool Apply(RectTransform runtimeScreen, string key)
    {
        if (SuppressApply || runtimeScreen == null || string.IsNullOrEmpty(key) || !UITheme.Current.useEditorUI)
            return false;
        var prefab = Resources.Load<GameObject>(ScreenResourcesPrefix + key);
        if (prefab == null) return false;
        if (key == "map" && (EditorUiBinding.FindDeep(prefab.transform, "PcCampaignDetails") == null || EditorUiBinding.FindDeep(prefab.transform, "MapArea") == null))
        {
            Debug.LogWarning("[ZomBit UI] Screen_map desatualizado; usando layout do codigo.");
            return false;
        }
        workingReport = new ApplyReport { screenKey = key };
        ApplyNode(prefab.transform, runtimeScreen, true, key);
        // Garante que LayoutGroups/Fitters reflitam imediatamente as medidas aplicadas pelo prefab.
        ReapplyFits(runtimeScreen);
        lastReport = workingReport;
        if (UITheme.Current.logLayoutApplyDiagnostics)
            Debug.Log("[ZomBit UI] " + lastReport.Summary);
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
        workingReport.matchedNodes++;
        if (flags != null)
        {
            workingReport.nodesWithOverrides++;
            if (!isRoot) ApplyRectTransform(source as RectTransform, target as RectTransform, flags);
            if (flags.overrideActiveState && target.gameObject.activeSelf != source.gameObject.activeSelf)
                target.gameObject.SetActive(source.gameObject.activeSelf);
            if (!isRoot && flags.overrideSiblingOrder && target.parent != null)
                target.SetSiblingIndex(Mathf.Clamp(source.GetSiblingIndex(), 0, Mathf.Max(0, target.parent.childCount - 1)));
            ApplyImage(source.GetComponent<Image>(), target.GetComponent<Image>(), flags);
            ApplyText(source.GetComponent<TMP_Text>(), target.GetComponent<TMP_Text>(), flags);
            ApplyButton(source.GetComponent<Button>(), target.GetComponent<Button>(), flags);
            if (flags.overrideLayoutComponents) ApplyLayoutComponents(source, target);
        }
        for (int i = 0; i < source.childCount; i++)
        {
            var child = source.GetChild(i);
            if (OwnedByTemplate(key, child.name)) { workingReport.skippedTemplateNodes++; continue; }
            int occurrence = 0;
            for (int j = 0; j < i; j++)
                if (source.GetChild(j).name == child.name) occurrence++;
            var match = FindChildOccurrence(target, child.name, occurrence);
            if (match != null)
            {
                ApplyNode(child, match, false, key);
            }
            else
            {
                // Elementos criados no Editor Visual podem ser adicionados a layouts dinamicos.
                // Quando marcados explicitamente, eles passam a existir no runtime mesmo sem
                // um objeto equivalente gerado pelo codigo. O clone preserva filhos, imagens e TMP.
                var createFlag = child.GetComponent<UIPrefabLayoutOverride>();
                if (createFlag != null && createFlag.runtimeCreateIfMissing)
                {
                    var created = Object.Instantiate(child.gameObject, target, false);
                    created.name = child.name;
                    workingReport.runtimeCreatedNodes++;
                }
                else workingReport.unmatchedNodes++;
            }
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
