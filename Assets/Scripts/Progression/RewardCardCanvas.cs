using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Phase 4 / PR 4.PC — procedural reward-card UI. No art assets required;
// the look is consistent with CombatHUDController (procedural sprites,
// sci-fi flat panels). Lifecycle is fully owned by RunProgressionController:
// the canvas is created on Show, destroyed on Hide.
//
// Visual layout (1920×1080 reference):
//   - Full-screen dark overlay with vignette
//   - Title "CHOOSE A REWARD" centered top
//   - 3 cards horizontal in middle band, ~280×420 each, ~40px gap
//   - Skip button below cards
//   - Input hints (1/2/3/Esc to skip)
public class RewardCardCanvas : MonoBehaviour
{
    static readonly Color RarityCommon = new Color32(0x9A, 0xA5, 0xB0, 0xFF);
    static readonly Color RarityRare = new Color32(0x3D, 0x9E, 0xE6, 0xFF);
    static readonly Color RarityEpic = new Color32(0xA6, 0x55, 0xE8, 0xFF);
    static readonly Color RarityLegendary = new Color32(0xFF, 0xB3, 0x47, 0xFF);

    public Action<int> onSelected; // arg: card index (-1 = skip)

    Canvas canvas;
    RectTransform rootRect;
    CardView[] cards;
    bool resolved;

    public static RewardCardCanvas Show(UpgradeData[] options, UpgradeSystem sys, GameObject playerRoot, Action<int> onSelected)
    {
        var go = new GameObject("RewardCardCanvas");
        var rcc = go.AddComponent<RewardCardCanvas>();
        rcc.onSelected = onSelected;
        rcc.Build(options, sys, playerRoot);
        return rcc;
    }

    public void Hide()
    {
        if (this != null && gameObject != null) Destroy(gameObject);
    }

    void Build(UpgradeData[] options, UpgradeSystem sys, GameObject playerRoot)
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000; // above CombatHUDController (4000) + DamageDirectionHUD (4500)

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        rootRect = canvas.GetComponent<RectTransform>();

        // 1) Dim overlay covering the whole screen.
        var overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayGo.transform.SetParent(rootRect, false);
        var overlayRt = (RectTransform)overlayGo.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlayGo.GetComponent<Image>();
        overlayImg.sprite = CombatHUDStyle.WhiteSprite();
        overlayImg.color = new Color(0f, 0f, 0f, 0.55f);

        // 2) Title.
        var title = CreateText(rootRect, "Title", "CHOOSE A REWARD", 56, FontStyles.Bold, TextAlignmentOptions.Center);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(800f, 80f);
        titleRt.anchoredPosition = new Vector2(0f, -120f);
        title.color = CombatHUDStyle.Cyan;

        // 3) Cards row.
        int n = options != null ? options.Length : 0;
        cards = new CardView[n];
        const float cardW = 320f;
        const float cardH = 460f;
        const float gap = 40f;
        float totalWidth = n * cardW + (n - 1) * gap;
        float startX = -totalWidth * 0.5f + cardW * 0.5f;

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            cards[i] = CardView.Build(rootRect, options[i], sys, playerRoot, i + 1);
            var cardRt = cards[i].root;
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(cardW, cardH);
            cardRt.anchoredPosition = new Vector2(startX + i * (cardW + gap), 20f);
            cards[i].clickButton.onClick.AddListener(() => Resolve(idx));
        }

        // 4) Skip button below cards.
        var skipBtnGo = new GameObject("SkipBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        skipBtnGo.transform.SetParent(rootRect, false);
        var skipRt = (RectTransform)skipBtnGo.transform;
        skipRt.anchorMin = new Vector2(0.5f, 0.5f);
        skipRt.anchorMax = new Vector2(0.5f, 0.5f);
        skipRt.pivot = new Vector2(0.5f, 0.5f);
        skipRt.sizeDelta = new Vector2(220f, 56f);
        skipRt.anchoredPosition = new Vector2(0f, -cardH * 0.5f - 60f);
        var skipImg = skipBtnGo.GetComponent<Image>();
        skipImg.sprite = CombatHUDStyle.WhiteSprite();
        skipImg.color = new Color(0.05f, 0.07f, 0.10f, 0.85f);
        var skipBtn = skipBtnGo.GetComponent<Button>();
        skipBtn.onClick.AddListener(() => Resolve(-1));

        var skipLabel = CreateText(skipRt, "Label", "[ESC] SKIP", 26, FontStyles.Bold, TextAlignmentOptions.Center);
        var skipLblRt = skipLabel.rectTransform;
        skipLblRt.anchorMin = Vector2.zero;
        skipLblRt.anchorMax = Vector2.one;
        skipLblRt.offsetMin = Vector2.zero;
        skipLblRt.offsetMax = Vector2.zero;
        skipLabel.color = CombatHUDStyle.White;
    }

    void Update()
    {
        if (resolved) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (cards != null)
        {
            if (cards.Length > 0 && kb.digit1Key.wasPressedThisFrame) Resolve(0);
            else if (cards.Length > 1 && kb.digit2Key.wasPressedThisFrame) Resolve(1);
            else if (cards.Length > 2 && kb.digit3Key.wasPressedThisFrame) Resolve(2);
        }
        if (kb.escapeKey.wasPressedThisFrame) Resolve(-1);
    }

    void Resolve(int index)
    {
        if (resolved) return;
        resolved = true;
        onSelected?.Invoke(index);
    }

    static TextMeshProUGUI CreateText(RectTransform parent, string name, string content, float fontSize, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.color = CombatHUDStyle.White;
        return t;
    }

    // ----------------------------------------------------------------------
    // CardView — one card panel with rarity border, name, description, preview, hint.
    // ----------------------------------------------------------------------
    class CardView
    {
        public RectTransform root;
        public Button clickButton;

        public static CardView Build(RectTransform parent, UpgradeData data, UpgradeSystem sys, GameObject playerRoot, int hintNumber)
        {
            CardView v = new CardView();

            var go = new GameObject($"Card_{data?.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            v.root = (RectTransform)go.transform;
            var bgImg = go.GetComponent<Image>();
            bgImg.sprite = CombatHUDStyle.WhiteSprite();
            bgImg.color = new Color(0.07f, 0.10f, 0.13f, 0.94f);
            v.clickButton = go.GetComponent<Button>();

            Color rarity = ResolveRarityColor(data != null ? data.rarity : UpgradeRarity.Common);

            // Top rarity bar.
            var bar = new GameObject("RarityBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(v.root, false);
            var barRt = (RectTransform)bar.transform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.sizeDelta = new Vector2(0f, 12f);
            barRt.anchoredPosition = Vector2.zero;
            bar.GetComponent<Image>().color = rarity;
            bar.GetComponent<Image>().sprite = CombatHUDStyle.WhiteSprite();

            // Outer border (frame as 4 thin Images).
            CreateBorder(v.root, rarity);

            // Rarity label.
            var rarityLbl = CreateText(v.root, "Rarity", (data != null ? data.rarity.ToString() : "?").ToUpperInvariant(),
                22, FontStyles.Bold, TextAlignmentOptions.Center);
            var rrt = rarityLbl.rectTransform;
            rrt.anchorMin = new Vector2(0f, 1f);
            rrt.anchorMax = new Vector2(1f, 1f);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.sizeDelta = new Vector2(0f, 36f);
            rrt.anchoredPosition = new Vector2(0f, -22f);
            rarityLbl.color = rarity;

            // Name.
            var nameLbl = CreateText(v.root, "Name", data != null ? data.displayName : "—",
                30, FontStyles.Bold, TextAlignmentOptions.Center);
            var nrt = nameLbl.rectTransform;
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(1f, 1f);
            nrt.pivot = new Vector2(0.5f, 1f);
            nrt.sizeDelta = new Vector2(-20f, 80f);
            nrt.anchoredPosition = new Vector2(0f, -64f);
            nameLbl.color = CombatHUDStyle.White;
            nameLbl.textWrappingMode = TextWrappingModes.Normal;

            // Category.
            var catLbl = CreateText(v.root, "Cat", data != null ? CategoryToText(data.category) : "",
                18, FontStyles.Italic, TextAlignmentOptions.Center);
            var crt = catLbl.rectTransform;
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(0f, 24f);
            crt.anchoredPosition = new Vector2(0f, -150f);
            catLbl.color = new Color(rarity.r, rarity.g, rarity.b, 0.75f);

            // Description.
            var descLbl = CreateText(v.root, "Desc", data != null ? data.description : "",
                20, FontStyles.Normal, TextAlignmentOptions.Center);
            var drt = descLbl.rectTransform;
            drt.anchorMin = new Vector2(0f, 0.5f);
            drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(-30f, 100f);
            drt.anchoredPosition = new Vector2(0f, 30f);
            descLbl.color = CombatHUDStyle.White;
            descLbl.textWrappingMode = TextWrappingModes.Normal;

            // Preview (before → after).
            string previewText = data != null ? RewardPreview.Build(data, sys, playerRoot) : "";
            var prevLbl = CreateText(v.root, "Preview", previewText,
                20, FontStyles.Bold, TextAlignmentOptions.Center);
            var prt = prevLbl.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.sizeDelta = new Vector2(-20f, 32f);
            prt.anchoredPosition = new Vector2(0f, 110f);
            prevLbl.color = CombatHUDStyle.HealCyanGreen;

            // Stack count.
            int currentStacks = (sys != null && data != null) ? sys.GetStackCount(data.id) : 0;
            int maxStacks = data != null ? data.maxStacks : 1;
            var stackLbl = CreateText(v.root, "Stack", $"Stack: {currentStacks} → {currentStacks + 1} / {maxStacks}",
                18, FontStyles.Normal, TextAlignmentOptions.Center);
            var srt = stackLbl.rectTransform;
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.sizeDelta = new Vector2(0f, 24f);
            srt.anchoredPosition = new Vector2(0f, 70f);
            stackLbl.color = new Color(0.7f, 0.78f, 0.85f, 1f);

            // Input hint.
            var hintLbl = CreateText(v.root, "Hint", $"[{hintNumber}] SELECT",
                22, FontStyles.Bold, TextAlignmentOptions.Center);
            var hrt = hintLbl.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(1f, 0f);
            hrt.pivot = new Vector2(0.5f, 0f);
            hrt.sizeDelta = new Vector2(0f, 32f);
            hrt.anchoredPosition = new Vector2(0f, 22f);
            hintLbl.color = rarity;

            return v;
        }

        static void CreateBorder(RectTransform card, Color color)
        {
            const float thickness = 3f;
            (Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 pos)[] sides =
            {
                (new Vector2(0,0), new Vector2(1,0), new Vector2(0, thickness), new Vector2(0, thickness*0.5f)),       // bottom
                (new Vector2(0,1), new Vector2(1,1), new Vector2(0, thickness), new Vector2(0, -thickness*0.5f)),      // top under bar
                (new Vector2(0,0), new Vector2(0,1), new Vector2(thickness, 0), new Vector2(thickness*0.5f, 0)),       // left
                (new Vector2(1,0), new Vector2(1,1), new Vector2(thickness, 0), new Vector2(-thickness*0.5f, 0))       // right
            };
            foreach (var s in sides)
            {
                var go = new GameObject("Border", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(card, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = s.anchorMin;
                rt.anchorMax = s.anchorMax;
                rt.sizeDelta = s.sizeDelta;
                rt.anchoredPosition = s.pos;
                var img = go.GetComponent<Image>();
                img.sprite = CombatHUDStyle.WhiteSprite();
                img.color = new Color(color.r, color.g, color.b, 0.9f);
            }
        }

        static Color ResolveRarityColor(UpgradeRarity r)
        {
            switch (r)
            {
                case UpgradeRarity.Rare: return RarityRare;
                case UpgradeRarity.Epic: return RarityEpic;
                case UpgradeRarity.Legendary: return RarityLegendary;
                default: return RarityCommon;
            }
        }

        static string CategoryToText(UpgradeCategory c)
        {
            switch (c)
            {
                case UpgradeCategory.WeaponCore: return "Weapon Core";
                case UpgradeCategory.MobilityCore: return "Mobility Core";
                case UpgradeCategory.SustainCore: return "Sustain Core";
                case UpgradeCategory.CombatTempo: return "Combat Tempo";
                case UpgradeCategory.RareMutator: return "Rare Mutator";
            }
            return "";
        }
    }
}
