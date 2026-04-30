# UI / HUD Polish Plan

**Status:** PLANNED
**Created:** 2026-05-01
**Scope:** next focused UI pass after PR 5.C visual polish

## Goal

Replace the legacy prototype HUD with a cleaner combat HUD that supports the current fast FPS loop without covering the center of the screen.

Style direction:
- sci-fi combat HUD;
- closer to Ultrakill in screen cleanliness;
- Void Survivor-specific technical panels, thin lines, cyan/white base colors, red/orange warning accents;
- keep the center of the screen clear for aiming and movement.

## Accepted Scope

### 1. Player HP Block

Placement: bottom-left.

Required:
- large numeric HP value;
- segmented or angled health bar;
- normal / medium / low HP color states;
- short damage pulse;
- short heal pulse when HP orbs restore health.

### 2. Ammo And Current Weapon Block

Placement: bottom-right.

Required:
- current magazine / reserve ammo;
- current weapon name;
- slot indicator if easy to read from `WeaponManager`;
- reload / empty state if available from the current weapon API;
- special display for infinite-ammo or melee weapons, for example `∞` or `BLADE READY`.

### 3. Dash Charges

Placement: near HP or lower-center edge.

Required:
- two dash charge indicators;
- full / empty / recharging states;
- should be readable during movement without becoming a large UI panel.

### 4. Crosshair

Placement: center.

Required:
- clean small crosshair;
- dynamic expansion on firing / movement if low-risk;
- hit marker on enemy hit;
- kill marker if it can be connected cleanly through `Health.onDeath` or `GameManager.OnEnemyKilled`.

### 5. Enemy Counter

Placement: compact top area or upper-right.

Required:
- replace the large center encounter text with a compact counter;
- format like `ENEMIES 13`;
- optional subtle pulse on kill;
- keep it removable later if the design no longer needs it.

### 6. Damage Direction Indicator

Required:
- restyle the existing `DamageDirectionHUD`;
- make it thinner and less blob-like;
- should point toward incoming damage without covering combat readability.

### 7. Pickup / Heal Feedback

Required:
- show a small `+HP` feedback near the HP block when a health pickup heals the player;
- green/cyan pulse on the HP block;
- do not spawn large floating text in the center of the screen.

## Explicit Non-Scope For First Pass

- Encounter start banner.
- Full weapon icon art set.
- Shop / upgrade / progression UI.
- Removing or redesigning the arena debug overlay.
- Production-level UI animation framework.
- Timer display.

## Legacy UI Decisions

- The old time counter is considered legacy and should be removed from the main combat HUD.
- `Seed / Arena / Biome` debug info should stay for now. It can be hidden behind a debug toggle later.
- Existing `UIManager` can remain for compatibility, but the new HUD should own the active combat UI where practical.

## Likely Implementation Direction

Create a new `CombatHUDController` that auto-wires to:
- player `Health`;
- `WeaponManager` / active `WeaponBase`;
- `GameManager` enemy count / encounter state;
- `PlayerController` dash charge state;
- hit / heal events.

Prefer auto-created runtime UI or low-manual-scene setup so the user does not need heavy Inspector wiring.

Before implementation, inspect:
- `WeaponManager` and `WeaponBase` events / ammo API;
- current `UIManager` wiring in `Assets/test.unity`;
- `GameManager` enemy counters in encounter mode;
- current `DamageDirectionHUD` implementation;
- whether dash charge state needs public read-only properties on `PlayerController`.

## Acceptance Checklist

- [ ] HP is readable at a glance and pulses on damage/heal.
- [ ] Ammo and current weapon are visible without blocking combat.
- [ ] Dash charges show full/empty/recharging state.
- [ ] Crosshair supports aiming and hit feedback.
- [ ] Enemy counter is compact and no longer dominates the center.
- [ ] Time counter is removed from the main HUD.
- [ ] Arena debug info remains visible for development.
- [ ] New HUD compiles cleanly and requires minimal Unity Editor setup.
