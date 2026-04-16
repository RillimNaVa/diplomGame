# Void Survivor Weapon System - Technical Specification

## Purpose

This document defines the target weapon-system architecture for `Void Survivor` and is intended to be handed to another AI agent for implementation.

The chosen approach is the hybrid architecture:

- `WeaponManager + WeaponBase + WeaponDefinition + FireMode`

This system must replace the current combat logic embedded in `PlayerController` with a modular, extensible, data-driven weapon framework that still stays practical for a diploma project.

The work is split into **two sequential pull requests** (PR A and PR B). The scene must remain playable after each PR.

---

## Project Context

Current project state:

- Engine: Unity 6, URP
- Current player movement is implemented in [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- Current health system is implemented in [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- Current projectile logic is implemented in [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)
- Current HUD manager is implemented in [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)
- Current scene with player is [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

Current problems in the existing combat code:

- `PlayerController` contains movement, look, shooting, melee, tracer spawning, animation triggering, and cooldown state in one class
- hitscan, projectile, and melee logic are tightly coupled to player code
- weapon switching does not exist
- ammo/inventory abstraction does not exist
- current melee is a temporary implementation and should be replaced by a weaponized melee solution
- current architecture is not suitable for later roguelike upgrades, pickups, or multiple weapons

---

## High-Level Goal

Implement a weapon system with these properties:

- player movement remains in `PlayerController`
- all weapon logic is moved out of `PlayerController`
- weapon behavior is configured through `ScriptableObject` definitions where practical
- concrete fire behavior is delegated to reusable fire-mode classes
- system supports exactly five planned weapons for Phase 1
- system is compatible with future ammo pickups, UI, upgrades, effects, and viewmodels
- implementation should not over-engineer into a full framework

---

## Scope

### In Scope

- weapon inventory with 5 fixed slots
- weapon switching via `1-5` and mouse wheel
- current weapon firing
- support for semi-auto, full-auto, projectile, shotgun spread, and melee arc
- reserve ammo and clip ammo where applicable
- unlimited-ammo weapons where applicable
- weapon definitions via `ScriptableObject`
- one generic runtime weapon class (`GenericWeapon : WeaponBase`); per-weapon C# subclasses are not required
- extraction of combat logic from `PlayerController`
- integration with existing `Health` component
- basic viewmodel activation/deactivation
- basic firing animation trigger support
- muzzle origin and camera-based aiming support
- compatibility with current `Projectile` prefab workflow
- friendly-fire safety (player cannot damage self with own projectiles)
- weapons stop firing on player death / scene pause

### Out of Scope For This Task

- complete roguelike upgrade system
- shops, pickups spawning from enemies, and economy
- full VFX/audio pipeline polish
- network/multiplayer
- advanced ECS/jobified architecture
- full save/load system
- deep editor tooling beyond required `ScriptableObject` assets
- recoil camera kick (field is not added now; introduce when recoil controller exists)
- equip / unequip animations and equip time (instant switch in DOOM Eternal style)
- shared ammo pools across multiple weapons (each finite-ammo weapon owns its own count)
- alt-fire
- splash damage for Plasma Launcher (direct projectile damage only in this task)

---

## Design Principles

The implementation must follow these principles:

- keep static weapon data (`WeaponDefinition`) separate from runtime state (kept on `WeaponBase` itself)
- keep aiming and player locomotion separate from weapon execution
- avoid duplicating hitscan/projectile/melee logic across weapon classes
- keep the first implementation simple enough to debug inside Unity Editor
- preserve future extensibility for upgrades and alternate variants without forcing a rewrite
- prefer explicit, readable code over abstract indirection
- prefer event-driven loose coupling between weapon system and UI

---

## Target Architecture

### Core Runtime Classes

#### `WeaponManager`

Responsibility:

- owned by the player
- holds the player's weapon slots
- tracks current selected slot and current equipped weapon
- receives input commands from `PlayerController`
- equips and unequips weapons (instant)
- forwards fire/reload/switch input to the active weapon
- exposes current weapon info to UI via events

Rules:

- only one weapon may be active at a time
- inactive weapon viewmodels must be hidden (`SetActive(false)`)
- switching is instant — no equip animation gating in this task
- manager is the single integration point between player input and weapon system
- subscribes to `Health.OnDeath` of its owner and stops processing fire input

Required events (for UI / future upgrade hooks):

- `event Action<WeaponBase> OnWeaponEquipped`
- `event Action<WeaponBase, int, int> OnAmmoChanged` (weapon, clip, reserve)

#### `WeaponBase`

Responsibility:

- abstract `MonoBehaviour` that lives on the same `GameObject` as the weapon viewmodel
- references one `WeaponDefinition`
- owns its runtime state directly as private fields (no separate `WeaponRuntimeState` class)
- exposes methods:
  - `Initialize(WeaponContext context)`
  - `OnEquip()` / `OnUnequip()`
  - `Tick(float deltaTime)` (called by `WeaponManager` while equipped)
  - `TryStartFire()` / `TryStopFire()`
  - `TryReload()`
  - `AddAmmo(int amount)` (no ammo type discrimination — see Ammo Model)
- delegates the actual hit/spawn behavior to its `FireModeBase` instance

Runtime state held internally (private fields):

- `currentClipAmmo`
- `currentReserveAmmo`
- `nextFireTime`
- `isReloading`
- `isEquipped`
- `isTriggerHeld`
- `reloadEndTime`

Rules:

- must not contain player movement logic
- must not poll `Input` directly
- must encapsulate firing permission checks: cooldown, ammo, reload lock, equip state
- on successful fire, increments `nextFireTime` and triggers animator (if any)

#### `WeaponDefinition : ScriptableObject`

Responsibility:

- stores static configuration of a weapon
- one asset per weapon (5 assets total)
- editable in inspector

Required fields:

- `weaponId` (string, stable identifier)
- `displayName` (string, shown in UI)
- `slotIndex` (int, 0-4)
- `weaponCategory` (enum: Pistol, Shotgun, Rifle, Launcher, Melee — for UI/icons only)
- `damage` (float)
- `fireRate` (float, shots per second)
- `range` (float, max hit distance for hitscan/melee)
- `isAutomatic` (bool, hold-to-fire vs press-to-fire)
- `usesAmmo` (bool)
- `clipSize` (int, ignored if `usesAmmo == false`)
- `maxReserveAmmo` (int, ignored if `usesAmmo == false`)
- `reloadDuration` (float, ignored if `usesAmmo == false`)
- `pelletCount` (int, only read by `ShotgunFireMode`)
- `spreadAngle` (float, degrees, only read by `ShotgunFireMode`)
- `projectilePrefab` (`Projectile`, only read by `ProjectileFireMode`)
- `viewModelPrefab` (`GameObject`, optional — if null, `WeaponManager` expects pre-placed viewmodel)
- `muzzleFlashPrefab` (`ParticleSystem`, optional)
- `tracerPrefab` (`LineRenderer`, optional)
- `animTriggerName` (string, optional — animator trigger fired on shot)
- `fireMode` (`[SerializeReference] FireModeBase`) — see Fire Mode Layer

Notes:

- `[SerializeReference]` on `fireMode` lets the inspector pick a concrete FireMode subclass per asset and edit its inner fields directly. **No `fireModeType` enum and no switch-case mapping are needed.** Adding a new fire mode in the future is a single new C# class with `[Serializable]` — Unity exposes it automatically.
- `recoilKick`, `equipDuration`, and `ammoType` from earlier drafts are intentionally omitted. They will be added when the matching subsystem exists.

#### `WeaponContext`

Responsibility:

- bundles execution dependencies that fire modes need at runtime
- passed once into `WeaponBase.Initialize` and forwarded to `FireModeBase`
- avoids hard-coding `FindObjectOfType` inside fire modes

Required fields:

- `Transform owner` (player root, used to filter own colliders)
- `Transform cameraTransform` (aim source — fire modes always cast from camera position/forward)
- `Transform muzzleTransform` (visual origin for tracers, muzzle flash, projectile spawn)
- `LayerMask hitMask` (which layers count as valid hit targets)
- `MonoBehaviour coroutineHost` (so fire modes can start coroutines without being `MonoBehaviour` themselves)

Optional later (do not add until needed):

- recoil controller
- audio emitter
- VFX emitter
- upgrade modifier provider

---

## Fire Mode Layer

The system must use reusable fire-mode classes instead of hardcoding full weapon behavior in every weapon class.

### `FireModeBase`

- `[Serializable] abstract class FireModeBase`
- abstract method:
  - `void ExecuteFire(WeaponContext context, WeaponDefinition definition, WeaponBase weapon)`
- the `weapon` argument is passed so the fire mode can read additional runtime state if needed (e.g., overheat counters in future), without giving fire modes write access to ammo (ammo is decremented by `WeaponBase` before calling `ExecuteFire`)

### Required Fire Modes

#### `HitscanFireMode`

Use for: Pulse Pistol, Void Rifle.

Behavior:

- ray from `cameraTransform.position` in `cameraTransform.forward`
- distance limited by `definition.range`
- on hit, apply `definition.damage` to `Health` if present
- spawn tracer from `muzzleTransform` to hit point (or to ray endpoint if no hit)
- spawn impact effect at hit point if configured

#### `ShotgunFireMode`

Use for: Scatter Gun.

Behavior:

- fires `definition.pelletCount` rays
- each pellet uses random spread within `definition.spreadAngle` cone around camera forward
- each pellet resolves independently against `Health`
- damage per pellet equals `definition.damage` (designer tunes total DPS via `damage * pelletCount`)

#### `ProjectileFireMode`

Use for: Plasma Launcher.

Behavior:

- instantiate `definition.projectilePrefab` at `muzzleTransform.position`
- orient projectile along `cameraTransform.forward`
- call existing `Projectile.Launch(direction, damage)` API to stay compatible with current prefab
- projectile must ignore the player's own collider (use `Physics.IgnoreCollision` between projectile and `owner` colliders, or set up layer collision matrix)

#### `MeleeArcFireMode`

Use for: Void Blade.

Behavior:

- `Physics.OverlapSphere` at `cameraTransform.position + cameraTransform.forward * (range * 0.5f)` with radius `range * 0.5f`
- filter by `hitMask`
- for each unique `Health` in the result, apply `definition.damage` once
- replaces the current `SphereCast` melee in `PlayerController`

---

## Friendly-Fire and Owner Safety

Required behavior:

- Hitscan ray starts at camera position; `WeaponContext.owner` collider hierarchy is skipped (use `Physics.RaycastAll` filtered, or layer mask excluding the player layer)
- Projectile prefabs ignore the owner's collider on spawn (`Physics.IgnoreCollision` between every projectile collider and every `owner` collider)
- Melee `OverlapSphere` excludes the player layer in `hitMask`

The player must not be able to damage themselves with their own weapons under any circumstances in this task.

---

## Lifecycle and Player Death

Required behavior:

- `WeaponManager` subscribes to `Health.OnDeath` of its owner during `Start()`
- on owner death:
  - `currentWeapon.TryStopFire()` is called
  - the manager ignores all subsequent fire/switch/reload input until the scene reloads
- on scene reload (R key), the manager re-initializes from scratch

This prevents zombie firing from a dead player who is still holding the mouse button.

---

## Weapon Types To Implement

The system must support these five weapons. Each is one `WeaponDefinition.asset` plus one `GenericWeapon` `MonoBehaviour` instance in the scene.

### 1. Pulse Pistol

- slot: 1
- mode: `HitscanFireMode`, `isAutomatic = false`
- ammo: `usesAmmo = false`
- behavior: single accurate shot, modest damage, no reload, default starter weapon

### 2. Scatter Gun

- slot: 2
- mode: `ShotgunFireMode`, `isAutomatic = false`, `pelletCount = 8`, `spreadAngle = 12`
- ammo: `usesAmmo = true`, finite reserve
- behavior: 8 pellets, strong close-range, magazine reload

### 3. Void Rifle

- slot: 3
- mode: `HitscanFireMode`, `isAutomatic = true`
- ammo: `usesAmmo = true`, finite reserve
- behavior: full-auto, lower per-shot damage than pistol, sustained DPS

### 4. Plasma Launcher

- slot: 4
- mode: `ProjectileFireMode`, `isAutomatic = false`
- ammo: `usesAmmo = true`, small clip
- behavior: visible projectile, slower cadence (splash deferred)

### 5. Void Blade

- slot: 5
- mode: `MeleeArcFireMode`, `isAutomatic = false`
- ammo: `usesAmmo = false`
- behavior: replaces temporary melee, future glory-kill hook

---

## Inventory Model

Fixed-slot inventory.

Rules:

- 5 slots indexed 0-4 (UI shows 1-5)
- Pulse Pistol and Void Blade are owned at run start
- other weapons may be assigned in scene inspector for now
- `WeaponManager` exposes:
  - `WeaponBase CurrentWeapon`
  - `int CurrentSlot`
  - `bool IsSlotOccupied(int slot)`

Internal structure:

- `WeaponBase[5] slots` (null in empty slots)

Future extensibility note:

- when shop/pickup work begins (Phase 4), `WeaponManager` will gain `AssignWeapon(int slot, WeaponBase weapon)`. The current array layout already supports this — no refactor needed.

---

## Ammo Model

The first implementation supports both infinite-ammo and finite-ammo weapons.

Required behavior:

- weapon defines `usesAmmo` (bool) on its `WeaponDefinition`
- finite-ammo weapons hold:
  - `currentClipAmmo` (loaded into the weapon)
  - `currentReserveAmmo` (held in inventory for that specific weapon)
- reload moves ammo from reserve into clip up to `clipSize`
- unlimited-ammo weapons bypass all ammo checks and reload entirely

Important — **no `AmmoType` enum in this task**:

- each finite-ammo weapon owns its own reserve count
- when ammo pickups appear in Phase 3, the pickup will target a specific weapon (or a category) — at that point an `AmmoType` enum can be introduced if shared pools are wanted; the only changes will be the new enum, a new field on `WeaponDefinition`, and a routing call inside `WeaponManager.AddAmmo`. No FireMode or `WeaponBase` changes will be needed.

API now:

- `WeaponBase.AddAmmo(int amount)` — adds to that weapon's reserve, capped by `maxReserveAmmo`

---

## Input Integration

Input authority remains in `PlayerController`.

`PlayerController` responsibilities after refactor:

- movement, look, jump, dash, slide
- input callbacks
- forwarding combat commands to `WeaponManager`

`PlayerController` must no longer contain:

- weapon-specific cooldown state (`nextFireTime`, `nextMeleeTime`)
- hitscan logic
- projectile spawning logic
- melee damage logic
- tracer spawning coroutine
- direct animator weapon trigger logic (delegated to weapon system)

Required command flow:

- `OnFire` (Value, hold) → `WeaponManager.SetFireHeld(bool held)`
  - `WeaponManager` checks `currentWeapon.definition.isAutomatic`:
    - automatic: calls `TryStartFire` while held, `TryStopFire` on release
    - semi-auto: calls `TryStartFire` only on the held-edge transition (false → true), then ignores until released
- `OnReload` → `WeaponManager.Reload()`
- `OnSwitchSlot1..5` → `WeaponManager.EquipSlot(slotIndex)`
- `OnSwitchScroll` (Vector2 from mouse wheel) → `WeaponManager.CycleSlot(direction)`
- `OnMelee` callback is removed; melee is now a normal weapon (Void Blade, slot 5)

Input Action requirements:

- the existing `Fire` action must be changed from **Button** to **Value (Axis)** so it reports hold state
- new actions to add: `Reload`, `SlotSelect1..SlotSelect5`, `SwitchScroll`

---

## Viewmodel Integration

Each weapon viewmodel is a child `GameObject` under the existing `weaponHolder` transform on the player. The `WeaponBase` `MonoBehaviour` lives on that same viewmodel `GameObject`.

Rules:

- only the equipped weapon's `GameObject` is active; others are `SetActive(false)`
- `WeaponManager` toggles `gameObject.SetActive` on equip/unequip
- firing animation trigger is fired by `WeaponBase` via `Animator` on the viewmodel (animator reference resolved in `Initialize`)
- if `definition.viewModelPrefab` is set and no pre-placed viewmodel exists, `WeaponManager` may instantiate it on first equip; for the first scene wiring pass, all 5 viewmodels are pre-placed in the editor (simpler to debug)

Current scene note:

- player already has `weaponHolder` and an animator setup in [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)
- existing pistol viewmodel becomes the Pulse Pistol; do not rebuild it

---

## Damage Integration

Use the existing `Health` component directly.

Rules:

- fire modes call `health.TakeDamage(damage)` on hit
- no `IDamageable` interface in this task (introduce only when there is a second damageable type, e.g., destructibles)
- damage application must remain compatible with [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)

---

## Required File Structure

Old `Assets/test/` scripts stay where they are (not refactored in this task). All new weapon code goes into a clean tree:

```text
Assets/
  Scripts/
    Combat/
      Weapons/
        Core/
          WeaponManager.cs
          WeaponBase.cs
          GenericWeapon.cs
          WeaponDefinition.cs
          WeaponContext.cs
          WeaponEnums.cs
        FireModes/
          FireModeBase.cs
          HitscanFireMode.cs
          ShotgunFireMode.cs
          ProjectileFireMode.cs
          MeleeArcFireMode.cs
      Data/
        Weapons/
          PulsePistol_Def.asset
          ScatterGun_Def.asset
          VoidRifle_Def.asset
          PlasmaLauncher_Def.asset
          VoidBlade_Def.asset
```

The implementing agent may reorganize slightly but must preserve the Core / FireModes / Data separation.

---

## Scene Wiring Requirements

Required scene work:

- attach `WeaponManager` to the player root (or a dedicated child `WeaponRig`)
- assign `cameraTransform`, `muzzleTransform`, `hitMask` references on the manager
- place 5 weapon viewmodel `GameObject`s as children of `weaponHolder`, each with one `GenericWeapon` component and the matching `WeaponDefinition` asset assigned
- assign starting active slot (default: Pulse Pistol, slot 0)
- the scene must remain playable after each PR

Hard constraints:

- do not break current movement
- do not break current health/enemy damage interaction
- do not leave combat half-migrated between old and new systems at the end of either PR

---

## Migration Plan — Two Pull Requests

The work is split so the project stays playable at every commit boundary.

### PR A — Core + Hitscan + Pulse Pistol Migration

Goal: Pulse Pistol works through the new system. All 4 other weapons do not exist yet. Old combat code is fully removed.

Steps:

1. Create folder structure under `Assets/Scripts/Combat/Weapons/`
2. Create `WeaponEnums.cs`, `WeaponContext.cs`, `WeaponDefinition.cs`
3. Create `FireModeBase.cs` and `HitscanFireMode.cs`
4. Create `WeaponBase.cs` and `GenericWeapon.cs`
5. Create `WeaponManager.cs` (with slot array, but only slot 0 used in this PR; switching can be stubbed)
6. Create `PulsePistol_Def.asset` and assign `HitscanFireMode` to its `fireMode` field via `[SerializeReference]`
7. Add `GenericWeapon` component to existing pistol viewmodel `GameObject`, assign the asset
8. Add `WeaponManager` to player, wire references
9. Modify Input Actions: change `Fire` to Value (Axis)
10. Update `PlayerController`:
    - remove all shooting fields and methods (`Shoot`, `MeleeAttack`, `PlayShootAnim`, `HasAnimatorParameter`, related `[Header]` blocks)
    - `OnFire` forwards to `WeaponManager.SetFireHeld`
    - remove `OnMelee` (Void Blade comes in PR B; melee is fully unavailable between PRs — acceptable)
11. Subscribe `WeaponManager` to `Health.OnDeath` on the player
12. Verify in Editor: pistol shoots, animator triggers, no double-fire paths

PR A acceptance: scene plays, Pulse Pistol fires, no duplicate combat code remains in `PlayerController`.

### PR B — Remaining Weapons + Switching + Ammo + Reload

Goal: full 5-weapon kit, switching, ammo for finite weapons, reload.

Steps:

1. Create `ShotgunFireMode.cs`, `ProjectileFireMode.cs`, `MeleeArcFireMode.cs`
2. Create remaining 4 `WeaponDefinition.asset` files
3. Place 4 new viewmodel `GameObject`s under `weaponHolder` (placeholder primitives are fine if Blender models are not ready)
4. Implement ammo logic in `WeaponBase` (clip, reserve, decrement on fire, block on empty)
5. Implement reload coroutine in `WeaponBase` (uses `coroutineHost` from `WeaponContext`)
6. Implement slot switching in `WeaponManager`:
    - `EquipSlot(int)` — instant `SetActive` toggle
    - `CycleSlot(int direction)` — wraps, skips empty slots
7. Add Input Actions: `Reload`, `SlotSelect1..5`, `SwitchScroll`
8. Wire `OnReload`, slot inputs, scroll input in `PlayerController` → `WeaponManager`
9. Configure `Projectile` prefab to ignore player collider on spawn (in `ProjectileFireMode`)
10. Verify all 5 weapons play correctly; verify Void Blade replaces old melee

PR B acceptance: see "Concrete Acceptance Criteria" below — all conditions must hold.

---

## Concrete Acceptance Criteria

Final completion (after PR B) requires all of:

- player can still move, look, jump, dash, and slide
- player can switch weapons with `1-5`
- player can switch weapons with mouse wheel (skips empty slots)
- active weapon fires correctly according to its mode
- Pulse Pistol fires semi-auto hitscan, unlimited ammo
- Scatter Gun fires 8 pellets with spread, requires reload when clip empty
- Void Rifle fires continuously while trigger is held, requires reload
- Plasma Launcher spawns projectiles that travel and damage on impact, does not damage the player
- Void Blade performs melee through the weapon system, not old `PlayerController` melee code
- finite-ammo weapons cannot fire when clip is empty until reloaded
- unlimited-ammo weapons (Pulse Pistol, Void Blade) never block on ammo or reload
- player can reload finite-ammo weapons via the Reload input
- enemy `Health` reacts correctly to damage from all 5 weapons
- player cannot damage themselves with own projectiles
- on player death, weapons stop firing and ignore input until scene reload
- all old combat code in `PlayerController` is removed (no commented-out remnants)
- the scene remains playable without manual code fixes after migration

---

## Extension Points (Designed-In Future-Proofing)

These are the seams left open intentionally so future phases plug in without refactoring core types:

| Future feature | Where it plugs in | Cost |
|---|---|---|
| New fire mode (e.g., charge weapon, beam) | new `[Serializable] class XFireMode : FireModeBase`, set on the `WeaponDefinition` asset via inspector | 1 file, 0 changes to core |
| Recoil camera kick | add `Vector2 recoilKick` to `WeaponDefinition`, add optional `IRecoilController` to `WeaponContext`, call from `WeaponBase` after successful fire | additive, no core rewrite |
| Audio per weapon | add `AudioClip[] fireSfx` to `WeaponDefinition`, add `AudioSource` to `WeaponContext` | additive |
| Damage upgrades (Phase 4) | add `IDamageModifierProvider` to `WeaponContext`; FireModes multiply `definition.damage` by `provider.GetMultiplier()` before applying | additive, no FireMode signature change |
| Shared ammo pools (Phase 3) | introduce `AmmoType` enum, add field on `WeaponDefinition`, route `AddAmmo` through `WeaponManager` instead of `WeaponBase` directly | one enum + one routing method |
| Glory kill on Void Blade | `MeleeArcFireMode` checks `Health.IsStaggered`, applies bonus + heal trigger | one new branch in one fire mode |
| Equip animations | add `equipDuration` to `WeaponDefinition`, add `Equipping` state in `WeaponBase` that blocks `TryStartFire` during the timer | one state, one timer |
| New weapon (e.g., 6th) | new `WeaponDefinition.asset` + new viewmodel child + slot extension in `WeaponManager` | data only, no script work if FireMode reused |
| HUD ammo display | UI subscribes to `WeaponManager.OnAmmoChanged`; UI never imports `WeaponBase` directly | event-driven, zero coupling |

The fact that **all** of these are additive (no breaking changes to `WeaponBase` or `WeaponManager` signatures) is the test that the architecture is correct.

---

## Non-Functional Requirements

- code must be readable and explicit
- avoid unnecessary inheritance depth (one level: `WeaponBase` → `GenericWeapon`)
- avoid creating a micro-interface for every tiny concern
- avoid reflection and stringly-typed runtime logic beyond animator trigger names
- keep implementation practical for Unity inspector workflows
- do not introduce editor-only dependencies into runtime weapon code
- public API of `WeaponManager` must be stable across both PRs (PR B should not break PR A signatures)

---

## Constraints For The Implementing Agent

- preserve existing movement behavior
- preserve compatibility with the current `Health` system
- preserve compatibility with current projectile prefab workflow
- do not redesign the entire combat game loop beyond weaponization
- do not rewrite enemy AI in this task
- do not bundle upgrade-system work into this task
- avoid changing unrelated scene/render/performance code
- do not refactor existing scripts in `Assets/test/` outside of `PlayerController.cs`

---

## Recommended Simplifications

Built into the spec above; restated for clarity:

- single `GenericWeapon : WeaponBase` runtime class (no per-weapon C# subclasses)
- magazine-based reload only (no shell-by-shell)
- no alt-fire
- no splash damage in this task
- tracer / muzzle / animator references are nullable optional

---

## Notes About Current Project-Specific Details

- current player scene value for `moveSpeed` in `Assets/test.unity` is still serialized as `6`; unrelated but worth knowing
- current R-key scene reload depends on scene build setup and is unrelated to this task
- current melee in `PlayerController` is explicitly temporary — its removal in PR A is expected and acceptable even though Void Blade only arrives in PR B
- current `weaponHolder` and animator setup already exists in the scene and must be reused

---

## Suggested Deliverables

For each PR, the implementing agent must deliver:

- new/modified scripts under `Assets/Scripts/Combat/Weapons/`
- created `WeaponDefinition` assets where applicable
- scene wiring in `Assets/test.unity`
- cleaned `PlayerController` integration
- a short PR description listing migrated responsibilities and any manual inspector assignments still required by the user
- updated entry in `PROGRESS.md` Change Log

---

## Final Implementation Intent

This weapon system is intended to be:

- modular enough for future upgrades and pickups without core rewrites
- simple enough for a diploma project timeline (one developer + AI, ~2 weeks for Phase 1 weapons)
- easy to inspect in Unity Editor (data-driven via ScriptableObjects)
- easy to extend with new fire modes via `[SerializeReference]` (no enum maintenance)

If tradeoffs are required during implementation, prefer:

- correctness over cleverness
- playability over abstraction purity
- modular separation over keeping combat inside `PlayerController`
- additive extension points over speculative interfaces

---

## Implementation Status

### PR A — Core Framework + Pulse Pistol Migration (2026-04-17) — DONE

**New files:**

- `Assets/Scripts/Combat/Weapons/Core/WeaponEnums.cs`
- `Assets/Scripts/Combat/Weapons/Core/WeaponContext.cs`
- `Assets/Scripts/Combat/Weapons/Core/WeaponDefinition.cs` (ScriptableObject with `[SerializeReference] FireModeBase`)
- `Assets/Scripts/Combat/Weapons/Core/WeaponBase.cs` (abstract, runtime state inline — no separate RuntimeState class)
- `Assets/Scripts/Combat/Weapons/Core/GenericWeapon.cs` (one component for all 5 weapons)
- `Assets/Scripts/Combat/Weapons/Core/WeaponManager.cs` (slots[5], events, owner-death halt)
- `Assets/Scripts/Combat/Weapons/FireModes/FireModeBase.cs`
- `Assets/Scripts/Combat/Weapons/FireModes/HitscanFireMode.cs`
- `Assets/Scripts/Combat/Weapons/Editor/FireModeReferenceDrawer.cs` (custom Inspector dropdown for FireMode picker)

**Modified:**

- `Assets/test/PlayerController.cs` — all combat stripped (Shoot, MeleeAttack, PlayShootAnim, HasAnimatorParameter, related fields). `OnFire` now forwards to `WeaponManager.SetFireHeld`. `OnMelee` removed (returns as Void Blade in PR B).
- `Assets/test/PlayerInputActions.inputactions` — `Fire` action type `Button → Value/Button` for hold-to-fire support (needed by future full-auto Void Rifle).

**Scene wiring (done in editor):**

- `WeaponManager` component added to Player with all references wired (owner, cameraTransform, hitMask, ownerHealth).
- `PulsePistol_Def.asset` created with `HitscanFireMode` strategy assigned via the new dropdown.
- `GenericWeapon` component added to pistol viewmodel; `muzzlePoint` child Transform created at barrel tip; `ShotTracer` LineRenderer prefab created and wired into `Tracer Prefab`.
- Slot 0 on `WeaponManager` = pistol viewmodel.

**Verified playable:**

- Pulse Pistol fires semi-auto hitscan via left-click, fire rate ~5/sec.
- Tracer visible from muzzle to hit point.
- Aim correct during full-speed movement, dash, and slide (no bullet curving — the bug from PR #11 did not regress).
- Player cannot damage self (friendly-fire filter works).
- Movement, jump, double-jump, dash (2 charges), slide — no regressions.
- Player death → weapon stops firing and ignores input until scene reload.

### PR B — Remaining Weapons + Switching + Ammo (pending, next session)

- `ShotgunFireMode`, `ProjectileFireMode`, `MeleeArcFireMode` classes.
- 4 new `WeaponDefinition` assets: Scatter Gun, Void Rifle, Plasma Launcher, Void Blade.
- Slot switching: keys 1-5 + mouse scroll wheel (input actions + forwarding).
- Ammo system for finite-ammo weapons (clip + reserve, reload coroutine, reload input).
- Friendly-fire safety for projectiles (`Physics.IgnoreCollision` between projectile and player colliders on spawn).
- Viewmodel setup for 4 new weapons (primitive placeholders acceptable until Blender models are ready).
- Melee re-enabled through Void Blade in slot 5.

See the Migration Plan section above for full step-by-step of PR B.
