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
- [ ] Create `WeaponBase.cs` abstract class
- [ ] Create `WeaponManager.cs` (switching, inventory)
- [ ] Weapon 1: Pulse Pistol (semi-auto hitscan, unlimited ammo)
- [ ] Weapon 2: Scatter Gun (shotgun, 8 pellets spread)
- [ ] Weapon 3: Void Rifle (full-auto hitscan)
- [ ] Weapon 4: Plasma Launcher (projectile, splash damage)
- [ ] Weapon 5: Void Blade (melee, arc swing)
- [ ] Weapon switching (scroll wheel + number keys 1-5)
- [ ] Ammo system (pickups from enemies)
- [ ] Weapon viewmodels (first-person arms + gun) — Blender

### Kill-to-Survive
- [ ] HP orb drop on enemy kill (5 HP)
- [ ] Glory Kill system (melee staggered enemy → 25 HP)
- [ ] Enemy stagger state (flashing at low HP)
- [ ] Kill streak speed boost (5+ kills in 10s)
- [ ] Remove passive HP regen

---

## Phase 2: Procedural Arena Generation (Weeks 3-4)

### BSP Generator
- [ ] BSP tree data structure
- [ ] Recursive space partitioning (3-5 splits)
- [ ] Room placement within leaf nodes
- [ ] Corridor generation between rooms
- [ ] Floor/wall/ceiling mesh generation
- [ ] Seed-based randomization

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
| 2026-04-15 | GDD v2 created. PROGRESS.md created. Project analyzed. |
| 2026-04-15 | Phase 1 Movement Upgrades done: speed 10 m/s, double jump, slide (Ctrl), full air control, dash rework (2 charges/3s cooldown, works in air), momentum preservation. (PR #10) |
| 2026-04-15 | Phase 1 playtest fixes (PR #11): slide no longer falls through floor (removed controller.height resizing), slide ends correctly on Ctrl release (Button→Value input type), V + Right Shift added as dash keys, shot direction uses camera.forward (bullets no longer curve during fast motion), weapon tilt on camera pitch (Weapon Sway section), scene reset fixed (R key + auto-reload 2s after death, uses active scene buildIndex instead of hardcoded 0). |
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

## Next session starting point

- Phase 1 Movement Upgrades: **done** ✅
- Next on Phase 1: **Weapon System** — `WeaponBase.cs` abstract class, `WeaponManager.cs`, 5 weapons (Pulse Pistol, Scatter Gun, Void Rifle, Plasma Launcher, Void Blade), 1-5 + scroll wheel switching, ammo pickups, viewmodels.
- Melee fix is deferred into the Weapon System rework (Void Blade will replace current broken melee).
