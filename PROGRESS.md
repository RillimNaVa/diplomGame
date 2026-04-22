# Void Survivor — Progress Tracker

> **Diploma:** Development of a computer game in the Arcade survival and Roguelike genres with procedural generation algorithms
> **Style:** DOOM Eternal / Ultrakill fast FPS + Roguelike
> **Engine:** Unity 6 (URP)
> **Deadline:** ~June 2026
> **GDD:** v2.0 (Void_Survivor_GDD_v2.docx)

---

## Phase 0: Foundation (DONE)
- [x] Unity project setup (URP, Input System, NavMesh)
- [x] Basic FPS movement (WASD, jump, dash)
- [x] Basic shooting (hitscan + projectile)
- [x] Basic melee attack
- [x] Health system (event-driven)
- [x] Simple enemy AI (NavMesh pathfinding)
- [x] Basic wave spawner
- [x] Minimal HUD (HP bar, wave counter)
- [x] Procedural terrain (Perlin noise) — will be replaced with arena system
- [x] Gun model + recoil animation
- [x] GDD v2 written

---

## Phase 1: Core Movement & Combat (Weeks 1-2)

### Movement Upgrades
- [x] Increase base speed to 10 m/s
- [x] Double jump
- [x] Slide (Ctrl while moving)
- [x] Air control (full directional control while airborne)
- [x] Dash rework: 2 charges, 3s cooldown per charge
- [x] Momentum preservation (dash → slide combos)

### Weapon System
- [x] Create `WeaponBase.cs` abstract class
- [x] Create `WeaponManager.cs` (switching, inventory) — core in place, switching wired in PR B
- [x] Weapon 1: Pulse Pistol (semi-auto hitscan, unlimited ammo)
- [x] Weapon 2: Scatter Gun (shotgun, 8 pellets spread)
- [x] Weapon 3: Void Rifle (full-auto hitscan)
- [x] Weapon 4: Plasma Launcher (projectile) — splash damage deferred to polish
- [x] Weapon 5: Void Blade (melee, arc swing) — no swing VFX/anim yet (polish)
- [x] Weapon switching (scroll wheel + number keys 1-5) — PR B, code done
- [ ] Ammo system (pickups from enemies) — clip+reserve+reload in place; pickups in Phase 3
- [ ] Weapon viewmodels (first-person arms + gun) — Blender

### Kill-to-Survive
- [x] HP orb drop on enemy kill (5 HP) — PR A done 2026-04-19
- [x] Glory Kill system (melee staggered enemy → 25 HP) — PR B done 2026-04-19
- [x] Enemy stagger state (flashing at low HP) — PR B done 2026-04-19
- [x] Kill streak speed boost (5+ kills in 10s) — PR B done 2026-04-19
- [x] Remove passive HP regen — verified none existed (PR A audit 2026-04-19)

---

## Phase 2: Procedural Arena Generation (Weeks 3-4)

> **Pivot 2026-04-20 (TZ r4):** одна большая процедурная арена за encounter + procedural run graph с door-choice (вместо multi-room BSP). BSP-код остаётся в репо с меткой `[DEPRECATED]` для diploma reference.

### [DEPRECATED] BSP Generator (r1-r3 exploration, kept for diploma reference)
- [x] BSP tree data structure *(PR 1, 2026-04-20)*
- [x] Recursive space partitioning (3-5 splits) *(PR 1)*
- [x] Room placement within leaf nodes *(PR 1)*
- [x] Corridor generation between rooms *(PR 1, MST + extras)*
- [x] Floor/wall/ceiling mesh generation *(PR 2, 2026-04-20)*
- [x] Seed-based randomization *(PR 1, System.Random sub-streams — PRESERVED)*

### PR 2.A — Single-Arena Generator (code done 2026-04-21, pending Editor verification)
- [x] `SingleArenaGenerator` + shape gen (Rect / L / T / Octagon) *(2026-04-21)*
- [x] `ArenaCoverPlanner` (Poisson-disk + flow-constraints) *(2026-04-21)*
- [x] `ArenaExitPlanner` (2 exit doors, opposite walls) *(2026-04-21)*
- [x] `ArenaTypeProfile` SO + `ShapeWeight` + `ClearCondition` *(2026-04-21)*
- [x] `ArenaSizePreset` enum (S 10×10 / M 15×15 / L 20×20 cells → 40/60/80 м) *(2026-04-21)*
- [x] Per-arena ceiling 10–25м by type profile *(2026-04-21)*
- [x] Adapt `ArenaBuilder` to single-room (`BuildSingle` entry point — ArenaRoot/Shell/Cover/Exits/Anchors, no Room_N) *(2026-04-21)*
- [x] Mark BSP modules as `[DEPRECATED]` *(done 2026-04-20)*
- [x] 3 preset assets: `Arena_Start_S`, `Arena_Combat_M`, `Arena_Boss_L` under `Assets/ArenaProfiles/` *(2026-04-21)*
- [x] `ArenaDebugGizmos` extended: profile field + new ContextMenu entries + gizmos for shape mask / exits / cover / spawn *(2026-04-21)*
- [x] Editor verification (Arena_Combat_M assigned, 4 shapes observed, emissive exits/cover/start marker OK) — 2026-04-21

### PR 2.B — Run Graph + Transitions (verified 2026-04-21)
- [x] `RunGraph` + `RunGraphNode` + `RunGraphGenerator` (5-arena run: Start → Mid×3 → Boss, 8 nodes, shared subtree)
- [x] `RunConfig` SO (runSeed + start/boss/mid pools + fade timings + autoStart + skipClearCondition)
- [x] `RunController` state machine (Idle / Generating / Entering / Playing / Transitioning / Victory / GameOver)
- [x] `ArenaFlowController` — fade Canvas + destroy+regenerate + player-teleport via CharacterController.enabled toggle
- [x] `ExitDoorTrigger` — trigger-volume on each exit, dispatches `ChooseDoor(idx)` or `NotifyExitTriggeredOnBoss()`
- [x] `DoorChoiceLabel` — world-space `TextMesh` placeholder over doors ("Combat [2/5]", etc.) + Billboard
- [x] Victory / GameOver placeholder Canvas (auto-built in `RunController.Awake`, "Restart" button → re-runs StartRun)
- [x] `DefaultRunConfig.asset` referencing the 3 preset profiles
- [x] Editor verification — 5-arena traversal + Victory screen OK; fade/labels/determinism OK — 2026-04-21
- [x] Post-verify fixes — Billboard label flip (text was mirrored), door-opening cut in shell wall (emissive was only visible from outside), lintel above door (closes sky gap), solid `ExitBarrier_i` behind trigger (prevents falling off map during fade) — 2026-04-21

### PR 2.C — NavMesh + Encounter Integration (code done 2026-04-22, pending Editor verification)
- [x] Async `NavMeshSurface.UpdateNavMesh` bake *(ArenaNavMeshController, uses com.unity.ai.navigation 2.0.9)*
- [x] `GameManager.SetSpawnPoints / BeginEncounter / EndEncounter` API *(encounter mode gated behind `useEncounterMode` flag — legacy wave loop preserved)*
- [x] Encounter trigger component *(EncounterTrigger.cs scaffolded; current flow bypasses it — `EncounterController.BeginEncounter` is called directly from ArenaFlowController after fadeOut, since teleport places player inside arena)*
- [x] Soft-lock barrier on exit doors *(emissive cube + collider, toggled by EncounterController.Open/Close; `ArenaBuildMaterials.barrier` emissive-orange)*
- [x] Clear conditions: KillAll / ReachExit / None *(ReachExit hook via `EncounterController.FinishByReach`, Timer deferred to PR 2.D)*
- [x] `SimpleEnemyAI.isOnNavMesh` guard — prevents `SetDestination on inactive agent` during first frames of runtime bake
- [ ] Editor verification — wire `useEncounterMode=true` on GameManager, play run through 5 arenas, confirm: (a) enemies spawn on NavMesh, (b) barriers hold until last enemy dies, (c) ExitDoorTrigger fires only after clear

### PR 2.D — Verticality + Biomes + Balance
- [ ] `ArenaVerticalityPlanner` (platforms/ramps for Parkour arenas)
- [ ] `BiomeDefinition` + 2 biome presets
- [ ] Elite / Parkour / Shop / Rest type profiles
- [ ] Difficulty scaling по `arenaIndex`
- [ ] Debug UI: seed display, arena index, biome id

---

## Phase 3: Enemy AI (Weeks 5-6)

### Enemy Types
- [ ] Refactor `SimpleEnemyAI.cs` → state machine base
- [ ] Drone (grunt): fast melee rusher, swarm behavior
- [ ] Sentinel (ranged): keeps distance, slow projectiles
- [ ] Brute (elite): charge attack, ground slam, high HP
- [ ] Void Warden (mini-boss): 2 phases, summons drones

### AI Improvements
- [ ] Strafing behavior
- [ ] Group coordination (spread out, don't stack)
- [ ] Ranged enemy projectiles (dodgeable)
- [ ] Stagger state (low HP → flashy, glory-killable)
- [ ] Death effects (dissolve/explode)

### Spawning
- [ ] Wave composition per arena difficulty
- [ ] Enemy type introduction (Drone→Sentinel→Brute)
- [ ] Object pooling for enemies
- [ ] HP/damage scaling per arena number (+5%)

---

## Phase 4: Roguelike Progression (Weeks 6-7)

### Upgrade System
- [ ] `UpgradeData.cs` (ScriptableObject)
- [ ] `UpgradeSystem.cs` (pool, selection, application)
- [ ] Offensive upgrades: +damage, explosive rounds, piercing, fire rate
- [ ] Defensive upgrades: +max HP, damage reduction, HP on kill boost
- [ ] Mobility upgrades: +speed, extra dash, triple jump, slide damage
- [ ] Special upgrades: vampirism, chain lightning, time slow
- [ ] Upgrade selection UI (3 cards after arena clear)
- [ ] Upgrade stacking logic

### Run Structure
- [ ] `RunManager.cs` (run state, arena progression)
- [ ] Arena chain: arena → upgrade → arena → shop → ...
- [ ] Permadeath (death resets everything)
- [ ] Difficulty scaling per arena
- [ ] Run statistics tracking (kills, time, arenas cleared)

### Shop
- [ ] Shop room (safe, no enemies)
- [ ] Kill Points currency
- [ ] Buy weapons, upgrades, healing
- [ ] Randomized inventory (seed-based)

---

## Phase 5: UI, Audio, Visuals (Weeks 7-9)

### UI
- [ ] Main Menu (Play, Settings, Quit)
- [ ] Pause Menu (Resume, Settings, Quit to Menu)
- [ ] Game Over screen (run statistics)
- [ ] HUD redesign: HP bar, ammo, weapon icon, dash charges
- [ ] Crosshair (dynamic, expands on movement)
- [ ] Wave/arena info (top-center)
- [ ] Kill Points counter
- [ ] Damage numbers (floating text)
- [ ] Settings screen (sensitivity, volume, resolution)

### Audio
- [ ] Weapon SFX (per weapon)
- [ ] Impact/explosion SFX
- [ ] Enemy death SFX
- [ ] Player damage/death SFX
- [ ] UI click/select SFX
- [ ] Dash whoosh SFX
- [ ] Music (1-2 aggressive tracks)
- [ ] Low HP heartbeat

### 3D Models
- [ ] FPS arms/hands model (Blender)
- [ ] Weapon models — at least 2-3 (Blender)
- [ ] Enemy models — Asset Store + simple custom
- [ ] Arena modular pieces — Asset Store + primitives
- [ ] Pickup models (HP orb, ammo)

### VFX
- [ ] Muzzle flash (per weapon)
- [ ] Bullet impact sparks
- [ ] Explosion particle (Plasma Launcher)
- [ ] Dash trail / speed lines
- [ ] Enemy death effect
- [ ] HP orb glow
- [ ] Screen shake on heavy hits

### Post-Processing
- [ ] Bloom
- [ ] Vignette (intensifies at low HP)
- [ ] Chromatic aberration on damage
- [ ] Per-biome color grading

---

## Phase 6: Polish & Testing (Weeks 9-10)

### Balance
- [ ] Weapon damage/fire rate tuning
- [ ] Enemy HP/speed/damage tuning
- [ ] Upgrade power level balancing
- [ ] Difficulty curve testing

### Performance
- [ ] Object pooling (all spawned objects)
- [ ] LOD on arena geometry
- [ ] Profiler check (60 FPS target)
- [ ] Memory leak check

### Final
- [ ] Bug fixing pass
- [ ] Standalone .exe build
- [ ] Gameplay video recording
- [ ] Presentation preparation

---

## Change Log

| Date | What was done |
|------|---------------|
| 2026-04-21 | Phase 2 PR 2.A (r4 Single-Arena Generator) — code complete, pending Editor verification. New `Assets/Scripts/ProceduralArena/Arena/` module: `ArenaCategory`, `ArenaShape` + `ShapeWeight`, `ArenaSizePreset` (S 10×10 / M 15×15 / L 20×20 cells), `ClearCondition`, `ArenaPlacements` (CoverPlacement / ExitDoorAnchor / PlatformPlacement), `ArenaTypeProfile` SO, `ArenaShapeGenerator` (Rect/L/T/Octagon mask builders + weighted picker), `ArenaExitPlanner` (entry wall + 2 exits on opposite perpendicular walls with mid-jitter), `ArenaCoverPlanner` (Poisson-disk + axial-corridor flow constraint + start/door exclusion), `SingleArenaGenerator` (6 sub-stream System.Random RNGs from arenaSeed: size/shape/exit/cover/ceiling/spawn). Extended `ArenaRoomData` with r4 fields (category / shape / shapeMask / wallHeightMeters / coverPlacements / exitDoorAnchors / startSpawnPoint / combatSpawnPoints / platformPlacements). Added `ArenaBuilder.BuildSingle(...)` — single-arena path emits `ArenaRoot/Shell + Cover + Exits + StartMarker + Anchors`, shape-mask-aware wall emission, per-arena ceiling. Extended `ArenaDebugGizmos` with `typeProfile` + `arenaSeed` fields and ContextMenu entries `r4 / Generate Single Arena`, `r4 / Generate + Build Single Arena`, `r4 / Randomize Seed + Build`; gizmos now draw shape mask cells, exit spheres with outward arrow, cover wire-boxes, start spawn sphere, combat spawn spheres. Added `ArenaGenerationLog.BuildSingleSummary`. Authored 3 preset assets `Assets/ArenaProfiles/{Arena_Start_S, Arena_Combat_M, Arena_Boss_L}.asset` directly as YAML. Zero `UnityEngine.Random` in new code (grep-clean). |
| 2026-04-20 | Phase 2 specification drafted: added `ARENA_GENERATION_TZ.md` covering BSP layout, room/corridor generation, controlled encounter flow, runtime NavMesh baking, single-scene arena transitions, debug tooling, and performance constraints. |
| 2026-04-15 | GDD v2 created. PROGRESS.md created. Project analyzed. |
| 2026-04-15 | Phase 1 Movement Upgrades done: speed 10 m/s, double jump, slide (Ctrl), full air control, dash rework (2 charges/3s cooldown, works in air), momentum preservation. (PR #10) |
| 2026-04-15 | Phase 1 playtest fixes (PR #11): slide no longer falls through floor (removed controller.height resizing), slide ends correctly on Ctrl release (Button→Value input type), V + Right Shift added as dash keys, shot direction uses camera.forward (bullets no longer curve during fast motion), weapon tilt on camera pitch (Weapon Sway section), scene reset fixed (R key + auto-reload 2s after death, uses active scene buildIndex instead of hardcoded 0). |
| 2026-04-16 | Phase 1 Weapon System PR A: new modular framework under `Assets/Scripts/Combat/Weapons/` — `WeaponEnums`, `WeaponContext`, `WeaponDefinition` (ScriptableObject with `[SerializeReference] FireModeBase`), `FireModeBase` + `HitscanFireMode`, `WeaponBase` (runtime state inline), `GenericWeapon`, `WeaponManager` (slots[5], events, owner-death halt). Added `Editor/FireModeReferenceDrawer.cs` for Inspector type-picker dropdown. PlayerController stripped of all combat (Shoot/MeleeAttack/PlayShootAnim/HasAnimatorParameter removed); OnFire forwards to WeaponManager.SetFireHeld; OnMelee removed (returns as Void Blade in PR B). Fire input action changed Button → Value (Button) for hold-to-fire. Verified playable 2026-04-17 — Pulse Pistol fires correctly via new system with visible tracer. |
| 2026-04-20 | **Phase 2 PIVOT (TZ r4)** по итогам playtest'а PR 2 r3: переход от multi-room BSP arena к **одной большой процедурной арене за encounter + procedural run graph с door-choice** между аренами (Hades/Roboquest-style). Причины: коридоры ломают FPS-ритм, encounter-per-room конфликтует с dash/slide, roguelike-слой отсутствовал, multi-room BSP переусложнял scope. BSP-код r1-r3 помечен `[DEPRECATED]`-заголовками в `Assets/Scripts/ProceduralArena/{Layout/*, Core/ArenaGenerator.cs, Build/CorridorBlockoutBuilder.cs}` и оставлен в репо для diploma reference (демонстрация алгоритмического исследования). Переписан `ARENA_GENERATION_TZ.md` r4 с новой архитектурой в 4 PR'а (2.A SingleArenaGenerator + shape/cover/exit planners, 2.B run graph + transitions, 2.C async NavMesh + encounter, 2.D verticality + biomes). Потолок теперь per-arena 10-25м по type profile. Determinism / seed sub-streams / builder pipeline / anchor-система / URP materials переносятся в r4 без изменений. |
| 2026-04-20 | Phase 2 PR 2 r3 (Physical Build, flat blockout) — SUPERSEDED by r4: new `Assets/Scripts/ProceduralArena/Build/` module — `ArenaOccupancy` (macro-cell grid Empty/Room/Corridor), `ArenaBuildMaterials` (URP Lit defaults + emissive start/exit markers), `BuildUtils.SpawnBox`, `RoomBlockoutBuilder` (marker + cover pillars on microGrid via `spawnRng` + wall/corner/ceiling/floor/doorFrame anchors), `CorridorBlockoutBuilder` (wall-edge anchors along path), `ArenaBuilder` orchestrator (one `ArenaRoot` GO per build; per-room `Shell` children hold floor/ceiling/walls; walls emitted only on interior-cell edges facing Empty OR different-room interior — door gaps arise naturally where rooms meet corridor cells). Extended `ArenaRunConfig` with build-tier fields (`wallHeightMeters`, `wallThicknessMeters`, floor/ceiling thickness, cover density/width/height/spacing, anchor toggles). Extended `ArenaDebugGizmos` with ContextMenu entries `Build Geometry`, `Generate + Build`, `Clear Geometry` and `buildGeometryOnStart` play-mode flag. Zero `UnityEngine.Random` usage preserved (grep clean). All rooms flat — no verticality (PR 4). |
| 2026-04-20 | Phase 2 PR 1 (Layout + Seed): new module `Assets/Scripts/ProceduralArena/` with Core (`ArenaRunConfig` SO, `ArenaRuntimeContext` with 5 sub-stream `System.Random`s, `ArenaGenerator` orchestrator with retry + hand-coded fallback), Layout algorithms (`BspLayoutGenerator` recursive split with jitter, `RoomPlanner` rect-in-leaf, `CorridorPlanner` MST + L-paths + extras, `RoomTypeAssigner` Start=corner + Exit=BFS-farthest), Debug (`ArenaDebugGizmos` MonoBehaviour with ContextMenu Generate/Random/Clear + Scene-view gizmos, `ArenaGenerationLog` single-line summary). Zero `UnityEngine.Random` usage (verified by grep). Acceptance verified 2026-04-20: deterministic seed, no overlap, Start/Exit present, all rooms connected, no Console spam, <1ms generation on 24×24 grid with 6 rooms. |
| 2026-04-19 | Phase 1 Kill-to-Survive PR B: `Health` untouched. `GameManager.OnEnemyKilled` public event added; fires inside `OnEnemyDied`. `PlayerController.SetSpeedMultiplier(float)` added; `HandleMovement` now multiplies walk/air speed (dash/slide left authored). New `IGloryKillPolicy` + `GloryKillContext` + `AlwaysAllowPolicy` (seam #3). New `GloryKillDetector` — observes `WeaponManager.OnWeaponEquipped`, subscribes to Void Blade's `OnFired`, does its own `OverlapSphere` (same math as `MeleeArcFireMode`), asks policy, applies bonus damage + heal (one glory per swing). New `EnemyStagger` — listens to `Health.onHealthChanged`, enters one-way staggered state at ≤20% HP, instances materials + `_EMISSION` keyword, pulses `_EmissionColor`. New `KillStreakTracker` — subscribes to `GameManager.OnEnemyKilled`, sliding-window `List<float>` of timestamps, applies speed boost via `PlayerController` when crossing threshold, auto-expires. Scene wiring: `Enemy.prefab` got `EnemyStagger`; `test.unity` Player got `AlwaysAllowPolicy` + `GloryKillDetector` + `KillStreakTracker` (all references auto-resolved via `GetComponent` in Awake, so no manual drag required). |
| 2026-04-19 | Phase 1 Kill-to-Survive PR A: `Health.Heal(float)` added. New `PlayerStats` (seam #1, on Player, contains all tunables for both PR A and PR B). New `HealthPickup` + static `PickupSpawner` (`Assets/Scripts/Combat/Pickups/`). New `EnemyLootTable` + `LootEntry` (seam #2, `Assets/Scripts/Combat/Enemies/`). `HPOrb.prefab` + emissive-green `HPOrb.mat` authored directly as YAML under `Assets/Prefabs/`. `Enemy.prefab` wired with `EnemyLootTable` containing one entry: HPOrb @ 15% chance. `test.unity` Player wired with `PlayerStats` component (default tunables). Passive-regen audit: none found. GameManager untouched (TZ-allowed — still counts waves via onDeath). |
| 2026-04-17 | Phase 1 Weapon System PR B: added `ShotgunFireMode`, `ProjectileFireMode` (with owner-collision ignore on spawn), `MeleeArcFireMode`. Created 4 new `WeaponDefinition` assets (Scatter Gun, Void Rifle, Plasma Launcher, Void Blade). Input actions: `Melee` removed; added `Reload` (R), `SlotSelect1..5` (1-5), `SwitchScroll` (mouse wheel). `PlayerController` forwards new inputs to `WeaponManager` (Reload / EquipSlot / CycleSlot). `WeaponManager` switching + ammo/reload API on `WeaponBase` were already in place from PR A and now wired end-to-end. Scene wiring still required — see checklist below. |
| | |

---

## Notes

- **Assets strategy:** Mix of free Asset Store + custom Blender models
- **Audio sources:** freesound.org, opengameart.org, Pixabay Audio
- **Priority:** Gameplay feel > visuals > polish
- **Key risk:** 3D models are the bottleneck — use primitives early, replace later

## Unity manual setup reminders (do after pulling latest main)

- [ ] **Build Profiles → Scene List → Add Open Scenes**: add `Assets/test.unity` so R-key scene reset works (otherwise LoadScene falls back to -1).
- [ ] **Player → PlayerController → Weapon Sway → Weapon Holder**: drag the weapon GameObject (Sphere or gun model) into this field so weapon tilts on camera pitch.
- [ ] If `moveSpeed` in the inspector still shows the old `6`, set it to `10` manually (serialized scene value overrides script default).

### Weapon System PR A — scene wiring (DONE 2026-04-17, verified playable)

- [x] **Create the Pulse Pistol definition asset.** `Assets/Scripts/Combat/Weapons/Data/Weapons/PulsePistol_Def.asset` with `HitscanFireMode` assigned via custom Inspector dropdown.
- [x] **Add `WeaponManager` component to the Player GameObject.** Owner / cameraTransform / hitMask / ownerHealth references wired.
- [x] **Convert the existing pistol viewmodel into a weapon.** `GenericWeapon` component added with definition + `muzlePoint` child Transform + `ShotTracer` LineRenderer prefab for visible hitscan tracer.
- [x] **Wire the weapon into a slot.** Slot 0 = pistol viewmodel. `defaultSlot = 0`.
- [x] **Remove obsolete fields from PlayerController.** Old combat fields no longer appear in the Inspector after script recompile.
- [x] **PlayerInputActions asset regenerated.** Fire action is `Value`/`Button` for hold-to-fire support.

**Status: verified playable 2026-04-17 — Pulse Pistol fires correctly via new system with visible tracer, no regressions in movement/dash/slide/jump, friendly-fire prevention works, death-halt works.**

### Weapon System PR B — scene wiring (pending, do in Unity Editor)

For each of the 4 new weapons (Scatter Gun slot 1, Void Rifle slot 2, Plasma Launcher slot 3, Void Blade slot 4):

- [ ] Create a child GameObject under `weaponHolder` (primitive cube/sphere placeholders are fine until Blender models are ready).
- [ ] Add `GenericWeapon` component. Drag the matching `*_def.asset` from `Assets/Scripts/Combat/Weapons/Data/Weapons/` into its `Definition` field.
- [ ] Create a child `muzzlePoint` Transform at the tip and wire it into `Muzzle Point`.
- [ ] Drag the viewmodel into the matching `Slots[]` element on the player's `WeaponManager` (Scatter Gun → Slots[1], Void Rifle → Slots[2], Plasma Launcher → Slots[3], Void Blade → Slots[4]).
- [ ] Re-open each new `*_def.asset` in the inspector. If the `Fire Mode` dropdown shows `(None)` after Unity imports the hand-written YAML, re-pick the correct fire-mode subclass via the dropdown (this will assign the SerializeReference cleanly).
- [ ] For `Void_Rifle_def` and `Scatter_Gun_def`, assign `Tracer Prefab` to the existing `ShotTracer` LineRenderer prefab.
- [ ] For `Plasma_Launcher_def`, assign `Projectile Prefab` to the existing `Projectile` prefab under `Assets/test/`.
- [ ] Playtest: keys `1-5` switch slots, mouse wheel cycles (skipping empty slots), `R` reloads finite-ammo weapons, Void Blade (slot 5) swings at the OverlapSphere radius, Plasma projectile travels and damages enemies without harming the player.

## Next session starting point

- Phase 1: **done** ✅ (Movement, Weapons PR A+B, Kill-to-Survive PR A+B — all playtested).
- Phase 2 **PR 1** (Layout + Seed): done ✅, verified in Editor with gizmos (2026-04-20).
- Phase 2 PIVOT'нут 2026-04-20 на **TZ r4** — single procedural arena + run graph с door-choice. BSP r1-r3 код помечен `[DEPRECATED]` и оставлен для diploma reference.
- Phase 2 **PR 2.A** (SingleArenaGenerator + shape / cover / exit planners + size presets + per-arena ceiling 10-25м): **verified 2026-04-21** ✅
- Phase 2 **PR 2.B** (Run Graph + Transitions + fade + door-choice placeholder UI + Victory/GameOver screens + door-opening/lintel/barrier fixes): **verified 2026-04-21** ✅
- Phase 2 **PR 2.C** (Async NavMesh + encounter integration + soft-lock barriers + clear conditions): next.
- TZ: [ARENA_GENERATION_TZ.md](./ARENA_GENERATION_TZ.md) (APPROVED r4).

### Phase 2 PR 2 — Editor verification checklist

- [ ] Open `Assets/test.unity`, select `ArenaDebug` GameObject → context-click its `ArenaDebugGizmos` component → `Generate + Build`.
- [ ] Confirm an `ArenaRoot` child appears with `Room_N_*` subtrees + `Corridors/Corridor_i` subtrees, each `Shell` containing Floor/Ceiling/Wall cubes.
- [ ] Walk in Play mode from the emissive green start marker to the emissive red exit marker — no invisible walls blocking corridors, no holes in floor/walls.
- [ ] Same `config.seed` produces the exact same Hierarchy layout after `Clear Geometry` + `Generate + Build`.
- [ ] `Anchors` subfolders present per room (Corner / Wall / Ceiling / Floor / DoorFrame) and visible in Scene view as empty GOs.
- [ ] Cover pillars appear only in combat rooms, never on door cells.
- [ ] Console has one summary log per generation, no per-cell spam.
