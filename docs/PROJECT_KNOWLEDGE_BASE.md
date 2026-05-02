# Void Survivor - Project Knowledge Base

## Purpose

This file is a persistent knowledge base for the `Void Survivor` Unity project.

It is intended for:

- future AI agents starting in a fresh chat with no prior context
- the project owner as a fast technical overview
- reducing repeated full-project rescans when context windows reset

This document describes the current project structure, important files, implemented systems, scene setup, known issues, and current development direction.

---

## How To Use This File

If you are a new AI agent entering the project, use this order:

1. Read [AI_HANDOFF.md](C:/Users/assam/DiplomGame/docs/AI_HANDOFF.md) first — current active task, do-not-break list, manual setup reminders
2. Read this file for stable architecture overview
3. Read [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md) for full roadmap + Change Log
4. Read [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) if working on Phase 2 (procedural arenas) — active r4 spec
5. Read [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/docs/WEAPON_SYSTEM_TZ.md) / [KILL_TO_SURVIVE_TZ.md](C:/Users/assam/DiplomGame/docs/KILL_TO_SURVIVE_TZ.md) only for combat refactor (completed specs, historical)
6. Only then inspect the exact files related to the requested task

Do not start with a blind full scan of `Library`, `obj`, or package cache.

---

## Project Identity

- Project name: `DiplomGame`
- Current near-term order (2026-05-01): run the PR 3.F / PR 5.A / PR 5.B / PR 5.C Editor playtests, with special attention to pooling lifecycle, shader/VFX readability, subtle dash/slide speed feedback, reduced ceiling-lamp bloom, and no recurring ParticleSystem warnings.
- Cancelled direction: Arena Complex / Connected Arena Rooms was dropped on 2026-04-30; keep the single-arena run pipeline as the active architecture.
- Game concept: fast first-person arcade survival / roguelike
- Inspiration: `DOOM Eternal` + `Ultrakill`
- Engine: Unity 6 with URP
- Main short-term target: verify the landed Phase 3 pooling and Phase 4/5 polish code in Unity Editor, then implement the planned HUD refresh in `docs/UI_HUD_POLISH_PLAN.md`.
- Main long-term target: diploma-ready playable prototype by June 2026

---

## Current Project Status Summary (2026-05-02)

**Update 2026-05-02:** Phase 4 Roguelike Progression — PR 4.PA + PR 4.PB + PR 4.PC + PR 4.PD code landed. New `Assets/Scripts/Progression/` module owns runs and upgrades (`UpgradeData` SO + `UpgradeSystem` auto-singleton + `RewardCardGenerator` + `RewardPreview` + `RewardCardCanvas` + `RunProgressionController` + `UpgradeDebugProbe`). Run graph rebuilt for 10-room standard layout (`RunGraphGenerator` 10-stage `StageTemplates`). New `EliteEncounterModifier` SO drives Elite-arena scaling. Player input pipeline now exposes `PlayerController.IsFrozen` + `SetFrozen(bool)` so reward UI / future cutscenes can freeze input cleanly without the disabled-script SendMessage pitfall. 8 baseline `UpgradeData` YAML in `Assets/Resources/Progression/Upgrades/`. Master spec: `docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md` revision v3. Editor playtest of the full Phase 4 stack still pending after the same-day bugfix pass.

**Update 2026-05-01:** PR 5.C combat/environment feedback polish code landed. The project now has runtime muzzle flash and bullet-impact decals, HP pickup glow, exit portal shader, ambient dust, reactive lamp flicker, damage direction HUD, subtle dash/slide speed feedback, enemy death shards, Spitter strafing, and lightweight enemy separation. Ceiling lamp bloom was reduced after user screenshot feedback. External `dotnet build Assembly-CSharp.csproj --no-restore` is clean; Unity Editor visual/playtest verification is still pending. The next planned UI pass is captured in `docs/UI_HUD_POLISH_PLAN.md`.

**Phase 1 complete. Phase 2 complete through PR 2.E. Phase 3 PR 3.A–3.C verified; PR 3.D code and baseline wiring landed, Unity role-mix playtest pending.** As of the current state of the repository:

**Phase 1 (shipped + playtested):**
- First-person movement fully tuned (walk, jump, double jump, dash with charges, slide, air control, momentum preservation).
- **Modular weapon system shipped** (`WeaponManager` / `WeaponBase` / `WeaponDefinition` (ScriptableObject) / `[SerializeReference] FireModeBase`). Five weapons, switching, ammo, reload.
- **Kill-to-Survive shipped**: HP orbs + `Health.Heal()`, per-enemy loot tables, one-way enemy stagger with emission pulse at ≤20% HP, side-channel `GloryKillDetector` for the `void_blade`, `KillStreakTracker` granting timed movement-speed boost.
- Health system (event-driven) with `onHealthChanged`, `onDeath`, `onTakeDamage`, and `Heal(amount)`.
- Simple enemy AI + wave spawner + minimal HUD.

**Phase 2 (r4 pivot 2026-04-20, TZ APPROVED):**
- **PR 2.A verified 2026-04-21** — `SingleArenaGenerator` + shape (Rect/L/T/Octagon) / cover (Poisson-disk + flow) / exit planners + `ArenaTypeProfile` SO + S/M/L size presets (40/60/80м) + per-arena ceiling 10-25м. 3 preset assets в `Assets/ArenaProfiles/`.
- **PR 2.B verified 2026-04-21** — `RunGraph` (8 nodes, shared subtree) + `RunController` state machine + `ArenaFlowController` fade/teleport + `ExitDoorTrigger` + `DoorChoiceLabel` + Victory/GameOver Canvas + `DefaultRunConfig.asset`.
- **PR 2.C verified 2026-04-22** — async `NavMeshSurface.UpdateNavMesh` via `ArenaNavMeshController`, `GameManager.useEncounterMode` flag + `SetSpawnPoints`/`BeginEncounter`/`EndEncounter` API, `EncounterController` + `SoftLockBarrier` + `EncounterTrigger` (scaffolded), clear conditions (KillAll/ReachExit/None), `SimpleEnemyAI.isOnNavMesh` guard.
- **PR 2.D verified 2026-04-22** — `ArenaVerticalityPlanner`, `BiomeDefinition` + 2 biome assets, Elite/Parkour/Shop/Rest profiles, encounter scaling by `arenaIndex`, runtime debug UI (seed/index/biome).
- **PR 2.E verified 2026-04-24 by user Unity Editor test** — prototype PBR support remains in code, `VoidStation` was retuned away from over-bright blue trim/prop slots, `Parkour` is excluded from generated runs for now, and `SingleArenaGenerator` / `ArenaBuilder` now force `Start` / `Shop` / `Rest` arenas to stay flat with calmer decor even if a profile is misconfigured.
- BSP r1-r3 code помечен `[DEPRECATED]` и оставлен в репо для diploma reference.
- Prototype procedural terrain scene (`SampleScene.unity`) — independent of arena work, will likely be retired.
- Playable combat prototype scene (`test.unity`) wired с Phase 1 systems + `Run`/`ArenaHost` GameObjects для Phase 2.

What is not yet built:

- Phase 4 PR 4.PE Kill Points economy (clear reward + style points + payout UI)
- Phase 4 PR 4.PF Shop Room (offers, reroll, KP spend)
- Phase 4 PR 4.PG Rest Room (heal / max HP / reward boost choice)
- Phase 4 PR 4.PH Balance pass + run stats screen
- Phase 4 PR 4.PI Scenario playtest pass (S1-S10)
- Phase 4 triggered-effect upgrades (Combat Injector, Vampiric Momentum, etc. — `Notify*` hooks exist but no subscribers wire actual gameplay effects yet)
- Arena Complex / Connected Arena Rooms: cancelled 2026-04-30; do not build unless the user explicitly reopens the idea.
- Object pooling note: PR 3.F enemy/projectile pooling code has landed and received a 2026-04-30 lifecycle fix pass; long-run Unity Editor verification is still pending

---

## Important Root Files

### [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)

Main project roadmap and progress tracker.

Contains:

- phase breakdown
- completed and planned steps
- manual Unity setup reminders
- latest development notes

Important note:

- some checklist items in `PROGRESS.md` may be marked done while still being only partially reflected in serialized scene data
- example: movement speed default in code is `10`, but the active scene still has a serialized value of `6`

### [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/docs/WEAPON_SYSTEM_TZ.md)

Detailed technical specification for the planned hybrid weapon system architecture.

Use this when implementing:

- `WeaponManager`
- `WeaponBase`
- `WeaponDefinition`
- fire modes
- weapon switching
- ammo
- melee migration into weapon system

### [Void_Survivor_GDD_v2.docx](C:/Users/assam/DiplomGame/Void_Survivor_GDD_v2.docx)

The game design document.

Use this for:

- gameplay intent
- design-aligned implementation
- feature validation against diploma goals

---

## Folders That Matter

### `Assets/test`

Legacy folder from Phase 0. Still holds:

- `PlayerController.cs` (now slimmed down — movement, input, camera, dash/slide, `SetSpeedMultiplier` hook)
- `GameManager.cs` (wave flow; exposes `OnEnemyKilled` event)
- `SimpleEnemyAI.cs`
- `Health.cs` (with the new `Heal(float)` method)
- `Projectile.cs`
- `UIManager.cs`
- `EnemyHealthBarView.cs`
- Input assets (`PlayerInputActions`)
- `recoil.controller` animator

### `Assets/Scripts`

Contains the post-Phase-1 code organized by subsystem:

- `Assets/Scripts/Combat/Weapons/` — full weapon framework (`WeaponManager`, `WeaponBase`, `WeaponDefinition`, `FireModeBase` subclasses, per-weapon SOs).
- `Assets/Scripts/Combat/Pickups/` — `HealthPickup`, `PickupSpawner`.
- `Assets/Scripts/Combat/Enemies/` — `EnemyLootTable`, `EnemyStagger`.
- `Assets/Scripts/Combat/Player/` — `PlayerStats` (central stats seam), `IGloryKillPolicy` + `AlwaysAllowPolicy`, `GloryKillDetector`, `KillStreakTracker`.
- `Assets/Scripts/ProceduralArena/Core/` — `ArenaRunConfig` SO, `ArenaRuntimeContext` (sub-stream System.Random), `ArenaRoomData`, `ArenaGenerator` ([DEPRECATED] BSP orchestrator).
- `Assets/Scripts/ProceduralArena/Layout/` — [DEPRECATED] BSP: `BspLayoutGenerator`, `RoomPlanner`, `CorridorPlanner`, `RoomTypeAssigner`. Kept for diploma reference.
- `Assets/Scripts/ProceduralArena/Build/` — `ArenaOccupancy`, `ArenaBuilder` (orchestrator with `BuildSingle` r4 entry, calmer decor for utility arenas), `RoomBlockoutBuilder`, `CorridorBlockoutBuilder` ([DEPRECATED]), `ArenaBuildMaterials` (biome material-slot resolution + Resources fallback + marker materials), `BuildUtils`.
- `Assets/Scripts/ProceduralArena/Arena/` — **r4 single-arena module**: `ArenaCategory`, `ArenaShape` + `ShapeWeight`, `ArenaSizePreset`, `ClearCondition`, `ArenaPlacements` (cover + exit + platform + ramp placements), `ArenaTypeProfile` SO, `BiomeDefinition` SO (PR 2.E v2: material slots + atmosphere + contamination), `ArenaShapeGenerator` (Rect/L/T/Octagon masks), `ArenaExitPlanner`, `ArenaCoverPlanner` (Poisson-disk + flow constraint), `ArenaVerticalityPlanner` (deterministic platforms/ramps), `SingleArenaGenerator` (biome + verticality + cover + spawns via sub-stream RNGs, with flat-arena safeguards for `Start` / `Shop` / `Rest`).
- `Assets/Scripts/ProceduralArena/Run/` — **r4 run-graph module**: `RunStage`, `RunGraphNode`, `RunGraph`, `RunGraphGenerator` (8 nodes shared subtree), `RunConfig` SO, `RunController` state machine, `ArenaFlowController` (fade+teleport + encounter scaling hookup + biome fog/ambient application), `ExitDoorTrigger`, `DoorChoiceLabel`, `ArenaRuntimeDebugOverlay`.
- `Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs` — async `NavMeshSurface.UpdateNavMesh` on ArenaRoot.
- `Assets/Scripts/ProceduralArena/Encounter/` — `EncounterController` (per-arena clear condition orchestrator), `SoftLockBarrier` (emissive barrier toggle), `EncounterTrigger` (scaffolded, bypassed by teleport flow).
- `Assets/Scripts/ProceduralArena/Debug/` — `ArenaDebugGizmos` (Scene-view visualization + ContextMenu entries r1-r4), `ArenaGenerationLog`.
- `Assets/Scripts/TerrainGenerator.cs` — prototype terrain, unrelated to Phase 2 arenas (likely to be retired).
- `Assets/ArenaProfiles/` — `Arena_Start_S.asset`, `Arena_Combat_M.asset`, `Arena_Boss_L.asset`, `Arena_Elite_L.asset`, `Arena_Parkour_L.asset`, `Arena_Shop_S.asset`, `Arena_Rest_S.asset`, plus `Biome_VoidStation.asset` and `Biome_AlienNexus.asset` (hand-authored YAML).
- `Assets/Resources/ProceduralArena/Biomes/` — PR 2.E approved texture copies used by the new runtime Resources fallback for biome surfaces before authored `.mat` libraries exist.

### `Assets/Prefabs`

Contains gameplay prefabs:

- enemy prefab
- projectile prefab
- enemy health bar prefab

### `Assets/Scenes`

Contains:

- `SampleScene.unity`

This is the terrain/prototype showcase scene, not the main combat prototype scene.

### `Assets`

Also contains:

- [test.unity](C:/Users/assam/DiplomGame/Assets/test.unity), which is the actual current gameplay scene
- [Recoil.anim](C:/Users/assam/DiplomGame/Assets/Recoil.anim)
- [untitledWithAnimation1.fbx](C:/Users/assam/DiplomGame/Assets/untitledWithAnimation1.fbx), a model asset currently connected to the weapon/animation setup

---

## Folders That Usually Do Not Matter For Gameplay Tasks

These folders should normally be ignored by future AI agents unless the task explicitly requires them:

- `Library`
- `Logs`
- `obj`
- `.vs`
- package cache content inside `Library/PackageCache`

Also usually low priority unless specifically asked:

- `Assets/TutorialInfo`
- `Assets/TextMesh Pro`
- `Assets/_Recovery`

These are mostly template/demo/support assets and not core gameplay logic.

---

## Main Scenes

## 1. [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

This is the current playable gameplay prototype scene.

Purpose:

- active combat sandbox
- player movement and shooting testbed
- enemy spawning and basic wave loop
- HUD validation

Key contents:

- player object
- player camera
- `PlayerInput`
- `PlayerController`
- `Health`
- `GameManager`
- spawn points
- HUD canvas
- basic geometry and materials
- weapon holder / viewmodel-ish setup
- recoil animator setup

Important note:

- this scene is currently not added to Build Settings
- only `Assets/Scenes/SampleScene.unity` is in Build Settings
- because of this, active-scene reload logic may be unreliable if playing `test.unity`

## 2. [Assets/Scenes/SampleScene.unity](C:/Users/assam/DiplomGame/Assets/Scenes/SampleScene.unity)

This is the terrain/procedural world prototype scene.

Purpose:

- test procedural terrain generation
- test URP scene lighting and environment

Key contents:

- `Terrain`
- `TerrainGenerator`
- directional light
- global volume
- scene camera

Important note:

- this scene is currently the only one in Build Settings
- it has much heavier rendering/settings cost than the combat scene
- terrain auto-generation is currently enabled here

---

## Core Gameplay Scripts

## [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)

### Role

Movement + input + camera controller for the player. Combat logic was extracted during the Phase 1 Weapon System refactor.

### Current Responsibilities

- character movement (WASD, air control, momentum)
- jump / double jump
- dash (2 charges with recharge)
- slide
- camera look
- weapon tilt/sway hook
- input forwarding to `WeaponManager`
- `SetSpeedMultiplier(float)` — used by `KillStreakTracker` to apply streak boost to walk/air movement (dash/slide intentionally unscaled)

### Notes

- The class is no longer overloaded. It no longer owns shooting, melee, projectile spawning, tracer lifecycle, or recoil triggering — those live in `Assets/Scripts/Combat/Weapons/`.
- Serialized move-speed mismatch between script default and scene value may still exist — see issue #4.

---

## [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)

### Role

Controls wave flow, enemy spawning, player death behavior, and HUD updates.

### Responsibilities

- starts waves
- counts enemies to spawn
- tracks alive enemies
- updates timer text
- reloads scene on death or on `R`
- finds player references if not assigned

### Strengths

- small and readable
- event-driven integration with `Health`

### Important Notes

- reload uses active scene build index
- this conflicts with the fact that `test.unity` is not in Build Settings
- enemies are instantiated every wave and only deactivated on death, not destroyed permanently

### Known Performance Debt

- continuous wave scaling with no pooling
- inactive dead enemies accumulate in scene hierarchy over time

---

## [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)

### Role

Very simple NavMesh-driven enemy behavior.

### Responsibilities

- follow player
- attack when in range

### Behavior

- gets target player transform
- every `Update()` calls `agent.SetDestination(player.position)`
- checks distance
- damages player if in attack range and cooldown elapsed

### Current State

- functional but primitive
- enough for Phase 0 prototype

### Known Problems

- recalculates destination every frame
- logs every attack
- does `GetComponent<Health>()` during attack instead of caching
- not suitable for large enemy counts
- no state machine, strafing, ranged logic, or crowd handling

This script is a future refactor target for Phase 3.

---

## [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)

### Role

Generic event-driven health component for player and enemies.

### Responsibilities

- store current/max health
- take damage
- emit health-change events
- emit death event
- disable GameObject on death after short delay

### Strengths

- simple
- reusable
- already event-driven

### Important Notes

- this is currently the damage integration point for all combat
- future systems should remain compatible with it unless there is a strong reason to replace it

### Known Problem

- dead objects are disabled, not cleaned up or pooled
- death logs may become expensive in mass combat

---

## [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)

### Role

Handles projectile movement and direct-hit collision damage.

### Responsibilities

- set rigidbody velocity
- expire after lifetime
- damage hit targets
- optionally spawn impact VFX
- destroy itself on impact

### Current State

- works for current prototype
- currently used by player combat code

### Known Notes

- uses instantiate/destroy lifecycle
- later likely candidate for pooling
- splash logic is not implemented yet

This script should remain reusable for the future `Plasma Launcher`.

---

## [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)

### Role

Small UI wrapper for the combat HUD.

### Responsibilities

- update player health slider
- update wave text
- update timer text
- display generic wave state messages

### Current State

- minimal but functional

### Future Direction

This will likely be expanded or replaced when the HUD grows to include:

- ammo
- current weapon
- dash charges
- kill points
- damage numbers

---

## [Assets/test/EnemyHealthBarView.cs](C:/Users/assam/DiplomGame/Assets/test/EnemyHealthBarView.cs)

### Role

Connects enemy `Health` to a UI fill image.

### Responsibilities

- subscribe to enemy health events
- update fill amount
- cleanly unsubscribe on disable

### Current State

- technically correct
- simple and reusable

### Known Performance Note

- each enemy uses a world-space canvas health bar
- this is acceptable at prototype scale, but not ideal for large enemy counts

---

## Prototype/Environment Script

## [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)

### Role

Prototype terrain generation and texturing utility.

### Responsibilities

- generate Perlin/fBM-based terrain heightmap
- stretch terrain height
- apply simple procedural texture layers

### Important Notes

- marked with `[ExecuteAlways]`
- `SampleScene` currently has `autoUpdate` enabled
- this can trigger heavy editor-side terrain generation

### Strategic Note

This system is temporary.

According to project direction:

- procedural terrain will eventually be replaced by arena generation / BSP-based rooms
- this file is useful for prototype history, but not a long-term core system

---

## Prefabs

## [Assets/Prefabs/Enemy.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Enemy.prefab)

### Contents

- basic mesh
- collider
- `NavMeshAgent`
- `Health`
- `SimpleEnemyAI`
- child world-space health bar prefab

### Role

Current prototype enemy unit.

### Notes

- enemy is still very placeholder-like
- designed for simple wave combat only

## [Assets/Prefabs/Projectile.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Projectile.prefab)

### Contents

- mesh
- collider
- rigidbody
- `Projectile` component

### Role

Reusable projectile prefab for current and future weapon logic.

## [Assets/Prefabs/UI/EnemyHealthBar.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/UI/EnemyHealthBar.prefab)

### Role

World-space health bar shown above enemies.

### Notes

- connected to `EnemyHealthBarView`
- uses world-space canvas

---

## Input Assets

## `PlayerInput`

The player in `test.unity` uses Unity Input System `PlayerInput`.

### Important Input-Related Files

- [Assets/test/PlayerInputActions.inputactions](C:/Users/assam/DiplomGame/Assets/test/PlayerInputActions.inputactions)
- [Assets/test/PlayerInputActions.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerInputActions.cs)
- [Assets/test/InputSystem.inputsettings.asset](C:/Users/assam/DiplomGame/Assets/test/InputSystem.inputsettings.asset)

### Notes

- `PlayerInputActions.cs` is generated code
- generated files should generally not be manually edited
- prefer editing the `.inputactions` asset or scene input bindings

There are also other input assets:

- `New Actions.inputactions`
- `InputSystem_Actions.inputactions`

These appear to be extra or legacy assets and are currently lower priority than the active `PlayerInputActions` setup.

---

## Animation / Model Assets

## [Assets/Recoil.anim](C:/Users/assam/DiplomGame/Assets/Recoil.anim)

Animation asset used for weapon recoil behavior.

## [Assets/test/recoil.controller](C:/Users/assam/DiplomGame/Assets/test/recoil.controller)

Animator controller for recoil playback.

## [Assets/untitledWithAnimation1.fbx](C:/Users/assam/DiplomGame/Assets/untitledWithAnimation1.fbx)

Model asset currently tied into the player weapon/animation setup in the gameplay scene.

### Important Note

There is already some viewmodel-style setup in the scene.

This matters for future weapon-system work because:

- it is better to reuse and formalize the existing viewmodel holder than to discard it blindly

---

## Render / URP Settings

Important assets:

- [Assets/Settings/PC_RPAsset.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_RPAsset.asset)
- [Assets/Settings/PC_Renderer.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_Renderer.asset)
- [Assets/Settings/DefaultVolumeProfile.asset](C:/Users/assam/DiplomGame/Assets/Settings/DefaultVolumeProfile.asset)
- [Assets/Settings/SampleSceneProfile.asset](C:/Users/assam/DiplomGame/Assets/Settings/SampleSceneProfile.asset)

### Current Situation

The project currently uses a relatively heavy URP setup for a prototype:

- shadows enabled
- multiple cascades
- SSAO enabled in renderer
- post-processing assets present
- VSync off in quality settings

### Practical Meaning

- editor CPU/GPU load can be high even in simple scenes
- `SampleScene` is particularly expensive due to terrain + URP settings

These settings are not the main focus of the next gameplay task, but they are important context for performance discussions.

---

## Implemented Systems - Functional Summary

## Movement

Current movement system supports:

- walk
- jump
- double jump
- dash with charge cooldown
- slide
- air control
- momentum preservation
- camera pitch weapon tilt
- external speed-multiplier hook (`SetSpeedMultiplier`) consumed by Kill-Streak boosts

## Combat (Weapon System)

Modular weapon framework in `Assets/Scripts/Combat/Weapons/`:

- `WeaponManager` — owns active weapon, handles switching and input routing
- `WeaponBase` — runtime per-weapon state, emits `OnFired`
- `WeaponDefinition` — ScriptableObject with per-weapon data and a `[SerializeReference] FireModeBase`
- `FireModeBase` subclasses: hitscan, projectile, melee arc, etc.
- 5 weapons, switching, ammo, reload (R key)
- Recoil via the existing `recoil.controller` animator

## Kill-to-Survive

Extensibility-first implementation in `Assets/Scripts/Combat/{Pickups,Enemies,Player}/`:

- `PlayerStats` — central stats seam (heal amounts, streak thresholds, multipliers)
- `HealthPickup` + `HPOrb.prefab` — OnTrigger heal via `Health.Heal`
- `EnemyLootTable` — per-enemy drop config, rolls on `Health.onDeath`
- `EnemyStagger` — one-way state at ≤20% HP, emission pulse via material instancing
- `GloryKillDetector` — side-channel observer of `WeaponBase.OnFired` for the `void_blade`; duplicates MeleeArc OverlapSphere math, applies bonus damage + heal through `PlayerStats`
- `KillStreakTracker` — sliding-window kill timestamps; applies timed speed multiplier on threshold
- `IGloryKillPolicy` / `AlwaysAllowPolicy` — pluggable rule for when glory kills are allowed (upgrade hook)

## Enemies

Current enemy system supports:

- spawning by waves
- player chasing via NavMeshAgent
- direct melee-style damage to player
- enemy health bars

Enemy system is still prototype-grade.

## UI

Current HUD supports:

- player health
- wave number / wave state text
- next-wave timer text

UI is deliberately minimal.

## Terrain / Environment

Current environment work includes:

- prototype procedural terrain generation
- prototype terrain texturing
- simple scene geometry in combat scene

Long-term arena generation is not implemented yet.

---

## Known Technical Debt

These are the most important known architecture or engineering debts in the project.

### 1. `PlayerController` is overloaded — RESOLVED (2026-04-17)

Combat was extracted during the Weapon System refactor. Class now owns movement, input, camera, dash/slide, and the `SetSpeedMultiplier` hook only.

### 2. No real weapon system yet — RESOLVED (2026-04-17)

Shipped per `WEAPON_SYSTEM_TZ.md`. See `Assets/Scripts/Combat/Weapons/`.

### 3. `test.unity` is not in Build Settings

Current reload behavior assumes active scene build index works correctly.

But:

- only `SampleScene` is currently registered in Build Settings

### 4. Enemies accumulate

Dead enemies are disabled, not removed or pooled.

This causes:

- scene hierarchy growth
- memory growth over long sessions
- worse scalability over time

### 5. Enemy AI scales poorly

`SimpleEnemyAI` updates path destination every frame and logs frequently.

### 6. Editor-side terrain generation is expensive

`TerrainGenerator` uses `[ExecuteAlways]` and auto-update in `SampleScene`.

### 7. World-space enemy health bars do not scale well

Acceptable now, but not ideal for later waves with many enemies.

---

## Known Performance Findings

From prior audit:

- `SampleScene` can heat CPU significantly because of:
  - terrain generation
  - terrain rendering
  - URP settings
  - post-processing
  - VSync disabled
- `test.unity` is lighter, but combat scalability is limited by:
  - `SimpleEnemyAI`
  - instantiate/destroy workflow
  - logging
  - enemy accumulation

Important practical interpretation:

- high CPU power draw in Unity Editor is not by itself proof of a bug
- but in this project there are real code and settings reasons for elevated load

---

## Current Development Direction

Phase 1 complete. Phase 2 (Procedural Arena Generation r4) in flight:

- PR 2.A + 2.B verified (single-arena gen + run graph + transitions).
- PR 2.C verified 2026-04-22 (async NavMesh bake + encounter integration + soft-lock barriers + `GameManager.useEncounterMode`).
- PR 2.D verified 2026-04-22 (biomes + deterministic platforms/ramps + extended type profiles + encounter difficulty scaling + runtime debug overlay).
- PR 2.E verified 2026-04-24 by user Unity Editor test: biome surface slots with Resources-backed texture fallback remain in place, `VoidStation` uses calmer trim/prop defaults, `Parkour` is excluded from generated runs, and utility arenas now have flat-arena safeguards.

See `AI_HANDOFF.md` for the latest active-task context and Phase 3 starting point.

### Cancelled Arena Complex Direction (captured 2026-04-26, cancelled 2026-04-30)

The user sketched a future map structure where one generated map contains several large combat rooms / arena halls connected directly by wide gates in shared walls. This direction was cancelled on 2026-04-30 after a short prototype attempt. Do not implement or plan Arena Complex / Connected Arena Rooms unless the user explicitly reopens it.

Historical idea, now cancelled:

- one `ArenaRoot` and one runtime NavMesh bake per complex;
- 3-6 large room nodes, each still sized for fast dash / slide / jump combat;
- direct `ArenaDoorLink` gates between neighboring rooms, not long empty corridors;
- staged clears: clear current room, open the next internal gate, eventually open the final run exit;
- existing PR 2.E/F/G/H systems should be reused as room-level or complex-level building blocks.

Current decision: keep the existing single-arena `RunController` / `ArenaFlowController` pipeline as the active arena architecture.

Reference specs:

- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) — **APPROVED r4**, active spec
- [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/docs/WEAPON_SYSTEM_TZ.md) — COMPLETED, history
- [KILL_TO_SURVIVE_TZ.md](C:/Users/assam/DiplomGame/docs/KILL_TO_SURVIVE_TZ.md) — COMPLETED, history

---

## Manual Unity Setup Notes

These are important current project reminders.

- add `Assets/test.unity` to Build Settings / Scene List
- verify serialized `moveSpeed` in `test.unity` if movement speed seems wrong
- verify weapon holder assignment if weapon tilt is expected

These are mentioned because some project state depends on scene/inspector wiring, not only code.

---

## What Future AI Agents Should Usually Inspect First By Task Type

## If task is about movement

Read:

- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about weapons/combat

Read:

- [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/docs/WEAPON_SYSTEM_TZ.md)
- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)
- [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about enemies or waves

Read:

- [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/Prefabs/Enemy.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Enemy.prefab)

## If task is about UI

Read:

- [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)
- [Assets/test/EnemyHealthBarView.cs](C:/Users/assam/DiplomGame/Assets/test/EnemyHealthBarView.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about terrain or world generation

Read:

- [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)
- [Assets/Scenes/SampleScene.unity](C:/Users/assam/DiplomGame/Assets/Scenes/SampleScene.unity)

## If task is about performance

Read:

- this file
- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)
- URP assets in `Assets/Settings`

---

## Suggested Maintenance Strategy For This Knowledge Base

This file will stay useful only if it is kept compact, practical, and updated after major changes.

Recommended maintenance rules:

- update it after each major subsystem refactor
- do not log every tiny code change here
- keep it focused on architecture, scene wiring, and important facts
- keep roadmap details in `PROGRESS.md`
- keep implementation specs in dedicated files like `WEAPON_SYSTEM_TZ.md`

Recommended future companion files:

- `KNOWN_ISSUES.md` for live bugs and technical debt
- `AI_HANDOFF.md` for short-term current-task context
- subsystem-specific specs if other big refactors appear

---

## Final Summary

This project is currently a playable Unity prototype with:

- strong movement prototype
- basic combat prototype
- basic wave/enemy loop
- simple HUD
- temporary terrain prototype

The most important current truth is:

- movement and combat are architecturally mature (Phase 1 shipped)
- procedural arena pipeline (r4) is verified through PR 2.A-2.E: single-arena generation + run graph + async NavMesh + encounters + biome styling + verticality + decor/atmosphere
- next major step is Phase 3 enemy AI: refactor `SimpleEnemyAI` into a state-machine base, then add enemy types and better spawning behavior
- Arena Complex / Connected Arena Rooms is cancelled as of 2026-04-30; keep the single-arena run architecture unless the user explicitly reopens the idea

This file should let future AI agents understand the project quickly without doing a blind full scan first.

