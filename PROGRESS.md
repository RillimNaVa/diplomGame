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

### BSP Generator
- [x] BSP tree data structure *(PR 1, 2026-04-20)*
- [x] Recursive space partitioning (3-5 splits) *(PR 1)*
- [x] Room placement within leaf nodes *(PR 1)*
- [x] Corridor generation between rooms *(PR 1, MST + extras)*
- [ ] Floor/wall/ceiling mesh generation *(PR 2)*
- [x] Seed-based randomization *(PR 1, System.Random sub-streams)*

### Room Types
- [ ] Small arena (15x15)
- [ ] Medium arena (25x25, platforms)
- [ ] Large arena (40x40, multi-level)
- [ ] Boss arena (50x50)
- [ ] Corridors (3-5 wide)

### Verticality
- [ ] Ramps
- [ ] Platforms (elevated areas)
- [ ] Stairs
- [ ] Cover objects (pillars, crates)

### Navigation
- [ ] Runtime NavMesh baking
- [ ] Spawn point placement in generated rooms
- [ ] Portal/door system between arenas

### Biomes
- [ ] Biome 1: Void Station (metal, blue/white)
- [ ] Biome 2: Alien Nexus (organic, purple/red)
- [ ] Material swapping system per biome

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
| 2026-04-20 | Phase 2 specification drafted: added `ARENA_GENERATION_TZ.md` covering BSP layout, room/corridor generation, controlled encounter flow, runtime NavMesh baking, single-scene arena transitions, debug tooling, and performance constraints. |
| 2026-04-15 | GDD v2 created. PROGRESS.md created. Project analyzed. |
| 2026-04-15 | Phase 1 Movement Upgrades done: speed 10 m/s, double jump, slide (Ctrl), full air control, dash rework (2 charges/3s cooldown, works in air), momentum preservation. (PR #10) |
| 2026-04-15 | Phase 1 playtest fixes (PR #11): slide no longer falls through floor (removed controller.height resizing), slide ends correctly on Ctrl release (Button→Value input type), V + Right Shift added as dash keys, shot direction uses camera.forward (bullets no longer curve during fast motion), weapon tilt on camera pitch (Weapon Sway section), scene reset fixed (R key + auto-reload 2s after death, uses active scene buildIndex instead of hardcoded 0). |
| 2026-04-16 | Phase 1 Weapon System PR A: new modular framework under `Assets/Scripts/Combat/Weapons/` — `WeaponEnums`, `WeaponContext`, `WeaponDefinition` (ScriptableObject with `[SerializeReference] FireModeBase`), `FireModeBase` + `HitscanFireMode`, `WeaponBase` (runtime state inline), `GenericWeapon`, `WeaponManager` (slots[5], events, owner-death halt). Added `Editor/FireModeReferenceDrawer.cs` for Inspector type-picker dropdown. PlayerController stripped of all combat (Shoot/MeleeAttack/PlayShootAnim/HasAnimatorParameter removed); OnFire forwards to WeaponManager.SetFireHeld; OnMelee removed (returns as Void Blade in PR B). Fire input action changed Button → Value (Button) for hold-to-fire. Verified playable 2026-04-17 — Pulse Pistol fires correctly via new system with visible tracer. |
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
- Phase 2 **PR 2** (Physical Build — universal procedural blockout, flat only): **next**.
- TZ: [ARENA_GENERATION_TZ.md](./ARENA_GENERATION_TZ.md) (APPROVED r2).
