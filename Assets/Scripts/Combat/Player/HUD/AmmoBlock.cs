using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Bottom-right ammo + weapon block. Listens to WeaponManager events and
// formats the display based on the weapon's WeaponDefinition (usesAmmo,
// weaponCategory, displayName). Procedural visuals only.
public class AmmoBlock
{
    RectTransform root;
    TextMeshProUGUI clipText;
    TextMeshProUGUI reserveText;
    TextMeshProUGUI weaponNameText;
    TextMeshProUGUI slotText;
    TextMeshProUGUI stateText; // RELOADING / EMPTY / etc.
    Image accentBar;

    WeaponManager manager;
    WeaponBase boundWeapon;

    public static AmmoBlock Build(RectTransform canvasRoot, WeaponManager weaponManager)
    {
        var block = new AmmoBlock { manager = weaponManager };
        block.BuildInternal(canvasRoot);
        block.Bind(weaponManager);
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("AmmoBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-48f, 48f);
        root.sizeDelta = new Vector2(420f, 130f);

        // Big clip number (right aligned)
        var clipGo = new GameObject("Clip", typeof(RectTransform));
        var clipRt = (RectTransform)clipGo.transform;
        clipRt.SetParent(root, false);
        clipRt.anchorMin = new Vector2(1f, 0f);
        clipRt.anchorMax = new Vector2(1f, 0f);
        clipRt.pivot = new Vector2(1f, 0f);
        clipRt.anchoredPosition = new Vector2(-110f, 36f);
        clipRt.sizeDelta = new Vector2(220f, 80f);
        clipText = clipGo.AddComponent<TextMeshProUGUI>();
        clipText.font = CombatHUDStyle.DefaultFont();
        clipText.fontSize = 64f;
        clipText.fontStyle = FontStyles.Bold;
        clipText.alignment = TextAlignmentOptions.BottomRight;
        clipText.color = CombatHUDStyle.White;
        clipText.text = "—";
        clipText.raycastTarget = false;

        // " / reserve " smaller, to the right of the clip
        var reserveGo = new GameObject("Reserve", typeof(RectTransform));
        var reserveRt = (RectTransform)reserveGo.transform;
        reserveRt.SetParent(root, false);
        reserveRt.anchorMin = new Vector2(1f, 0f);
        reserveRt.anchorMax = new Vector2(1f, 0f);
        reserveRt.pivot = new Vector2(1f, 0f);
        reserveRt.anchoredPosition = new Vector2(-4f, 42f);
        reserveRt.sizeDelta = new Vector2(110f, 36f);
        reserveText = reserveGo.AddComponent<TextMeshProUGUI>();
        reserveText.font = CombatHUDStyle.DefaultFont();
        reserveText.fontSize = 28f;
        reserveText.fontStyle = FontStyles.Bold;
        reserveText.alignment = TextAlignmentOptions.BottomRight;
        reserveText.color = CombatHUDStyle.Cyan;
        reserveText.text = "/ —";
        reserveText.raycastTarget = false;

        // Accent bar above the weapon name
        var accentGo = new GameObject("Accent", typeof(RectTransform));
        var accentRt = (RectTransform)accentGo.transform;
        accentRt.SetParent(root, false);
        accentRt.anchorMin = new Vector2(1f, 0f);
        accentRt.anchorMax = new Vector2(1f, 0f);
        accentRt.pivot = new Vector2(1f, 0f);
        accentRt.anchoredPosition = new Vector2(-4f, 28f);
        accentRt.sizeDelta = new Vector2(280f, 2f);
        accentBar = accentGo.AddComponent<Image>();
        accentBar.sprite = CombatHUDStyle.WhiteSprite();
        accentBar.color = CombatHUDStyle.Cyan;
        accentBar.raycastTarget = false;

        // Weapon name (small, above accent? — we put it under clip number)
        var nameGo = new GameObject("WeaponName", typeof(RectTransform));
        var nameRt = (RectTransform)nameGo.transform;
        nameRt.SetParent(root, false);
        nameRt.anchorMin = new Vector2(1f, 0f);
        nameRt.anchorMax = new Vector2(1f, 0f);
        nameRt.pivot = new Vector2(1f, 0f);
        nameRt.anchoredPosition = new Vector2(-4f, 4f);
        nameRt.sizeDelta = new Vector2(280f, 24f);
        weaponNameText = nameGo.AddComponent<TextMeshProUGUI>();
        weaponNameText.font = CombatHUDStyle.DefaultFont();
        weaponNameText.fontSize = 18f;
        weaponNameText.fontStyle = FontStyles.Bold;
        weaponNameText.alignment = TextAlignmentOptions.BottomRight;
        weaponNameText.color = CombatHUDStyle.White;
        weaponNameText.text = "—";
        weaponNameText.raycastTarget = false;

        // Slot indicator (left of weapon name)
        var slotGo = new GameObject("Slot", typeof(RectTransform));
        var slotRt = (RectTransform)slotGo.transform;
        slotRt.SetParent(root, false);
        slotRt.anchorMin = new Vector2(1f, 0f);
        slotRt.anchorMax = new Vector2(1f, 0f);
        slotRt.pivot = new Vector2(1f, 0f);
        slotRt.anchoredPosition = new Vector2(-290f, 4f);
        slotRt.sizeDelta = new Vector2(80f, 24f);
        slotText = slotGo.AddComponent<TextMeshProUGUI>();
        slotText.font = CombatHUDStyle.DefaultFont();
        slotText.fontSize = 18f;
        slotText.fontStyle = FontStyles.Bold;
        slotText.alignment = TextAlignmentOptions.BottomRight;
        slotText.color = CombatHUDStyle.CyanDim;
        slotText.text = "";
        slotText.raycastTarget = false;

        // State text (RELOADING / EMPTY) — sits above clip
        var stateGo = new GameObject("State", typeof(RectTransform));
        var stateRt = (RectTransform)stateGo.transform;
        stateRt.SetParent(root, false);
        stateRt.anchorMin = new Vector2(1f, 0f);
        stateRt.anchorMax = new Vector2(1f, 0f);
        stateRt.pivot = new Vector2(1f, 0f);
        stateRt.anchoredPosition = new Vector2(-4f, 110f);
        stateRt.sizeDelta = new Vector2(280f, 22f);
        stateText = stateGo.AddComponent<TextMeshProUGUI>();
        stateText.font = CombatHUDStyle.DefaultFont();
        stateText.fontSize = 18f;
        stateText.fontStyle = FontStyles.Bold;
        stateText.alignment = TextAlignmentOptions.BottomRight;
        stateText.color = CombatHUDStyle.WarnOrange;
        stateText.text = "";
        stateText.raycastTarget = false;
    }

    public void Bind(WeaponManager wm)
    {
        if (wm == null) return;
        if (manager == wm && subscribed) return;
        Unbind();
        manager = wm;
        wm.OnWeaponEquipped += OnWeaponEquipped;
        wm.OnAmmoChanged += OnAmmoChanged;
        subscribed = true;
        if (wm.CurrentWeapon != null) OnWeaponEquipped(wm.CurrentWeapon);
    }

    bool subscribed;

    // Called every frame by CombatHUDController.Tick — late-binds in case the
    // WeaponManager wasn't resolvable when the block was first built (race with
    // GameManager auto-attach order), and refreshes if the equipped weapon
    // pointer changed without an event having fired.
    public void Tick()
    {
        if (manager == null || !subscribed)
        {
            var wm = Object.FindAnyObjectByType<WeaponManager>();
            if (wm != null) Bind(wm);
        }
        if (manager != null && manager.CurrentWeapon != boundWeapon)
        {
            OnWeaponEquipped(manager.CurrentWeapon);
        }
    }

    public void Unbind()
    {
        if (manager != null && subscribed)
        {
            manager.OnWeaponEquipped -= OnWeaponEquipped;
            manager.OnAmmoChanged -= OnAmmoChanged;
        }
        subscribed = false;
        UnbindWeapon();
    }

    void UnbindWeapon()
    {
        if (boundWeapon == null) return;
        boundWeapon.OnReloadStarted -= OnReloadStarted;
        boundWeapon.OnReloadCompleted -= OnReloadCompleted;
        boundWeapon = null;
    }

    void OnWeaponEquipped(WeaponBase weapon)
    {
        UnbindWeapon();
        boundWeapon = weapon;
        if (weapon != null)
        {
            weapon.OnReloadStarted += OnReloadStarted;
            weapon.OnReloadCompleted += OnReloadCompleted;
        }
        RefreshAll();
    }

    void OnAmmoChanged(WeaponBase weapon, int clip, int reserve)
    {
        RefreshAll();
    }

    void OnReloadStarted(WeaponBase _) { if (stateText != null) { stateText.text = "RELOADING"; stateText.color = CombatHUDStyle.WarnOrange; } }
    void OnReloadCompleted(WeaponBase _) { if (stateText != null) stateText.text = ""; }

    void RefreshAll()
    {
        var w = manager != null ? manager.CurrentWeapon : null;
        if (w == null || w.Definition == null)
        {
            if (clipText != null) clipText.text = "—";
            if (reserveText != null) reserveText.text = "";
            if (weaponNameText != null) weaponNameText.text = "—";
            if (slotText != null) slotText.text = "";
            if (stateText != null) stateText.text = "";
            return;
        }

        var def = w.Definition;
        weaponNameText.text = string.IsNullOrEmpty(def.displayName) ? def.weaponId : def.displayName.ToUpperInvariant();
        slotText.text = $"SLOT {def.slotIndex + 1}";

        if (def.usesAmmo)
        {
            clipText.text = w.CurrentClipAmmo.ToString();
            reserveText.text = $"/ {w.CurrentReserveAmmo}";
            // Empty-clip warning
            if (w.CurrentClipAmmo <= 0 && !w.IsReloading)
            {
                stateText.text = "EMPTY";
                stateText.color = CombatHUDStyle.WarnRed;
            }
            else if (!w.IsReloading)
            {
                stateText.text = "";
            }
            // Color clip text by remaining %
            float frac = def.clipSize > 0 ? (float)w.CurrentClipAmmo / def.clipSize : 0f;
            clipText.color = frac < 0.25f ? CombatHUDStyle.WarnOrange : CombatHUDStyle.White;
            accentBar.color = CombatHUDStyle.Cyan;
        }
        else
        {
            // Infinite-ammo or melee weapons
            bool isMelee = def.weaponCategory == WeaponCategory.Melee;
            clipText.text = isMelee ? "" : "∞";
            reserveText.text = isMelee ? "READY" : "";
            stateText.text = isMelee ? "BLADE READY" : "";
            stateText.color = CombatHUDStyle.Cyan;
            clipText.color = CombatHUDStyle.White;
            accentBar.color = CombatHUDStyle.Cyan;
        }
    }
}
