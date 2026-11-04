using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Escala o conteudo de 1920x1080 para caber inteiro na tela (sem cortar).</summary>
public class ModesFitToScreen : MonoBehaviour
{
    RectTransform self, parent;
    void LateUpdate()
    {
        if (parent == null) { self = (RectTransform)transform; parent = self.parent as RectTransform; if (parent == null) return; }
        var r = parent.rect;
        if (r.width < 1f || r.height < 1f) return;
        float sc = Mathf.Min(r.width / 1920f, r.height / 1080f);
        self.localScale = new Vector3(sc, sc, 1f);
    }
}

/// <summary>Selecao dos modos especiais. Last Stand e a primeira atividade liberada.</summary>
public partial class MainMenuController
{
    static readonly Color ModeAccent = new Color(0.95f, 0.27f, 0.12f, 1f);
    readonly Dictionary<string, Button> lastStandLocationButtons = new Dictionary<string, Button>();
    string lastStandSelectedLocationId = "suburbio";
    int bossRushSelectedDifficulty = (int)Difficulty.Normal;
    int refugeDefenseSelectedDifficulty = (int)Difficulty.Normal;
    readonly Dictionary<int, Button> bossRushDifficultyButtons = new Dictionary<int, Button>();

    // ------------------------------------------------------------------
    // Tela "MODOS DE JOGO" (layout fiel a referencia, canvas 1920x1080).
    // Todas as posicoes abaixo sao medidas a partir do canto superior esquerdo.
    // ------------------------------------------------------------------
    static Sprite modesHazardSprite;
    static readonly Dictionary<string, Sprite> modesSpriteCache = new Dictionary<string, Sprite>();

    // Carrega como Sprite e, se o PNG estiver importado como textura comum, cria o Sprite em runtime.
    static Sprite ModesSprite(string path)
    {
        var sprite = ArtLoader.Get(path);
        if (sprite != null) return sprite;
        if (modesSpriteCache.TryGetValue(path, out var cached) && cached != null) return cached;
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) { Debug.LogWarning("Zombit: arte ausente em Resources/" + path); return null; }
        cached = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        cached.name = "Modes_" + System.IO.Path.GetFileName(path);
        modesSpriteCache[path] = cached;
        return cached;
    }

    static Sprite ModesHazardSprite()
    {
        if (modesHazardSprite != null) return modesHazardSprite;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var yellow = new Color(0.96f, 0.74f, 0.10f, 1f);
        var dark = new Color(0.07f, 0.07f, 0.06f, 1f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                tex.SetPixel(x, y, ((x + y) % n) < n / 2 ? yellow : dark);
        tex.Apply();
        modesHazardSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        return modesHazardSprite;
    }

    static RectTransform ModesPlaceTL(RectTransform rt, float x, float y, float w, float h)
        => UIFactory.Place(rt, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

    void BuildModesBackdrop(RectTransform screen)
    {
        // Para usar a ilustracao final do acampamento, coloque "modes_backdrop.png" em Resources/Art/PCReferenceUI.
        var sprite = ModesSprite("Art/PCReferenceUI/modes_backdrop");
        bool custom = sprite != null;
        if (sprite == null) sprite = ArtLoader.Get("Art/Backgrounds/wave_ruins");
        var art = UIFactory.Img(screen, custom ? Color.white : new Color(0.88f, 0.80f, 0.78f, 1f), "ModesBackdropArt");
        art.sprite = sprite; art.preserveAspect = false; art.raycastTarget = false;
        UIFactory.Stretch(art.rectTransform);
        art.transform.SetAsFirstSibling();
        var shade = UIFactory.Img(screen, new Color(0.01f, 0.01f, 0.02f, custom ? 0.10f : 0.38f), "ModesBackdropShade");
        UIFactory.Stretch(shade.rectTransform); shade.raycastTarget = false;
        shade.transform.SetSiblingIndex(1);
    }

    void BuildModesPill(Transform parent, string name, string iconName, string label, int value, float x, float w)
    {
        var pill = UIKit.Frame(parent, name, new Color(0.03f, 0.035f, 0.04f, 0.94f), new Color(0.62f, 0.42f, 0.20f, 0.95f), 2f);
        ModesPlaceTL(pill, x, 16f, w, 64f);
        var icon = ArtLoader.Get("Art/Skills/" + iconName);
        if (icon != null)
        {
            var img = UIFactory.Img(pill, new Color(1f, 0.76f, 0.30f, 1f), "Icon");
            img.sprite = icon; img.preserveAspect = true; img.raycastTarget = false;
            UIFactory.Place(img.rectTransform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(38f, 38f));
        }
        var txt = UIFactory.Txt(pill, "<size=18><color=#E8E0D0>" + label + "</color></size>   <size=30><color=#FFC24A>" + value.ToString("N0") + "</color></size>",
            24, TextAlignmentOptions.MidlineLeft, Color.white, "Value");
        UIFactory.Place(txt.rectTransform, new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(w - 70f, 56f));
        txt.fontStyle = FontStyles.Bold; txt.richText = true; txt.raycastTarget = false;
        txt.textWrappingMode = TextWrappingModes.NoWrap;
    }

    void ShowGameModes()
    {
        if (GameSession.Save == null) { Show("main"); return; }
        GameSession.Save.MigrateFromPreviousVersions();
        var screenRoot = Rebuild("gameModes");
        BuildModesBackdrop(screenRoot);
        // Conteudo desenhado em 1920x1080 e reduzido automaticamente para caber em qualquer tela.
        var s = UIFactory.Node("ModesContent", screenRoot);
        UIFactory.Place(s, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
        s.gameObject.AddComponent<ModesFitToScreen>();
        var save = GameSession.Save;

        // Saldos (canto superior direito).
        BuildModesPill(s, "ResistanceBalance", "icon_shield", "RESISTÊNCIA", save.resistanceTokens, 1213f, 248f);
        BuildModesPill(s, "BossBalance", "icon_skull", "CHEFES", save.bossTokens, 1471f, 198f);
        BuildModesPill(s, "DefenseBalance", "icon_gear", "DEFESA", save.defenseMaterials, 1679f, 225f);

        // Faixa do titulo com listras de aviso nas pontas.
        var banner = UIKit.Frame(s, "ModesTitleBanner", new Color(0.045f, 0.045f, 0.05f, 0.97f), new Color(0.90f, 0.62f, 0.26f, 1f), 3f);
        ModesPlaceTL(banner, 579f, 112f, 772f, 82f);
        foreach (int side in new[] { 0, 1 })
        {
            var stripe = UIFactory.Img(banner, Color.white, "Hazard" + side);
            stripe.sprite = ModesHazardSprite(); stripe.type = Image.Type.Tiled; stripe.raycastTarget = false;
            UIFactory.Place(stripe.rectTransform, new Vector2(side, 0.5f), new Vector2(side == 0 ? 14f : -14f, 0f), new Vector2(66f, 46f));
        }
        var title = UIFactory.Txt(banner, "MODOS DE JOGO", 56, TextAlignmentOptions.Center, new Color(0.97f, 0.96f, 0.94f), "ModesTitle");
        UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 70f));
        title.fontStyle = FontStyles.Bold; title.characterSpacing = 2f; title.raycastTarget = false;

        var subtitle = UIFactory.Txt(s, "Escolha como deseja lutar. Cada modo tem sua própria progressão e recompensas.",
            24, TextAlignmentOptions.Center, new Color(0.93f, 0.89f, 0.80f), "ModesSubtitle");
        UIFactory.Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -208f), new Vector2(1500f, 36f));
        subtitle.raycastTarget = false;

        BuildModeCard(s, "ÚLTIMA RESISTÊNCIA", "SOBREVIVÊNCIA SEM FIM",
            "Enfrente hordas cada vez maiores. Extraia para garantir suas Fichas de Resistência.",
            "Art/PCReferenceUI/mode_ultimate_resistance", "icon_skull",
            new[] { "icon_coins", "icon_gear", "icon_medcrate", "icon_chip" },
            new Color(0.98f, 0.56f, 0.10f, 1f), 175f, ShowLastStandSetup);
        BuildModeCard(s, "CORRIDA DOS CHEFES", "CAÇADA AOS MUTANTES",
            "Enfrente cinco chefes regionais em sequência. Ganhe Fichas de Chefe e prove a força da sua equipe.",
            "Art/PCReferenceUI/mode_boss_rush", "icon_star_up",
            new[] { "icon_skull", "icon_gear", "icon_explosive", "icon_medcrate" },
            new Color(0.92f, 0.14f, 0.12f, 1f), 705f, ShowBossRushSetup);
        BuildModeCard(s, "DEFESA DO REFÚGIO", "PROTEJA A BASE",
            "Construa defesas, repare o gerador e resista às invasões de zumbis.",
            "Art/PCReferenceUI/mode_refuge_defense", "icon_shield",
            new[] { "icon_star_up", "icon_medcrate", "icon_chip", "icon_bolt" },
            new Color(0.16f, 0.76f, 0.92f, 1f), 1235f, ShowRefugeDefenseSetup);

        // Botao "VOLTAR AO ACAMPAMENTO".
        var back = UIKit.Frame(s, "BackToCamp", new Color(0.045f, 0.045f, 0.05f, 0.97f), new Color(0.90f, 0.62f, 0.26f, 1f), 3f);
        ModesPlaceTL(back, 657f, 958f, 609f, 78f);
        var backFill = back.Find("Fill") != null ? back.Find("Fill").GetComponent<Image>() : null;
        if (backFill != null) backFill.raycastTarget = true;
        var backButton = back.gameObject.AddComponent<Button>();
        backButton.targetGraphic = backFill;
        backButton.onClick.AddListener(() => { ZombitAudio.PlayUI(); ShowHome(); });
        var tent = ArtLoader.Get("Art/Skills/icon_backpack");
        if (tent != null)
        {
            var ti = UIFactory.Img(back, new Color(1f, 0.74f, 0.28f, 1f), "CampIcon");
            ti.sprite = tent; ti.preserveAspect = true; ti.raycastTarget = false;
            UIFactory.Place(ti.rectTransform, new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(46f, 46f));
        }
        var backLabel = UIFactory.Txt(back, "VOLTAR AO ACAMPAMENTO", 32, TextAlignmentOptions.Center, new Color(0.97f, 0.95f, 0.90f), "BackLabel");
        UIFactory.Place(backLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 0f), new Vector2(470f, 60f));
        backLabel.fontStyle = FontStyles.Bold; backLabel.characterSpacing = 1.2f; backLabel.raycastTarget = false;
        backLabel.enableAutoSizing = true; backLabel.fontSizeMin = 22f; backLabel.fontSizeMax = 32f;

        Show("gameModes");
    }

    void BuildModeCard(Transform parent, string title, string kicker, string description,
        string artPath, string emblemIcon, string[] rewardIcons, Color accent, float x, System.Action action)
    {
        const float cardW = 510f, cardH = 620f, cardTop = 294f;
        var card = UIKit.Frame(parent, "ModeCard_" + title.Replace(" ", ""),
            new Color(0.020f, 0.032f, 0.045f, 0.97f), accent, 5f);
        ModesPlaceTL(card, x, cardTop, cardW, cardH);

        // Cantos decorativos.
        var gold = new Color(0.98f, 0.78f, 0.34f, 1f);
        Vector2[] corners = { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
        foreach (var c in corners)
        {
            var d = UIFactory.Img(card, gold, "Corner");
            d.raycastTarget = false;
            UIFactory.Place(d.rectTransform, c, Vector2.zero, new Vector2(16f, 16f));
            d.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        // Emblema acima do cartao.
        var emblem = UIFactory.Node("Emblem", card);
        UIFactory.Place(emblem, new Vector2(0.5f, 1f), new Vector2(0f, 58f), new Vector2(150f, 96f));
        foreach (int side in new[] { -1, 1 })
        {
            var wing = UIFactory.Img(emblem, new Color(0.92f, 0.70f, 0.26f, 1f), "Wing");
            wing.raycastTarget = false;
            UIFactory.Place(wing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(side * 52f, -2f), new Vector2(46f, 16f));
            wing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * -16f);
        }
        var ring = UIFactory.Img(emblem, gold, "EmblemRing");
        ring.raycastTarget = false;
        UIFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(66f, 66f));
        ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var ringIn = UIFactory.Img(emblem, new Color(0.05f, 0.05f, 0.07f, 1f), "EmblemFill");
        ringIn.raycastTarget = false;
        UIFactory.Place(ringIn.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f));
        ringIn.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var emblemSprite = ArtLoader.Get("Art/Skills/" + emblemIcon);
        if (emblemSprite != null)
        {
            var ei = UIFactory.Img(emblem, accent, "EmblemIcon");
            ei.sprite = emblemSprite; ei.preserveAspect = true; ei.raycastTarget = false;
            UIFactory.Place(ei.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
        }

        // Cabecalho.
        var header = UIFactory.Img(card, new Color(0.11f, 0.085f, 0.07f, 0.96f), "HeaderBand");
        header.raycastTarget = false;
        UIFactory.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(490f, 64f));
        var headerLine = UIFactory.Img(card, accent, "HeaderLine");
        headerLine.raycastTarget = false;
        UIFactory.Place(headerLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(490f, 2f));
        var head = UIFactory.Txt(card, title, 36, TextAlignmentOptions.Center, Color.white, "ModeTitle");
        UIFactory.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(474f, 56f));
        head.fontStyle = FontStyles.Bold; head.characterSpacing = 1f; head.raycastTarget = false;
        head.enableAutoSizing = true; head.fontSizeMin = 26f; head.fontSizeMax = 36f;

        // Arte do modo (1224x516, enquadrada exatamente na janela).
        var artSprite = ModesSprite(artPath);
        var art = UIFactory.Img(card, artSprite != null ? Color.white : new Color(0.1f, 0.12f, 0.14f, 1f), "ModeArtwork");
        art.sprite = artSprite; art.preserveAspect = false; art.raycastTarget = false;
        UIFactory.Place(art.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(486f, 205f));

        var sub = UIFactory.Txt(card, kicker, 25, TextAlignmentOptions.Center, accent, "ModeKicker");
        UIFactory.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -304f), new Vector2(480f, 34f));
        sub.fontStyle = FontStyles.Bold; sub.characterSpacing = 0.8f; sub.raycastTarget = false;

        var body = UIFactory.Txt(card, description, 21, TextAlignmentOptions.Top, new Color(0.88f, 0.88f, 0.90f), "ModeDescription");
        UIFactory.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -340f), new Vector2(440f, 82f));
        body.textWrappingMode = TextWrappingModes.Normal;
        body.enableAutoSizing = true; body.fontSizeMin = 16f; body.fontSizeMax = 21f; body.raycastTarget = false;

        // Recompensas.
        var rewards = UIKit.Frame(card, "ModeRewards", new Color(0.03f, 0.04f, 0.05f, 0.92f),
            new Color(accent.r * 0.75f, accent.g * 0.75f, accent.b * 0.75f, 0.85f), 1.6f);
        UIFactory.Place(rewards, new Vector2(0.5f, 1f), new Vector2(0f, -432f), new Vector2(470f, 84f));
        var rewardTitle = UIFactory.Txt(rewards, "RECOMPENSAS", 15, TextAlignmentOptions.Center, new Color(0.95f, 0.92f, 0.85f), "RewardsTitle");
        UIFactory.Place(rewardTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(400f, 22f));
        rewardTitle.fontStyle = FontStyles.Bold; rewardTitle.characterSpacing = 1.6f; rewardTitle.raycastTarget = false;
        int rewardCount = rewardIcons != null ? Mathf.Min(4, rewardIcons.Length) : 0;
        for (int i = 0; i < rewardCount; i++)
        {
            var sprite = ArtLoader.Get("Art/Skills/" + rewardIcons[i]);
            if (sprite == null) continue;
            var reward = UIFactory.Img(rewards, new Color(1f, 0.86f, 0.55f, 1f), "Reward_" + rewardIcons[i]);
            reward.sprite = sprite; reward.preserveAspect = true; reward.raycastTarget = false;
            UIFactory.Place(reward.rectTransform, new Vector2(0.5f, 0f), new Vector2((i - (rewardCount - 1) * 0.5f) * 100f, 8f), new Vector2(44f, 44f));
        }

        // Botao JOGAR.
        var play = UIFactory.Btn(card, "JOGAR", 30, action, new Color(0.20f, 0.52f, 0.12f, 1f), "ModeAction");
        UIFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(356f, 72f));
        var playLabel = play.GetComponentInChildren<TMP_Text>(true);
        if (playLabel) { playLabel.fontStyle = FontStyles.Bold; playLabel.characterSpacing = 1.6f; }
        var arrowSprite = ArtLoader.Get("Art/Skills/ui_arrow_down");
        if (arrowSprite != null)
        {
            var arrow = UIFactory.Img(play.transform, Color.white, "PlayArrow");
            arrow.sprite = arrowSprite; arrow.preserveAspect = true; arrow.raycastTarget = false;
            UIFactory.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70f, 0f), new Vector2(28f, 28f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            if (playLabel) playLabel.rectTransform.anchoredPosition += new Vector2(16f, 0f);
        }
    }

    void ShowLastStandSetup()
    {
        var save = GameSession.Save;
        if (save == null) { Show("main"); return; }
        save.NormalizeSquad();
        var screen = Rebuild("lastStandSetup");
        CreateUtilityBackdrop(screen, "LastStandSetupBackdrop");
        Title(screen, "ÚLTIMA RESISTÊNCIA", 58f);

        var info = UIFactory.Txt(screen, "ESCOLHA O CAMPO DE BATALHA · OS INIMIGOS FICAM MAIS FORTES A CADA 3 MINUTOS · CHEFES SURGEM A CADA 5 MINUTOS",
            20, TextAlignmentOptions.Center, new Color(0.80f, 0.86f, 0.88f), "LastStandRules");
        UIFactory.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(1600f, 40f));

        var squadText = LastStandSquadSummary(save);
        var squad = UIFactory.Txt(screen, squadText, 19, TextAlignmentOptions.Center,
            new Color(1f, 0.78f, 0.39f), "SelectedSquad");
        UIFactory.Place(squad.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(1200f, 34f));

        lastStandLocationButtons.Clear();
        int count = GameDatabase.Locations.Count;
        const float cardW = 510f, cardH = 172f, gapX = 28f, gapY = 20f;
        float startX = -cardW - gapX;
        float startY = 215f;   // distancia do topo da primeira linha; as proximas descem
        for (int i = 0; i < count; i++)
        {
            var loc = GameDatabase.Locations[i];
            if (loc == null || !GameDatabase.IsUnlocked(loc, save)) continue;
            int col = i % 3, row = i / 3;
            float x = startX + col * (cardW + gapX);
            float y = startY + row * (cardH + gapY);
            BuildLastStandLocationCard(screen, loc, x, y, cardW, cardH);
        }

        var foot = UIFactory.Txt(screen, "EXTRAÇÃO: 100% DAS FICHAS + BÔNUS DE 15% · DERROTA OU SAÍDA ANTECIPADA: 50% · SEM PERDA DE EQUIPAMENTO",
            19, TextAlignmentOptions.Center, new Color(0.73f, 0.81f, 0.84f), "LastStandRewardNote");
        UIFactory.Place(foot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(1400f, 32f));

        var back = UIFactory.Btn(screen, "VOLTAR", 23, ShowGameModes, new Color(0.13f, 0.17f, 0.20f), "BackToModes");
        UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-330f, 40f), new Vector2(250f, 68f));
        var start = UIFactory.Btn(screen, "INICIAR", 25, StartLastStand, new Color(0.58f, 0.13f, 0.08f), "DeployLastStand");
        UIFactory.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(160f, 40f), new Vector2(390f, 68f));

        if (string.IsNullOrEmpty(lastStandSelectedLocationId) || GameDatabase.GetLocation(lastStandSelectedLocationId) == null)
            lastStandSelectedLocationId = GameDatabase.Locations.Count > 0 ? GameDatabase.Locations[0].id : "suburbio";
        RefreshLastStandLocationCards();
        Show("lastStandSetup");
    }

    void BuildLastStandLocationCard(Transform parent, LocationDef loc, float x, float y, float w, float h)
    {
        Color border = loc.id == lastStandSelectedLocationId ? ModeAccent : new Color(0.25f, 0.33f, 0.37f, 0.95f);
        var card = UIKit.Frame(parent, "Arena_" + loc.id, new Color(0.025f, 0.045f, 0.055f, 0.96f), border, 2f);
        UIFactory.Place(card, new Vector2(0.5f, 1f), new Vector2(x, -y), new Vector2(w, h));
        var hit = card.gameObject.AddComponent<Button>();
        var graphic = card.Find("Fill") != null ? card.Find("Fill").GetComponent<Image>() : null;
        if (graphic != null) graphic.raycastTarget = true;
        hit.targetGraphic = graphic;
        // A selecao visual e controlada pelo Outline abaixo; a transicao de cor do Button
        // sobrescreveria a cor do Fill durante o hover/foco.
        hit.transition = Selectable.Transition.None;
        hit.onClick.AddListener(() => { lastStandSelectedLocationId = loc.id; RefreshLastStandLocationCards(); });
        var colors = hit.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 0.65f);
        colors.pressedColor = new Color(0.75f, 0.65f, 0.58f);
        hit.colors = colors;
        lastStandLocationButtons[loc.id] = hit;

        var thumb = UIFactory.Img(card, Color.white, "ArenaArt");
        var sprite = ArtLoader.Get(CampaignThumbnailPath(loc));
        if (sprite == null) sprite = ArtLoader.Get(SceneBackgrounds.PathFor(loc));
        if (sprite != null) thumb.sprite = sprite;
        thumb.preserveAspect = false;
        thumb.color = new Color(0.62f, 0.70f, 0.74f, 0.55f);
        UIFactory.Stretch(thumb.rectTransform, 0f, 0f, 0.38f, 1f);
        thumb.raycastTarget = false;

        var shade = UIFactory.Img(card, new Color(0.006f, 0.012f, 0.016f, 0.34f), "ArenaShade");
        UIFactory.Stretch(shade.rectTransform, 0f, 0f, 0.38f, 1f);
        shade.raycastTarget = false;

        var title = UIFactory.Txt(card, LastStandLocationName(loc.id), 25, TextAlignmentOptions.MidlineLeft,
            Color.white, "ArenaName");
        UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(210f, -26f), new Vector2(270f, 38f));
        title.fontStyle = FontStyles.Bold;

        var level = UIFactory.Txt(card, "NÍVEL RECOMENDADO  " + loc.recommendedLevel, 16, TextAlignmentOptions.MidlineLeft,
            new Color(1f, 0.70f, 0.35f), "ArenaLevel");
        UIFactory.Place(level.rectTransform, new Vector2(0f, 1f), new Vector2(210f, -68f), new Vector2(270f, 28f));
        level.fontStyle = FontStyles.Bold;

        var boss = UIFactory.Txt(card, "CHEFES SURGEM A CADA 5 MIN", 14,
            TextAlignmentOptions.MidlineLeft, new Color(0.75f, 0.83f, 0.85f), "ArenaBoss");
        UIFactory.Place(boss.rectTransform, new Vector2(0f, 1f), new Vector2(210f, -105f), new Vector2(270f, 26f));
    }

    void RefreshLastStandLocationCards()
    {
        foreach (var pair in lastStandLocationButtons)
        {
            if (pair.Value == null) continue;
            var image = pair.Value.targetGraphic as Image;
            bool selectedLocation = pair.Key == lastStandSelectedLocationId;
            if (image) image.color = selectedLocation
                ? new Color(0.12f, 0.075f, 0.055f, 1f)
                : new Color(0.035f, 0.055f, 0.065f, 1f);
            var outline = pair.Value.GetComponent<Outline>();
            if (outline == null) outline = pair.Value.gameObject.AddComponent<Outline>();
            outline.effectColor = selectedLocation ? ModeAccent : new Color(0.12f, 0.17f, 0.19f, 0.75f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }


    void ShowBossRushSetup()
    {
        var save = GameSession.Save;
        if (save == null) { Show("main"); return; }
        save.NormalizeSquad();
        var screen = Rebuild("bossRushSetup");
        CreateUtilityBackdrop(screen, "BossRushSetupBackdrop");
        Title(screen, "CORRIDA DOS CHEFES", 58f);

        var info = UIFactory.Txt(screen, "CINCO CHEFES REGIONAIS · SEM HORDA COMUM · PAUSAS DE 8 SEGUNDOS COM CURA DE 12% PARA A EQUIPE",
            19, TextAlignmentOptions.Center, new Color(0.80f, 0.86f, 0.88f), "BossRushRules");
        UIFactory.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(1600f, 38f));

        var squad = UIFactory.Txt(screen, LastStandSquadSummary(save), 19, TextAlignmentOptions.Center,
            new Color(1f, 0.78f, 0.39f), "SelectedSquad");
        UIFactory.Place(squad.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(1200f, 32f));

        string[] themes = { "escola", "delegacia", "hospital", "centro", "base" };
        string[] names = { "MUTANTE PRINCIPAL", "COMANDANTE CORROMPIDO", "PACIENTE ZERO", "TITÃ DA INFECÇÃO", "EXPERIMENTO ALFA" };
        float[] xs = { -584f, -292f, 0f, 292f, 584f };
        for (int i = 0; i < themes.Length; i++)
        {
            var card = UIKit.Frame(screen, "BossCard_" + (i + 1), new Color(0.025f, 0.045f, 0.055f, 0.96f),
                new Color(0.55f, 0.24f, 0.14f, 0.95f), 1.8f);
            UIFactory.Place(card, new Vector2(0.5f, 1f), new Vector2(xs[i], -194f), new Vector2(270f, 230f));

            var number = UIFactory.Txt(card, "ALVO " + (i + 1).ToString("00"), 16, TextAlignmentOptions.Center,
                new Color(1f, 0.68f, 0.30f), "BossNumber");
            UIFactory.Place(number.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(250f, 26f));

            var art = UIFactory.Img(card, new Color(0.50f, 0.58f, 0.60f, 0.95f), "BossArt");
            var data = Resources.Load<EnemyData>("Config/Enemies/" + themes[i] + "/boss_1");
            if (data != null && data.sprite != null) art.sprite = data.sprite;
            art.preserveAspect = true;
            UIFactory.Place(art.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(170f, 118f));
            art.raycastTarget = false;

            var title = UIFactory.Txt(card, names[i], 17, TextAlignmentOptions.Center, Color.white, "BossTitle");
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(250f, 34f));
            title.fontStyle = FontStyles.Bold;
        }

        var diffTitle = UIFactory.Txt(screen, "ESCOLHA A DIFICULDADE", 20, TextAlignmentOptions.Center,
            new Color(0.80f, 0.86f, 0.88f), "DifficultyTitle");
        UIFactory.Place(diffTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(500f, 30f));

        bossRushDifficultyButtons.Clear();
        string[] diffNames = { "FÁCIL", "NORMAL", "DIFÍCIL", "PESADELO" };
        float[] diffXs = { -330f, -110f, 110f, 330f };
        for (int i = 0; i < diffNames.Length; i++)
        {
            int value = i + 1;
            var button = UIFactory.Btn(screen, diffNames[i], 19,
                () => { bossRushSelectedDifficulty = value; RefreshBossRushDifficultyButtons(); },
                new Color(0.13f, 0.18f, 0.21f), "Difficulty_" + value);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(diffXs[i], -493f), new Vector2(200f, 54f));
            bossRushDifficultyButtons[value] = button;
        }
        RefreshBossRushDifficultyButtons();

        var rewardNote = UIFactory.Txt(screen,
            "RECOMPENSAS: FICHAS DE CHEFE POR DERROTA + BÔNUS POR CONCLUIR TUDO · SEM FARM DE XP OU OURO · SEM PERDA DE EQUIPAMENTO",
            18, TextAlignmentOptions.Center, new Color(0.73f, 0.81f, 0.84f), "BossRushRewardNote");
        UIFactory.Place(rewardNote.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(1500f, 30f));

        var back = UIFactory.Btn(screen, "VOLTAR", 23, ShowGameModes, new Color(0.13f, 0.17f, 0.20f), "BackToModes");
        UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-300f, 42f), new Vector2(260f, 66f));
        var start = UIFactory.Btn(screen, "INICIAR CORRIDA", 24, StartBossRush,
            new Color(0.58f, 0.13f, 0.08f), "StartBossRush");
        UIFactory.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(160f, 42f), new Vector2(390f, 66f));

        Show("bossRushSetup");
    }

    void RefreshBossRushDifficultyButtons()
    {
        foreach (var pair in bossRushDifficultyButtons)
        {
            bool selected = pair.Key == bossRushSelectedDifficulty;
            UIFactory.SetButtonColor(pair.Value, selected
                ? new Color(0.58f, 0.16f, 0.08f)
                : new Color(0.13f, 0.18f, 0.21f));
        }
    }

    void StartBossRush()
    {
        var save = GameSession.Save;
        var arena = GameDatabase.GetLocation("suburbio");
        if (save == null || arena == null) { Toast("A arena da Corrida dos Chefes não está configurada."); return; }

        save.NormalizeSquad();
        GameSession.Mode = GameMode.BossRush;
        GameSession.Location = arena;
        GameSession.Survivor = GameDatabase.GetSurvivor(save.selectedSurvivor);
        GameSession.Partner = save.HasPartner ? GameDatabase.GetSurvivor(save.selectedPartner) : null;
        GameSession.Third = save.HasPartner && save.HasThird ? GameDatabase.GetSurvivor(save.selectedThird) : null;
        GameSession.Difficulty = Mathf.Clamp(bossRushSelectedDifficulty, 1, 4);
        GameSession.DebugRun = false;
        GameSession.InsuranceBought = false;
        GameSession.OpenMapOnLoad = false;
        save.activeRun = new ActiveRun();

        try { SaveSystem.Save(save); }
        catch (System.Exception e) { Debug.LogWarning("Zombit: falha ao salvar antes do Boss Rush: " + e.Message); }
        UnityEngine.SceneManagement.SceneManager.LoadScene(GameSession.GameSceneIndex);
    }

    void StartLastStand()
    {
        var save = GameSession.Save;
        var loc = GameDatabase.GetLocation(lastStandSelectedLocationId);
        if (save == null || loc == null) { Toast("Selecione um campo de batalha primeiro."); return; }

        save.NormalizeSquad();
        GameSession.Mode = GameMode.LastStand;
        GameSession.Location = loc;
        GameSession.Survivor = GameDatabase.GetSurvivor(save.selectedSurvivor);
        GameSession.Partner = save.HasPartner ? GameDatabase.GetSurvivor(save.selectedPartner) : null;
        GameSession.Third = save.HasPartner && save.HasThird ? GameDatabase.GetSurvivor(save.selectedThird) : null;
        GameSession.Difficulty = (int)Difficulty.Normal;
        GameSession.DebugRun = false;
        GameSession.InsuranceBought = false;
        GameSession.OpenMapOnLoad = false;
        save.activeRun = new ActiveRun(); // Last Stand nao deve herdar uma run de campanha interrompida.

        try { SaveSystem.Save(save); }
        catch (System.Exception e) { Debug.LogWarning("Zombit: falha ao salvar antes do Last Stand: " + e.Message); }
        UnityEngine.SceneManagement.SceneManager.LoadScene(GameSession.GameSceneIndex);
    }



    void ShowRefugeDefenseSetup()
    {
        var save = GameSession.Save;
        if (save == null) { Show("main"); return; }
        save.MigrateFromPreviousVersions();
        save.NormalizeSquad();

        var screen = Rebuild("refugeDefenseSetup");
        CreateUtilityBackdrop(screen, "RefugeDefenseSetupBackdrop");
        Title(screen, "DEFESA DO REFÚGIO", 58f);

        var info = UIFactory.Txt(screen, "PROTEJA O GERADOR DURANTE 5 ONDAS · CONSTRUA TORRETAS, REFORCE BARREIRAS E INSTALE MINAS",
            19, TextAlignmentOptions.Center, new Color(0.80f, 0.86f, 0.88f), "RefugeDefenseRules");
        UIFactory.Place(info.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(1600f, 38f));

        var squad = UIFactory.Txt(screen, LastStandSquadSummary(save), 19, TextAlignmentOptions.Center,
            new Color(1f, 0.78f, 0.39f), "SelectedSquad");
        UIFactory.Place(squad.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -148f), new Vector2(1200f, 32f));

        var balance = UIKit.Frame(screen, "DefenseMaterialsBalance", new Color(0.035f, 0.06f, 0.07f, 0.96f),
            new Color(0.88f, 0.42f, 0.16f, 0.95f), 1.5f);
        UIFactory.Place(balance, new Vector2(0.5f, 1f), new Vector2(0f, -197f), new Vector2(420f, 48f));
        var balanceText = UIFactory.Txt(balance, "MATERIAIS DE DEFESA  " + save.defenseMaterials.ToString("N0"),
            18, TextAlignmentOptions.Center, new Color(1f, 0.79f, 0.42f), "DefenseMaterialsBalanceText");
        UIFactory.Stretch(balanceText.rectTransform);
        balanceText.fontStyle = FontStyles.Bold;

        BuildRefugeUpgradeCard(screen, "GERADOR", "Aumenta a integridade do gerador", save.refugeGeneratorLevel,
            80 + save.refugeGeneratorLevel * 60, -465f, "generator");
        BuildRefugeUpgradeCard(screen, "TORRETAS", "Aumenta o dano das torretas automáticas", save.refugeTurretLevel,
            90 + save.refugeTurretLevel * 70, 0f, "turret");
        BuildRefugeUpgradeCard(screen, "BARREIRAS", "Aumenta a resistência inicial das barreiras", save.refugeBarricadeLevel,
            70 + save.refugeBarricadeLevel * 55, 465f, "barricade");

        var diffTitle = UIFactory.Txt(screen, "ESCOLHA A DIFICULDADE", 19, TextAlignmentOptions.Center,
            new Color(0.80f, 0.86f, 0.88f), "RefugeDifficultyTitle");
        UIFactory.Place(diffTitle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 168f), new Vector2(500f, 28f));
        string[] diffNames = { "FÁCIL", "NORMAL", "DIFÍCIL", "PESADELO" };
        float[] diffXs = { -330f, -110f, 110f, 330f };
        for (int i = 0; i < diffNames.Length; i++)
        {
            int value = i + 1;
            var button = UIFactory.Btn(screen, diffNames[i], 18,
                () => { refugeDefenseSelectedDifficulty = value; RefreshRefugeDifficultyButtons(screen); },
                new Color(0.13f, 0.18f, 0.21f), "RefugeDifficulty_" + value);
            UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(diffXs[i], 125f), new Vector2(200f, 52f));
        }
        RefreshRefugeDifficultyButtons(screen);

        var note = UIFactory.Txt(screen, "GANHE MATERIAIS DE DEFESA NAS ONDAS E NOS ABATES · AS MELHORIAS PERMANENTES FICAM SALVAS NO ACAMPAMENTO",
            17, TextAlignmentOptions.Center, new Color(0.72f, 0.80f, 0.83f), "RefugeDefenseRewardNote");
        UIFactory.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 86f), new Vector2(1400f, 28f));

        var back = UIFactory.Btn(screen, "VOLTAR", 22, ShowGameModes, new Color(0.13f, 0.17f, 0.20f), "BackToModes");
        UIFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-300f, 28f), new Vector2(250f, 62f));
        var start = UIFactory.Btn(screen, "INICIAR DEFESA", 23, StartRefugeDefense,
            new Color(0.58f, 0.13f, 0.08f), "StartRefugeDefense");
        UIFactory.Place((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(180f, 28f), new Vector2(390f, 62f));
        Show("refugeDefenseSetup");
    }

    void BuildRefugeUpgradeCard(Transform parent, string title, string description, int level, int cost, float x, string key)
    {
        var card = UIKit.Frame(parent, "RefugeUpgrade_" + key, new Color(0.025f, 0.045f, 0.055f, 0.96f),
            new Color(0.55f, 0.27f, 0.14f, 0.92f), 1.8f);
        UIFactory.Place(card, new Vector2(0.5f, 0.5f), new Vector2(x, -6f), new Vector2(410f, 230f));
        string iconPath = key == "generator" ? "Art/Refuge/generator" :
            key == "turret" ? "Art/Refuge/turret" : "Art/Refuge/barricade";
        var facilitySprite = ArtLoader.Get(iconPath);
        if (facilitySprite != null)
        {
            var icon = UIFactory.Img(card, Color.white, "FacilityArtwork");
            icon.sprite = facilitySprite;icon.preserveAspect = true;icon.raycastTarget = false;
            UIFactory.Place(icon.rectTransform,new Vector2(0f,1f),new Vector2(16f,-46f),new Vector2(105f,112f));
        }
        var heading = UIFactory.Txt(card, title, 25, TextAlignmentOptions.Center, Color.white, "UpgradeTitle");
        UIFactory.Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(46f, -20f), new Vector2(290f, 38f));
        heading.fontStyle = FontStyles.Bold;
        var desc = UIFactory.Txt(card, description, 16, TextAlignmentOptions.Center,
            new Color(0.73f, 0.81f, 0.84f), "UpgradeDescription");
        UIFactory.Place(desc.rectTransform, new Vector2(0.5f, 1f), new Vector2(46f, -64f), new Vector2(290f, 44f));
        var levelText = UIFactory.Txt(card, level >= 5 ? "NÍVEL 5 / 5  ·  MÁXIMO" : "NÍVEL " + level + " / 5", 22,
            TextAlignmentOptions.Center, new Color(1f, 0.72f, 0.32f), "UpgradeLevel");
        UIFactory.Place(levelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(340f, 34f));
        var button = UIFactory.Btn(card, level >= 5 ? "NÍVEL MÁXIMO" : "MELHORAR  ·  " + cost + " MATERIAIS", 18,
            level >= 5 ? (System.Action)null : () => UpgradeRefugeFacility(key),
            level >= 5 ? new Color(0.15f, 0.19f, 0.21f) : new Color(0.48f, 0.17f, 0.09f), "UpgradeButton");
        UIFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(330f, 54f));
        button.interactable = level < 5;
    }

    void UpgradeRefugeFacility(string key)
    {
        var save = GameSession.Save;
        if (save == null) return;
        int level, cost;
        switch (key)
        {
            case "generator": level = save.refugeGeneratorLevel; cost = 80 + level * 60; break;
            case "turret": level = save.refugeTurretLevel; cost = 90 + level * 70; break;
            case "barricade": level = save.refugeBarricadeLevel; cost = 70 + level * 55; break;
            default: return;
        }
        if (level >= 5) { Toast("Esta estrutura já está no nível máximo."); return; }
        if (save.defenseMaterials < cost) { Toast("Materiais de defesa insuficientes."); return; }
        save.defenseMaterials -= cost;
        if (key == "generator") save.refugeGeneratorLevel++;
        else if (key == "turret") save.refugeTurretLevel++;
        else save.refugeBarricadeLevel++;
        try { SaveSystem.Save(save); }
        catch (System.Exception e) { Debug.LogWarning("Zombit: failed to save Refuge upgrade: " + e.Message); }
        ZombitAudio.PlayUI();
        ShowRefugeDefenseSetup();
    }

    void RefreshRefugeDifficultyButtons(Transform screen)
    {
        for (int i = 1; i <= 4; i++)
        {
            var t = screen.Find("RefugeDifficulty_" + i);
            if (t == null) continue;
            var button = t.GetComponent<Button>();
            if (button != null) UIFactory.SetButtonColor(button, refugeDefenseSelectedDifficulty == i
                ? new Color(0.58f, 0.16f, 0.08f) : new Color(0.13f, 0.18f, 0.21f));
        }
    }

    void StartRefugeDefense()
    {
        var save = GameSession.Save;
        var location = GameDatabase.GetLocation("base");
        if (location == null)
        {
            foreach (var candidate in GameDatabase.Locations)
                if (candidate != null && candidate.theme == "base") { location = candidate; break; }
        }
        if (save == null || location == null) { Toast("A localização da Base Militar não está configurada."); return; }
        save.NormalizeSquad();
        GameSession.Mode = GameMode.RefugeDefense;
        GameSession.Location = location;
        GameSession.Survivor = GameDatabase.GetSurvivor(save.selectedSurvivor);
        GameSession.Partner = save.HasPartner ? GameDatabase.GetSurvivor(save.selectedPartner) : null;
        GameSession.Third = save.HasPartner && save.HasThird ? GameDatabase.GetSurvivor(save.selectedThird) : null;
        GameSession.Difficulty = Mathf.Clamp(refugeDefenseSelectedDifficulty, 1, 4);
        GameSession.DebugRun = false;
        GameSession.InsuranceBought = false;
        GameSession.OpenMapOnLoad = false;
        save.activeRun = new ActiveRun();
        try { SaveSystem.Save(save); }
        catch (System.Exception e) { Debug.LogWarning("Zombit: failed to save before Refuge Defense: " + e.Message); }
        UnityEngine.SceneManagement.SceneManager.LoadScene(GameSession.GameSceneIndex);
    }


    static string LastStandSquadSummary(SaveData save)
    {
        var names = new List<string>();
        var main = GameDatabase.GetSurvivor(save.selectedSurvivor);
        if (main != null) names.Add(main.name);
        if (save.HasPartner)
        {
            var partner = GameDatabase.GetSurvivor(save.selectedPartner);
            if (partner != null) names.Add(partner.name);
        }
        if (save.HasThird)
        {
            var third = GameDatabase.GetSurvivor(save.selectedThird);
            if (third != null) names.Add(third.name);
        }
        return "EQUIPE ATUAL  ·  " + string.Join("  /  ", names).ToUpperInvariant();
    }

    static string LastStandLocationName(string id)
    {
        switch (id)
        {
            case "suburbio": return "SUBÚRBIO";
            case "residencial": return "BAIRRO RESIDENCIAL";
            case "shopping": return "SHOPPING";
            case "industrial": return "ZONA INDUSTRIAL";
            case "hospital": return "HOSPITAL";
            case "delegacia": return "DELEGACIA";
            case "metro": return "ESTAÇÃO DE METRÔ";
            case "centro": return "CENTRO DA CIDADE";
            case "base": return "BASE MILITAR";
            default: return id.Replace("_", " ").ToUpperInvariant();
        }
    }
}
