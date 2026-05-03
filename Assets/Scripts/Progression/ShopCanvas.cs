using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Phase 4 / PR 4.PF — procedural Shop UI. One heal offer, two upgrade offers,
// reroll, close. Built at runtime so no scene wiring is required.
public class ShopCanvas : MonoBehaviour
{
    Action<int> onPurchase;
    Action onReroll;
    Action onClose;

    Canvas canvas;
    RectTransform rootRect;
    OfferView[] offerViews;
    TextMeshProUGUI kpText;
    Button rerollButton;
    TextMeshProUGUI rerollText;
    ShopOffer[] offers;
    int rerollCount;
    bool closing;
    KillPointsWallet wallet;

    public static ShopCanvas Show(
        ShopOffer[] offers,
        int rerollCount,
        Action<int> onPurchase,
        Action onReroll,
        Action onClose)
    {
        var go = new GameObject("ShopCanvas");
        var c = go.AddComponent<ShopCanvas>();
        c.onPurchase = onPurchase;
        c.onReroll = onReroll;
        c.onClose = onClose;
        c.Build();
        c.Refresh(offers, rerollCount);
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
        panelImg.color = new Color(0.04f, 0.07f, 0.10f, 0.94f);

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

        var title = MakeText(panelRt, "Title", "SHOP", 52f, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(0f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(36f, -28f);
        title.rectTransform.sizeDelta = new Vector2(420f, 70f);
        title.color = CombatHUDStyle.HealCyanGreen;

        kpText = MakeText(panelRt, "KP", "", 30f, FontStyles.Bold, TextAlignmentOptions.TopRight);
        kpText.rectTransform.anchorMin = new Vector2(1f, 1f);
        kpText.rectTransform.anchorMax = new Vector2(1f, 1f);
        kpText.rectTransform.pivot = new Vector2(1f, 1f);
        kpText.rectTransform.anchoredPosition = new Vector2(-36f, -36f);
        kpText.rectTransform.sizeDelta = new Vector2(300f, 48f);
        kpText.color = CombatHUDStyle.WarnOrange;

        offerViews = new OfferView[3];
        const float cardW = 340f;
        const float cardH = 360f;
        const float gap = 36f;
        float startX = -(cardW + gap);
        for (int i = 0; i < offerViews.Length; i++)
        {
            int idx = i;
            offerViews[i] = OfferView.Build(panelRt, i + 1);
            offerViews[i].root.anchorMin = offerViews[i].root.anchorMax = new Vector2(0.5f, 0.5f);
            offerViews[i].root.pivot = new Vector2(0.5f, 0.5f);
            offerViews[i].root.sizeDelta = new Vector2(cardW, cardH);
            offerViews[i].root.anchoredPosition = new Vector2(startX + i * (cardW + gap), 12f);
            offerViews[i].button.onClick.AddListener(() => onPurchase?.Invoke(idx));
        }

        rerollButton = BuildButton(panelRt, "Reroll", new Vector2(-128f, -260f), new Vector2(240f, 54f), out rerollText);
        rerollButton.onClick.AddListener(() => onReroll?.Invoke());

        var closeButton = BuildButton(panelRt, "Close", new Vector2(150f, -260f), new Vector2(240f, 54f), out var closeText);
        closeText.text = "[ESC] CLOSE";
        closeButton.onClick.AddListener(RequestClose);
    }

    public void Refresh(ShopOffer[] newOffers, int newRerollCount)
    {
        offers = newOffers ?? new ShopOffer[0];
        rerollCount = newRerollCount;
        int kp = KillPointsWallet.Instance != null ? KillPointsWallet.Instance.Total : 0;
        if (kpText != null) kpText.text = $"KP {kp}";

        for (int i = 0; i < offerViews.Length; i++)
        {
            var offer = i < offers.Length ? offers[i] : null;
            offerViews[i].SetOffer(offer, kp);
        }

        int rerollPrice = ShopInventoryGenerator.RerollPrice(rerollCount);
        if (rerollText != null) rerollText.text = $"[R] REROLL  {rerollPrice} KP";
        if (rerollButton != null) rerollButton.interactable = kp >= rerollPrice;
    }

    void OnWalletChanged(int total, int delta)
    {
        Refresh(offers, rerollCount);
    }

    void Update()
    {
        if (closing) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) onPurchase?.Invoke(0);
        else if (kb.digit2Key.wasPressedThisFrame) onPurchase?.Invoke(1);
        else if (kb.digit3Key.wasPressedThisFrame) onPurchase?.Invoke(2);
        else if (kb.rKey.wasPressedThisFrame) onReroll?.Invoke();
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

    sealed class OfferView
    {
        public RectTransform root;
        public Button button;
        TextMeshProUGUI typeText;
        TextMeshProUGUI titleText;
        TextMeshProUGUI descText;
        TextMeshProUGUI priceText;
        TextMeshProUGUI hintText;
        Image bg;
        Image bar;

        public static OfferView Build(RectTransform parent, int hint)
        {
            var v = new OfferView();
            var go = new GameObject("Offer", typeof(RectTransform), typeof(Image), typeof(Button));
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

            v.priceText = MakeText(v.root, "Price", "", 28f, FontStyles.Bold, TextAlignmentOptions.BottomLeft);
            v.priceText.rectTransform.anchorMin = new Vector2(0f, 0f);
            v.priceText.rectTransform.anchorMax = new Vector2(1f, 0f);
            v.priceText.rectTransform.pivot = new Vector2(0.5f, 0f);
            v.priceText.rectTransform.anchoredPosition = new Vector2(18f, 64f);
            v.priceText.rectTransform.sizeDelta = new Vector2(-36f, 42f);

            v.hintText = MakeText(v.root, "Hint", $"[{hint}] BUY", 22f, FontStyles.Bold, TextAlignmentOptions.BottomRight);
            v.hintText.rectTransform.anchorMin = new Vector2(0f, 0f);
            v.hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
            v.hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
            v.hintText.rectTransform.anchoredPosition = new Vector2(-18f, 24f);
            v.hintText.rectTransform.sizeDelta = new Vector2(-36f, 32f);

            return v;
        }

        public void SetOffer(ShopOffer offer, int currentKp)
        {
            if (offer == null)
            {
                root.gameObject.SetActive(false);
                return;
            }
            root.gameObject.SetActive(true);
            bool afford = currentKp >= offer.price;
            bool sold = offer.purchased;
            Color accent = offer.kind == ShopOfferKind.Heal ? CombatHUDStyle.HealCyanGreen : CombatHUDStyle.Cyan;

            bg.color = sold
                ? new Color(0.04f, 0.05f, 0.06f, 0.76f)
                : new Color(0.07f, 0.10f, 0.13f, 0.96f);
            bar.color = sold ? new Color(0.35f, 0.38f, 0.40f, 1f) : accent;
            button.interactable = !sold && afford;

            typeText.text = offer.kind == ShopOfferKind.Heal ? "HEAL" : ResolveRarity(offer.upgrade);
            typeText.color = sold ? Color.gray : accent;
            titleText.text = sold ? "SOLD" : offer.title;
            titleText.color = sold ? Color.gray : CombatHUDStyle.White;
            descText.text = offer.description;
            descText.color = sold ? Color.gray : CombatHUDStyle.White;
            priceText.text = $"{offer.price} KP";
            priceText.color = sold ? Color.gray : (afford ? CombatHUDStyle.WarnOrange : CombatHUDStyle.WarnRed);
            hintText.text = sold ? "PURCHASED" : (afford ? "BUY" : "NOT ENOUGH KP");
            hintText.color = sold ? Color.gray : (afford ? accent : CombatHUDStyle.WarnRed);
        }

        static string ResolveRarity(UpgradeData upgrade)
        {
            return upgrade != null ? upgrade.rarity.ToString().ToUpperInvariant() : "UPGRADE";
        }
    }
}
