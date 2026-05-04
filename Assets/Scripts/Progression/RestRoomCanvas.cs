using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Phase 4 / PR 4.PG — runtime Rest Room UI. Three options, one pick only,
// no reroll. Built procedurally so no scene wiring is required.
public class RestRoomCanvas : MonoBehaviour
{
    Action<int> onSelect;
    Action onClose;

    Canvas canvas;
    RectTransform rootRect;
    OptionView[] views;
    TextMeshProUGUI kpText;
    RestOption[] options;
    bool closing;
    KillPointsWallet wallet;

    public static RestRoomCanvas Show(RestOption[] options, Action<int> onSelect, Action onClose)
    {
        var go = new GameObject("RestRoomCanvas");
        var c = go.AddComponent<RestRoomCanvas>();
        c.onSelect = onSelect;
        c.onClose = onClose;
        c.Build();
        c.Refresh(options);
        return c;
    }

    public void Hide()
    {
        if (this != null && gameObject != null) Destroy(gameObject);
    }

    void OnEnable()
    {
        wallet = KillPointsWallet.Instance;
        if (wallet != null) wallet.OnTotalChanged += OnWalletChanged;
    }

    void OnDisable()
    {
        if (wallet != null) wallet.OnTotalChanged -= OnWalletChanged;
        wallet = null;
    }

    void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5900;

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

        var overlayGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayGo.transform.SetParent(rootRect, false);
        var overlayRt = (RectTransform)overlayGo.transform;
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;
        var overlayImg = overlayGo.GetComponent<Image>();
        overlayImg.sprite = CombatHUDStyle.WhiteSprite();
        overlayImg.color = new Color(0f, 0f, 0f, 0.42f);

        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(rootRect, false);
        var panelRt = (RectTransform)panelGo.transform;
        panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(1180f, 620f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = CombatHUDStyle.WhiteSprite();
        panelImg.color = new Color(0.05f, 0.08f, 0.12f, 0.94f);

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(panelRt, false);
        var accentRt = (RectTransform)accent.transform;
        accentRt.anchorMin = new Vector2(0f, 1f);
        accentRt.anchorMax = new Vector2(1f, 1f);
        accentRt.pivot = new Vector2(0.5f, 1f);
        accentRt.sizeDelta = new Vector2(0f, 4f);
        accentRt.anchoredPosition = Vector2.zero;
        var accentImg = accent.GetComponent<Image>();
        accentImg.sprite = CombatHUDStyle.WhiteSprite();
        accentImg.color = CombatHUDStyle.HealCyanGreen;

        var title = MakeText(panelRt, "Title", "REST CHAMBER", 52f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(0f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(36f, -28f);
        title.rectTransform.sizeDelta = new Vector2(560f, 70f);
        title.color = CombatHUDStyle.HealCyanGreen;

        var subtitle = MakeText(panelRt, "Subtitle", "Pick one — exit unlocks after choice.", 22f, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        subtitle.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitle.rectTransform.anchorMax = new Vector2(0f, 1f);
        subtitle.rectTransform.pivot = new Vector2(0f, 1f);
        subtitle.rectTransform.anchoredPosition = new Vector2(38f, -90f);
        subtitle.rectTransform.sizeDelta = new Vector2(620f, 30f);
        subtitle.color = new Color(0.7f, 0.85f, 0.9f, 1f);

        kpText = MakeText(panelRt, "KP", "", 30f, FontStyles.Bold, TextAlignmentOptions.TopRight);
        kpText.rectTransform.anchorMin = new Vector2(1f, 1f);
        kpText.rectTransform.anchorMax = new Vector2(1f, 1f);
        kpText.rectTransform.pivot = new Vector2(1f, 1f);
        kpText.rectTransform.anchoredPosition = new Vector2(-36f, -36f);
        kpText.rectTransform.sizeDelta = new Vector2(300f, 48f);
        kpText.color = CombatHUDStyle.WarnOrange;

        views = new OptionView[3];
        const float cardW = 340f;
        const float cardH = 360f;
        const float gap = 36f;
        float startX = -(cardW + gap);
        for (int i = 0; i < views.Length; i++)
        {
            int idx = i;
            views[i] = OptionView.Build(panelRt, i + 1);
            views[i].root.anchorMin = views[i].root.anchorMax = new Vector2(0.5f, 0.5f);
            views[i].root.pivot = new Vector2(0.5f, 0.5f);
            views[i].root.sizeDelta = new Vector2(cardW, cardH);
            views[i].root.anchoredPosition = new Vector2(startX + i * (cardW + gap), 12f);
            views[i].button.onClick.AddListener(() => onSelect?.Invoke(idx));
        }

        var closeButton = BuildButton(panelRt, "Close", new Vector2(0f, -260f), new Vector2(240f, 54f), out var closeText);
        closeText.text = "[ESC] CLOSE";
        closeButton.onClick.AddListener(RequestClose);
    }

    public void Refresh(RestOption[] newOptions)
    {
        options = newOptions ?? new RestOption[0];
        int kp = KillPointsWallet.Instance != null ? KillPointsWallet.Instance.Total : 0;
        if (kpText != null) kpText.text = $"KP {kp}";

        for (int i = 0; i < views.Length; i++)
        {
            var option = i < options.Length ? options[i] : null;
            if (option != null && option.kind == RestOptionKind.RareBoost)
                option.affordable = kp >= option.kpCost;
            else if (option != null)
                option.affordable = true;
            views[i].SetOption(option);
        }
    }

    void OnWalletChanged(int total, int delta)
    {
        Refresh(options);
    }

    void Update()
    {
        if (closing) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) onSelect?.Invoke(0);
        else if (kb.digit2Key.wasPressedThisFrame) onSelect?.Invoke(1);
        else if (kb.digit3Key.wasPressedThisFrame) onSelect?.Invoke(2);
        else if (kb.escapeKey.wasPressedThisFrame) RequestClose();
    }

    void RequestClose()
    {
        if (closing) return;
        closing = true;
        onClose?.Invoke();
    }

    static Button BuildButton(RectTransform parent, string name, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = CombatHUDStyle.WhiteSprite();
        img.color = new Color(0.07f, 0.12f, 0.16f, 0.95f);
        var btn = go.GetComponent<Button>();
        label = MakeText(rt, "Label", "", 22f, FontStyles.Bold, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.color = CombatHUDStyle.White;
        return btn;
    }

    static TextMeshProUGUI MakeText(RectTransform parent, string name, string content, float size, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = CombatHUDStyle.DefaultFont();
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.text = content;
        t.color = CombatHUDStyle.White;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }

    sealed class OptionView
    {
        public RectTransform root;
        public Button button;
        TextMeshProUGUI typeText;
        TextMeshProUGUI titleText;
        TextMeshProUGUI descText;
        TextMeshProUGUI costText;
        TextMeshProUGUI hintText;
        Image bg;
        Image bar;

        public static OptionView Build(RectTransform parent, int hint)
        {
            var v = new OptionView();
            var go = new GameObject("Option", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            v.root = (RectTransform)go.transform;
            v.bg = go.GetComponent<Image>();
            v.bg.sprite = CombatHUDStyle.WhiteSprite();
            v.button = go.GetComponent<Button>();

            var barGo = new GameObject("Bar", typeof(RectTransform), typeof(Image));
            barGo.transform.SetParent(v.root, false);
            var brt = (RectTransform)barGo.transform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.sizeDelta = new Vector2(0f, 5f);
            v.bar = barGo.GetComponent<Image>();
            v.bar.sprite = CombatHUDStyle.WhiteSprite();

            v.typeText = MakeText(v.root, "Type", "", 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            v.typeText.rectTransform.anchorMin = new Vector2(0f, 1f);
            v.typeText.rectTransform.anchorMax = new Vector2(1f, 1f);
            v.typeText.rectTransform.pivot = new Vector2(0.5f, 1f);
            v.typeText.rectTransform.anchoredPosition = new Vector2(18f, -18f);
            v.typeText.rectTransform.sizeDelta = new Vector2(-36f, 28f);

            v.titleText = MakeText(v.root, "Title", "", 30f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            v.titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            v.titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
            v.titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            v.titleText.rectTransform.anchoredPosition = new Vector2(18f, -58f);
            v.titleText.rectTransform.sizeDelta = new Vector2(-36f, 76f);

            v.descText = MakeText(v.root, "Desc", "", 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            v.descText.rectTransform.anchorMin = new Vector2(0f, 1f);
            v.descText.rectTransform.anchorMax = new Vector2(1f, 1f);
            v.descText.rectTransform.pivot = new Vector2(0.5f, 1f);
            v.descText.rectTransform.anchoredPosition = new Vector2(18f, -150f);
            v.descText.rectTransform.sizeDelta = new Vector2(-36f, 104f);

            v.costText = MakeText(v.root, "Cost", "", 26f, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            v.costText.rectTransform.anchorMin = new Vector2(0f, 0f);
            v.costText.rectTransform.anchorMax = new Vector2(1f, 0f);
            v.costText.rectTransform.pivot = new Vector2(0.5f, 0f);
            v.costText.rectTransform.anchoredPosition = new Vector2(18f, 64f);
            v.costText.rectTransform.sizeDelta = new Vector2(-36f, 42f);

            v.hintText = MakeText(v.root, "Hint", $"[{hint}] PICK", 22f, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            v.hintText.rectTransform.anchorMin = new Vector2(0f, 0f);
            v.hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
            v.hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
            v.hintText.rectTransform.anchoredPosition = new Vector2(-18f, 24f);
            v.hintText.rectTransform.sizeDelta = new Vector2(-36f, 32f);

            return v;
        }

        public void SetOption(RestOption option)
        {
            if (option == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            root.gameObject.SetActive(true);
            bool selected = option.selected;
            bool afford = option.affordable;
            Color accent = ResolveAccent(option.kind);

            bg.color = selected
                ? new Color(0.04f, 0.05f, 0.06f, 0.76f)
                : new Color(0.07f, 0.10f, 0.13f, 0.96f);
            bar.color = selected ? new Color(0.35f, 0.38f, 0.40f, 1f) : accent;
            button.interactable = !selected && afford;

            typeText.text = ResolveTypeLabel(option.kind);
            typeText.color = selected ? Color.gray : accent;
            titleText.text = selected ? "TAKEN" : option.title;
            titleText.color = selected ? Color.gray : CombatHUDStyle.White;
            descText.text = option.description;
            descText.color = selected ? Color.gray : CombatHUDStyle.White;

            if (option.kind == RestOptionKind.RareBoost)
            {
                costText.text = $"{option.kpCost} KP";
                costText.color = selected ? Color.gray : (afford ? CombatHUDStyle.WarnOrange : CombatHUDStyle.WarnRed);
                hintText.text = selected ? "TAKEN" : (afford ? "PICK" : "NOT ENOUGH KP");
            }
            else
            {
                costText.text = "FREE";
                costText.color = selected ? Color.gray : CombatHUDStyle.HealCyanGreen;
                hintText.text = selected ? "TAKEN" : "PICK";
            }
            hintText.color = selected ? Color.gray : (afford ? accent : CombatHUDStyle.WarnRed);
        }

        static string ResolveTypeLabel(RestOptionKind kind)
        {
            switch (kind)
            {
                case RestOptionKind.HealPercent: return "HEAL";
                case RestOptionKind.MaxHpFlat: return "REINFORCE";
                case RestOptionKind.RareBoost: return "PRIME";
            }
            return "REST";
        }

        static Color ResolveAccent(RestOptionKind kind)
        {
            switch (kind)
            {
                case RestOptionKind.HealPercent: return CombatHUDStyle.HealCyanGreen;
                case RestOptionKind.MaxHpFlat: return CombatHUDStyle.Cyan;
                case RestOptionKind.RareBoost: return CombatHUDStyle.WarnOrange;
            }
            return CombatHUDStyle.White;
        }
    }
}
