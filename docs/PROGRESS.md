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

### PR 2.C — NavMesh + Encounter Integration (verified 2026-04-22)
- [x] Async `NavMeshSurface.UpdateNavMesh` bake *(ArenaNavMeshController, uses com.unity.ai.navigation 2.0.9)*
- [x] `GameManager.SetSpawnPoints / BeginEncounter / EndEncounter` API *(encounter mode gated behind `useEncounterMode` flag — legacy wave loop preserved)*
- [x] Encounter trigger component *(EncounterTrigger.cs scaffolded; current flow bypasses it — `EncounterController.BeginEncounter` is called directly from ArenaFlowController after fadeOut, since teleport places player inside arena)*
- [x] Soft-lock barrier on exit doors *(emissive cube + collider, toggled by EncounterController.Open/Close; `ArenaBuildMaterials.barrier` emissive-orange)*
- [x] Clear conditions: KillAll / ReachExit / None *(ReachExit hook via `EncounterController.FinishByReach`, Timer deferred to PR 2.D)*
- [x] `SimpleEnemyAI.isOnNavMesh` guard — prevents `SetDestination on inactive agent` during first frames of runtime bake
- [x] Editor verification — wire `useEncounterMode=true` on GameManager, play run through 5 arenas, confirm: (a) enemies spawn on NavMesh, (b) barriers hold until last enemy dies, (c) ExitDoorTrigger fires only after clear

### PR 2.D — Verticality + Biomes + Balance (verified 2026-04-22)
- [x] `ArenaVerticalityPlanner` (platforms/ramps for Parkour arenas)
- [x] `BiomeDefinition` + 2 biome presets
- [x] Elite / Parkour / Shop / Rest type profiles
- [x] Difficulty scaling по `arenaIndex`
- [x] Debug UI: seed display, arena index, biome id

### PR 2.E — Visual Style Pass For Pre-Defense (closed 2026-04-24)
- [x] Strengthen biomes into full material/style sets (Void Station / Alien Nexus)
- [x] Add architectural details in builder (ribs / beams / door frames / borders / corner pillars)
- [x] Add floor patterns / panels / center motifs
- [x] Add rule-based decorative props by room zones
- [x] Add atmosphere pass (fog / emissive accents / background silhouettes / grading)

### PR 2.F — Visual Fidelity Pass (code landed 2026-04-25, Unity verify pending)
- [x] Enable camera `m_RenderPostProcessing` + SMAA in `Assets/test.unity` *(was off — main "robloxness" cause)*
- [x] Bump SSAO intensity / radius in `PC_Renderer.asset`
- [x] Per-slot `bumpScale` / `parallaxStrength` / detail-map fields on `BiomeSurfaceDefinition`, wired into `ArenaBuildMaterials.ApplyTextureSet` (`_BumpScale`, `_Parallax`, `_DetailAlbedoMap` + `_DETAIL_MULX2`)
- [x] `ArenaPostProcessingController` — runtime Global Volume (Bloom + ColorAdjustments + Vignette + ACES Tonemapping, priority 100); biome `colorFilter` replaces ambient-tint hack
- [x] Realtime `ReflectionProbe` per arena (one-shot bake at center on build)
- [x] Runtime URP Point Lights on exit markers + atmosphere pylons (color/intensity/range from biome)
- [x] Fog `Color.Lerp` bug fixed (`fogStrength` is now the lerp t, not a 0.92 floor)
- [ ] Unity Editor visual verify across 5-arena run
- [ ] Per-biome inspector tuning (bloom / saturation / vignette / colorFilter)

### PR 2.G — Anti-stretch + lighting fill (code landed 2026-04-25, Unity verify pending)
- [x] `WorldUVScaler` + `WorldUVDensityRegistry` — per-instance MaterialPropertyBlock that derives `_BaseMap_ST` (and `_BumpMap_ST` / occlusion / metallic / emission ST) from each box's `lossyScale`, kills "Roblox" stretching on big floors and walls
- [x] `BuildUtils.SpawnBox` auto-attaches `WorldUVScaler` to every spawned cube
- [x] `ArenaBuildMaterials.CreateSurface` registers per-material tile density derived from `slot.textureScale` (×0.25 → tiles/meter); marker materials get a default density too; `enableInstancing = true` on all materials
- [x] `ArenaBuilder.BuildSingleEdgeStrips` — thin emissive strips along floor-to-wall seams (skips door openings) for "panel-light" feel
- [x] `ArenaBuilder.BuildSingleFillLights` — one center + four quadrant point lights at 0.85·wh height; intensity ≈ 0.45·biome.accentLightIntensity, range ≈ arena half-span, color = `Lerp(neutralWhite, biome.ambientTint, 0.35)`
- [x] `ArenaBuilder.SpawnCeilingLamp` — visible ceiling fixture (dark bracket flush to ceiling + emissive panel hanging just below) co-spawned at every fill-light position; player sees *where* the light comes from
- [x] `PC_RPAsset.asset`: `m_AdditionalLightsPerObjectLimit` 4→8 (so fill + exit + pylon lights coexist), shadow distance 50→60
- [ ] Unity verify: arena no longer feels dim, floor/wall textures don't stretch, additional-light count stays under per-camera cap

---

## Phase 3: Enemy AI (Weeks 5-6)

> Master spec: [ENEMY_AI_TZ.md](./ENEMY_AI_TZ.md) — Phase 3 scoped as four main enemy roles, state-machine AI, spawn composition, readability, and later pooling.

### PR 3.A — State Machine Base + Drone/Crawler (verified 2026-04-27)
- [x] `EnemyRole`, `EnemyAIState`, `IEnemyTargetReceiver`, `EnemyData` SO, `EnemyBrainBase`, `MeleeEnemyBrain` *(Assets/Scripts/Combat/Enemies/AI + Data, 2026-04-27)*
- [x] `SimpleEnemyAI` migrated to `IEnemyTargetReceiver`; throttled `SetDestination`; per-attack `Debug.Log` removed *(2026-04-27)*
- [x] `GameManager` resolves `IEnemyTargetReceiver` instead of `SimpleEnemyAI` directly (no fallback branch) *(2026-04-27)*
- [x] `dotnet build Assembly-CSharp.csproj` clean (zero CS errors/warnings) *(2026-04-27, verified after temp-injecting new files into csproj)*
- [x] Author `Drone.asset` and `Crawler.asset` `EnemyData` SOs in Unity Editor
- [x] Create `Enemy_Drone.prefab` / `Enemy_Crawler.prefab` variants using `MeleeEnemyBrain` + the two SOs
- [x] Editor verification: legacy wave mode + encounter mode both still spawn enemies; barriers behave; HP orbs/stagger/glory-kill still trigger; no `SetDestination on inactive agent`

### PR 5.B — Brute Slam Shaders (code landed 2026-04-30, Editor playtest pending)
- [x] **SlamWarning.shader** (`Assets/Shaders/SlamWarning.shader`) — HLSL polar-coords decal animated by `_Progress`. Layers: outer ring + pulsing inner X-cross + clockwise sweep arc (countdown) + rotating tick marks. Uses `unity_ObjectToWorld` to derive pivot/axes/scale so polar coords work on the runtime cylinder primitive without needing UV authoring
- [x] **SlamShockwave.shader** (`Assets/Shaders/SlamShockwave.shader`) — HLSL polar-coords impact effect animated by `_Progress` 0 → 1.05. Layers: hot core (shrinks + dims as ring expands) + primary expanding ring at radius=`_Progress` + trailing secondary ring + N radial lightning cracks (per-finger angle jitter via `hash11(slot)`, length jitter via `hash11(slot+offset)`). Overall fade after `_Progress > 0.7`
- [x] **BruteSlamDecal.cs refactored** — replaces runtime URP Lit transparent material with `VoidSurvivor/SlamWarning` shader; animates `_Progress` 0 → 1 over `telegraphTime`. Falls back to legacy alpha+emission ramp if shader is missing
- [x] **SlamImpactRing.cs refactored** — replaces runtime URP Lit transparent material with `VoidSurvivor/SlamShockwave` shader; disc mesh stays at full slam radius and the shader animates expansion via `_Progress`. Falls back to legacy scale-up + emission ramp if shader is missing
- [x] `dotnet build Assembly-CSharp.csproj` clean (0/0)
- [ ] Editor playtest: (a) Brute wind-up shows the new warning rune (rotating ticks, pulsing inner cross, clockwise sweep filling the outer band), (b) Brute slam impact shows hot core + expanding shockwave ring + radiating lightning cracks instead of the simple expanding orange disc

### PR 5.C — Combat / Environment Feedback Polish (code landed 2026-05-01, Editor playtest pending)
- [x] `ImpactFXSystem` — runtime muzzle flash + FIFO bullet-impact decals with `BulletImpactDecal.shader`
- [x] `PickupGlow.shader` + `HealthPickupGlow` — HP orbs get a pulsing glow automatically from `HealthPickup`
- [x] `ExitPortal.shader` + `ArenaBuildMaterials.MakeExitPortal` — exit markers read as portal surfaces instead of flat emissive panels
- [x] `AmbientDustMotes` — biome-tinted dust particles spawned per generated arena
- [x] `LampFlicker` — ceiling lamps flicker/dim on Brute slam or nearby enemy projectile impact; uses horizontal distance and also dims the visible lamp panel
- [x] `DamageDirectionHUD` — player hit direction arcs driven by `Health.TakeDamage(damage, sourcePosition)`
- [x] `SpeedBurstFeedback` — subtle dash/slide FOV kick, edge tint, and camera-local speed-line particles
- [x] `EnemyDeathShards` — short-lived debris + flash-light on enemy death
- [x] Ranged enemy strafing + lightweight enemy separation to reduce stacking
- [x] Reduced ceiling lamp overexposure after playtest feedback: smaller panels, lower fill-light intensity, lower emissive intensity
- [x] ParticleSystem runtime setup warnings fixed (`StopEmittingAndClear` + `playOnAwake = false` before changing duration)
- [x] `dotnet build Assembly-CSharp.csproj --no-restore` clean (0/0)
- [ ] Editor playtest: verify dash/slide effect remains subtle, lamps are not overexposed, lamp flicker is visible during Brute slam/plasma wall hits, HP glow/exit portal/bullet decals/muzzle flash work, and no ParticleSystem warnings return

### Planned UI HUD Polish Pass (captured 2026-05-01)
- [ ] See `docs/UI_HUD_POLISH_PLAN.md`
- [ ] Replace legacy combat HUD with HP block, ammo/current weapon, dash charges, crosshair, compact enemy counter, restyled damage direction indicator, and heal feedback
- [ ] Remove legacy timer from the main combat HUD
- [ ] Keep arena debug info visible for now

### PR 5.A — Visual Polish Pass (code landed 2026-04-29, Editor playtest pending)
- [x] **EnemyDissolve shader** (`Assets/Shaders/EnemyDissolve.shader`) — HLSL custom shader with hash-based 3D world-space noise + clip threshold + emissive burning edge. Lit by `GetMainLight()` Lambert + ambient (no full PBR — overkill for a 1-second death animation)
- [x] **EnemyDissolve C#** — listens to `Health.onDeath`, swaps each Renderer's sharedMaterials to per-instance dissolve materials (copying `_BaseColor` / `_BaseMap` / `_EmissionColor` from the source), ramps `_DissolveAmount` 0 → 1.05 over 1.0s. `ResetForPool` restores original sharedMaterials so pool reuse is clean. Auto-attached by `EnemyBrainBase.Awake`
- [x] **StaggerOutline shader** (`Assets/Shaders/StaggerOutline.shader`) — inverted-hull outline pass (`Cull Front`, vertex inflated along normal by `_OutlineWidth`), HDR pulsing color
- [x] **StaggerOutline C#** — listens to `EnemyStagger.OnStaggerChanged`, appends an outline material as an extra slot on every renderer's `sharedMaterials`. Removes on stagger end / pool return / disable. Auto-attached
- [x] 2026-04-30 post-review fix: `StaggerOutline.OnDisable` and `ResetForPool` restore original `sharedMaterials` unconditionally, so a staggered enemy cannot keep the outline material slot after pooling
- [x] **ForceField shader** (`Assets/Shaders/ForceField.shader`) — animated hex-grid + vertical scroll waves + Fresnel rim + slow color pulse. Transparent, two-sided, ZWrite off
- [x] **ArenaBuildMaterials.MakeForceField** — replaces `MakeEmissive` for the soft-lock barrier; falls back to emissive cube if the shader is missing. Soft-lock barriers now read as energy fields, not orange boxes
- [x] **SpitterChargeBeam C#** (`Assets/Scripts/Combat/Enemies/AI/SpitterChargeBeam.cs`) — LineRenderer from muzzle to player during the Telegraph state; thickness + brightness ramp accelerates toward the firing frame; auto-attached by `RangedEnemyBrain.Awake`. Cleaned up on Stagger / Death / pool disable
- [x] **EnemyProjectile auto-built TrailRenderer** — if no `TrailRenderer` exists on the projectile prefab at Awake, builds a default cyan plasma trail (0.25s, 0.18→0.02 width, additive URP particle material). Existing `EnemyProjectile.ResetForPool` already calls `TrailRenderer.Clear()` on rent so the trail does not jump from previous shot's last position
- [x] **SpawnTelegraph C#** — pre-spawn warning marker at the chosen spawn point. Floor circle (rotating + emissive pulse, scales 0.4× → 1.05×) + vertical beam (cylinder from floor to ~5.5m, alpha + emission ramp). `SpawnTelegraph.SpawnAt(worldPos, duration)` static factory. Auto-destructs after `duration + 0.25s` tail
- [x] **GameManager spawn coroutine integration** — `SpawnEnemyWithTelegraph` and `SpawnEncounterEnemyWithTelegraph` wrappers pre-pick the spawn point, spawn telegraph, wait `spawnTelegraphDuration` (default 0.7s), then rent the enemy at that exact point. `useEncounterMode` and legacy wave both use the new path
- [x] `dotnet build Assembly-CSharp.csproj` clean (0/0)
- [ ] Editor playtest: (a) killed enemies dissolve over 1.0s with a glowing burn edge, (b) stagger pulses now show an HDR outline halo around the silhouette, (c) soft-lock barriers covering exits read as animated energy fields (hex grid + vertical waves + rim glow), (d) Spitter shows a visible thin laser beam from muzzle to player during its 0.7s telegraph, (e) Spitter projectiles leave a plasma trail behind them, (f) every enemy spawn is preceded by a 0.7s telegraph (floor circle + vertical beam) at the exact spawn point

### PR 4.A — Combat Feel Pass (code landed 2026-04-29, Editor playtest pending)
- [x] `HitFlash` — auto-attached emissive white flash on `Health.onTakeDamage` (0.07s, ease-out cubic), MPB-based so no runtime material instances
- [x] `CameraShake` — auto-attaches to `Camera.main`, additive Perlin-noise offset on top of `PlayerController.HandleMovement` writes via LateUpdate, trauma decay 4.5/sec, `Instance.AddTrauma(amount)` API
- [x] 2026-04-30 post-review fix: `CameraShake` now removes the previous frame's position/roll shake before gameplay Update and before applying the next LateUpdate shake, preventing permanent camera drift
- [x] `PlayerHitFeedback` — auto-added to player by `GameManager.ResolveReferences`; on `playerHealth.onTakeDamage` triggers (a) red vignette pulse via new `ArenaPostProcessingController.PulseDamageVignette` + (b) camera shake scaled by damage / `shakeReferenceDamage` (default 18)
- [x] `EnemyDeathBurst` — runtime ParticleSystem burst on `Health.onDeath` (24 particles, lifetime 0.9s, color from `EnemyData.telegraphColor`), detached from enemy so the burst survives the pool return
- [x] `SlamImpactRing` — runtime expanding emissive ring (0.4× → 1.05× radius over 0.32s) spawned by `BruteEnemyBrain.TickAttack` at the slam frame; also calls `CameraShake.AddTrauma` with distance falloff
- [x] `SpawnWarpIn` — auto-attached scale-up + emissive pulse on every `OnEnable` (covers fresh Instantiate AND pool reuse — `PooledEnemy.PrepareForReuse` re-fires OnEnable). 0.55× → 1.0× scale over 0.4s, smooth-step
- [x] All 5 components auto-attached in `EnemyBrainBase.Awake`, so existing prefabs (Drone/Crawler/Spitter/Brute) get the upgrade with no Editor work
- [x] `dotnet build Assembly-CSharp.csproj` clean (0/0)
- [ ] Editor playtest: hit flash visible on every enemy on damage; player damage shows red vignette + camera shake; killed enemies emit a short particle burst; Brute slam shows expanding ring + camera kick; every spawn (fresh + pooled) has 0.4s warp-in animation

### PR 4.B — Crawler Leap Attack (code landed 2026-04-29, Editor wiring pending)
- [x] `LeapMeleeEnemyBrain` extends `EnemyBrainBase` — replaces `MeleeEnemyBrain` for Crawler; state flow Move → Telegraph (leap) → Attack (kinematic arc) → Recover. Three ranges: dist > leapMaxRange walks; leapMinRange..leapMaxRange triggers leap; <attackRange falls through to regular swing
- [x] Leap mechanic: 0.45s telegraph wind-up → snapshot landing position → kinematic transform translate at 14 m/s for up to 0.5s → apply damage in 1.9m radius around Crawler at impact. Player can dodge by moving perpendicular during travel (snapshot is fixed)
- [x] Disables NavMeshAgent during travel so SetDestination doesn't fight kinematic writes; re-enables + warps + ResetPath on landing. Stagger / death properly clean up agent state
- [x] Slot manager + telegraph flash + fair-spawn delay all behave identically to `MeleeEnemyBrain`
- [x] `dotnet build Assembly-CSharp.csproj` clean
- [ ] Editor: swap `MeleeEnemyBrain` → `LeapMeleeEnemyBrain` on `Assets/Prefabs/Enemy_Crawler.prefab` (drag in `Crawler.asset` SO again to refill the `data` field). Default brain values are tuned for the existing Crawler stats; no SO changes needed

### PR 2.H1 — Hand-Authored Cover Structures (code landed 2026-04-29, Editor playtest pending)
- [x] `StructureDefinition` POCO + `StructureBoxPart` (axis-aligned local box, slot-tagged) + `StructurePlacement` runtime struct + `StructureSlot` enum (Wall/Cover/Trim/Floor/Decor/EmissiveAccent)
- [x] `BuiltInStructures` factory ships 4 baseline silhouettes:
  - **Bunker** — 3-walled U-shaped half-shelter, footprint 3×3 cells
  - **Sandbag Line** — 4 chest-high cover boxes + tall corner post, 4×1 cells
  - **Pillar Cluster** — 4 columns + low debris ring, 3×3 cells
  - **Sniper Nest** — 2-tier raised platform with horizontal firing slit, 3×2 cells (combat/elite/boss only)
- [x] `ArenaStructurePlanner.Plan` — deterministic via dedicated `structureRng` (`seed ^ 0x8855`); weighted pick across category-eligible pool; quantized 90° yaw; min 3-Manhattan distance from entry; min 3.5 macro-cells from any exit; reserves footprint cells before cover so `ArenaCoverPlanner` fills around structures
- [x] `SingleArenaGenerator` — adds `structureRng` sub-stream, plans structures **between verticality reservation and cover** so cover and combat-spawns avoid overlaps. Gates structures behind `AllowsStructures` (Combat/Elite/Boss/Parkour only — keeps Start/Shop/Rest open and calm)
- [x] `ArenaTypeProfile.structureBudget` (Range 0..5, default 2) — per-profile knob for how many structures spawn
- [x] `ArenaRoomData.structurePlacements` list
- [x] `ArenaBuilder.BuildSingleStructures` — looks up def by `structureId` from `BuiltInStructures.All()`, spawns each `StructureBoxPart` via `BuildUtils.SpawnBox` so `WorldUVScaler` + per-biome materials + instancing all apply for free; `ResolveStructureMaterial` maps `StructureSlot` → `ArenaBuildMaterials` slot
- [x] Zero `UnityEngine.Random` in any new procedural code (TZ contract preserved)
- [x] `dotnet build Assembly-CSharp.csproj` clean
- [ ] Editor playtest: Combat/Elite arenas now show 1-3 visible bunkers/pillars/sandbag-lines/sniper-nests integrated into cover; structures don't overlap exits, entry, platforms, or each other; arenas remain navigable (NavMesh bake reaches the rest of the room); deterministic — same seed → same structure layout

### PR 3.F — Enemy + Projectile Pooling (code landed 2026-04-29, Editor playtest pending)
- [x] `EnemyPool` (scene-singleton, auto-creates) — per-prefab `Stack<GameObject>` rent/return; tidy re-parenting under the pool root on Return; null-entry skip on Rent for scene-reload safety
- [x] `PooledEnemy` (`[DefaultExecutionOrder(200)]` so Awake captures post-`EnemyBrainBase.Awake` `health.maxHealth`) — caches `baselineMaxHealth`, listens to its own `Health.onDeath`, cancels `Health`'s `Invoke(Disable)` and schedules `EnemyPool.Return` after a 1.5s grace window so loot/glory-kill have time to complete
- [x] `Health.ResetForPool` + `Health.CancelAutoDisable` — restore `currentHealth = maxHealth` and re-fire `onHealthChanged`; cancel the pending 1s SetActive(false) when an enemy is pool-managed (TZ §9 PR 3.F reset rules)
- [x] 2026-04-30 post-review fix: `Health.CancelAutoDisable` now suppresses the auto-disable even when called during `onDeath` before `Die()` schedules `Invoke(Disable)`; `PooledEnemy` now splits inactive component reset from post-enable `NavMeshAgent.Warp`
- [x] `EnemyStagger.ResetForPool` — clear `IsStaggered`, restore black emission on cached material instances, fire `OnStaggerChanged(false)` so brain transitions out of `Staggered`
- [x] `EnemyLootTable.ResetForPool` — clear the one-shot `rolled` guard so the recycled enemy can drop loot again on its next death
- [x] `GameManager.SpawnEnemy` + `SpawnEncounterEnemy` rent through `EnemyPool.Instance.Rent`; `Health.onDeath` listener uses Remove+Add to avoid duplicate subscriptions across rents (TZ §10 acceptance: kills count exactly once)
- [x] `EnemyProjectilePool` (scene-singleton, auto-creates) — same per-prefab pattern for Spitter projectiles
- [x] `EnemyProjectile.ResetForPool` — clear owner / damage / speed / direction; cache `TrailRenderer[]` and `Clear()` each on rent so a recycled shot does not draw a trail from the previous muzzle
- [x] `RangedEnemyBrain.SpawnProjectile` rents through `EnemyProjectilePool.Instance.Rent` instead of `Instantiate`
- [x] `EnemyBrainBase` — fair-spawn init moved out of `Start` (only runs once per instance lifetime) into a per-`OnEnable` first-frame check, so pooled rents re-evaluate the spawn-near-player delay
- [x] `dotnet build Assembly-CSharp.csproj` clean (0/0) — verified 2026-04-29 via temp-injected csproj
- [ ] Editor playtest: long run (5-arena loop ×2) does not accumulate disabled enemy/projectile GameObjects in the Hierarchy; kill counts still match enemies killed; HP-orb still drops on every kill; recycled enemies start at full HP without stuck red stagger pulse; fair-spawn delay still triggers on close spawns; projectile trails do not jump from the previous shot's last position

### PR 3.E — Combat Readability + Active Attack Slots (verified 2026-04-29 by user)
- [x] `ActiveAttackSlotManager` (scene-singleton, auto-creates) — caps melee 3 / ranged 3 / heavy 1 / special 1; `KindForRole` maps `EnemyRole` → `AttackSlotKind`; idempotent `TryAcquire` / multi-set `Release` (TZ §8.3)
- [x] `EnemyBrainBase` integration — `TryAcquireAttackSlot()` gate before Move → Telegraph; `ReleaseAttackSlot()` on Recover → Move, OnDisable, Stagger, Death
- [x] `TelegraphFlash` — emissive pulse on every Renderer in the enemy hierarchy via MaterialPropertyBlock (no runtime material instances); ramp toward impact frame; auto-attached by `EnemyBrainBase.Awake` if not present (TZ §8.1)
- [x] Per-enemy `EnemyData.telegraphColor` (HDR) drives the pulse so Drone/Crawler/Spitter/Brute can read distinct
- [x] `BruteSlamDecal` — runtime-built flat cylinder primitive at the slam origin, biome-agnostic emissive orange, alpha+emission ramp toward impact; `Show(duration)` on Telegraph, `Hide()` on Attack/Recover/Stagger/Death (TZ §6.4 + §8.1 Brute warning circle)
- [x] Fair-spawn delay — `EnemyData.fairSpawnDistance` + `fairSpawnDelay`; if player closer than threshold at spawn, brain holds in `Spawn` state with telegraph pulse before transitioning to Move (TZ §8.4)
- [x] `dotnet build Assembly-CSharp.csproj` clean (zero CS errors/warnings) — verified 2026-04-28 via temp-injected csproj
- [ ] Editor playtest: many alive enemies → only 3 melee + 3 ranged + 1 heavy can resolve attacks at once; Brute decal visible during 0.9s wind-up; close spawn shows ~0.6s warning pulse before chase; HP-orb / stagger / glory-kill / barrier-open contracts intact

### PR 3.D — Spawn Composition (verified 2026-04-28)
- [x] `EnemySpawnEntry` (Serializable) + override-rule accessors (Resolved* properties: value > 0 overrides EnemyData, 0 inherits)
- [x] `EnemySpawnProfile` SO with budget curve (`baseBudget` + `budgetPerArenaIndex`), variety cap (`maxEnemyTypesPerEncounter`), bad-combination guards (`maxTanks`, `maxRanged`)
- [x] `EnemySpawnComposer` static utility — eligibility filter by arenaIndex, weighted distinct role pick (1 in arena 0, 2 in arena 1, 3 in arena 2+), budget-spending loop with maxAlive + role caps, deterministic via optional `System.Random`, `Debug.LogWarning` on every fallback path with reason (TZ §7.4 Revision)
- [x] `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, ...)` overload; legacy `BeginEncounter(int count, ...)` preserved
- [x] `EncounterController.arenaIndex` + `spawnProfile` fields — single owner of arenaIndex per TZ §7.4 Revision; falls back to legacy single-prefab path if profile is null
- [x] `ArenaTypeProfile.spawnProfile` field — per-arena-type composition (Combat, Elite, Boss can each have their own profile)
- [x] `ArenaFlowController.SetupEncounter` propagates `node.arenaIndex` and `profile.spawnProfile` into the runtime EncounterController
- [x] `dotnet build Assembly-CSharp.csproj` clean
- [x] Author baseline `SpawnProfile_Combat.asset` with Drone/Crawler/Spitter/Brute entries
- [x] Wire profile reference into `Arena_Combat_M` and `Arena_Elite_L` `ArenaTypeProfile` assets in Editor
- [x] Editor verification: arenaIndex 0 spawns only Drone/Crawler; arenaIndex 1+ introduces Spitter; arenaIndex 2+ may introduce Brute (≤1); fallback warning fires when profile unset *(user-confirmed 2026-04-28)*

### PR 3.C — Station Brute (verified 2026-04-27)
- [x] `BruteEnemyBrain` *(Move → Telegraph → Attack (area slam) → Recover, FaceTarget during wind-up)*
- [x] Slam damage via `Physics.OverlapSphereNonAlloc` at impact frame using `EnemyData.slamRadius` + `slamHitMask`; deduped by `Health` so player's parent + child colliders don't apply damage twice
- [x] Editor Gizmo (`OnDrawGizmosSelected`) draws slam radius — orange while telegraphing, red baseline
- [x] `dotnet build Assembly-CSharp.csproj` clean
- [x] Author `Brute.asset` `EnemyData` SO (Role: Tank, slamRadius/slamHitMask, **maxAlive=1 mandatory until PR 3.E slot manager**)
- [x] Author `Enemy_Brute.prefab` variant of `Enemy.prefab` with `BruteEnemyBrain` + Brute SO
- [x] Editor verification: Brute slow approach, ~0.9s telegraph readable, escape by leaving slam radius works, only one Brute alive in normal encounters

### PR 3.B — Plasma Spitter / Sentinel (verified 2026-04-27)
- [x] `RangedEnemyBrain` *(Move/Reposition/Telegraph/Attack/Recover with hysteresis band around `preferredDistance`, throttled LoS raycast, FaceTarget during Telegraph)*
- [x] `EnemyProjectile` *(owner-filtered trigger collider, manual movement to avoid Rigidbody tunneling, passes through other enemies, damages first non-owner Health, lifetime auto-destruct — pooling deferred to PR 3.F)*
- [x] LoS check refuses Telegraph entry without clear raycast and re-checks at Attack frame so wall-dashing the player cancels the shot mid-windup
- [x] `dotnet build Assembly-CSharp.csproj` clean
- [x] Author `Spitter.asset` `EnemyData` SO (role: Ranged, projectilePrefab assigned, preferredDistance, projectileSpeed)
- [x] Author `EnemyProjectile.prefab` (SphereCollider trigger + visual + `EnemyProjectile` script)
- [x] Author `Enemy_Spitter.prefab` variant of `Enemy.prefab` with `RangedEnemyBrain` + Spitter SO
- [x] Editor verification: Spitter holds preferred distance, does not shoot through walls, projectiles dodgeable

### Enemy Types (full PR 3 set)
- [ ] Drone (fodder): low-HP melee mass / drop source — code via `MeleeEnemyBrain`, needs SO + prefab in Editor
- [ ] Crawler (chaser): faster melee pressure with telegraph + recovery — code via `MeleeEnemyBrain`, needs SO + prefab in Editor
- [ ] Plasma Spitter / Sentinel (ranged): keeps distance, slow dodgeable projectiles — code landed PR 3.B, needs SO + prefab in Editor
- [ ] Station Brute (tank): high HP, telegraphed area slam, space control — PR 3.C
- [ ] Optional Gravity Node (zoner): deferred until the four-role core is stable

### AI Improvements
- [ ] Strafing behavior
- [ ] Group coordination (spread out, don't stack)
- [ ] Ranged enemy projectiles (dodgeable)
- [ ] Stagger state (low HP → flashy, glory-killable)
- [ ] Death effects (dissolve/explode)

### Spawning
- [ ] Wave composition per arena difficulty
- [ ] Enemy type introduction (Drone/Crawler→Spitter→Brute)
- [x] Object pooling for enemies *(PR 3.F, 2026-04-29 — code landed; covers projectile pooling too)*
- [ ] HP scaling per arena number (+5% baseline; damage scaling deferred by default)

---

## Phase 3.5: Arena Complex Prototype (CANCELLED 2026-04-30)

> Direction cancelled by user on 2026-04-30 after a short prototype attempt. Do not resume Arena Complex / Connected Arena Rooms unless the user explicitly reopens the idea.

Decision: keep the current single-arena `RunController` / `ArenaFlowController` pipeline as the main procedural arena architecture. Larger connected multi-room arenas are out of scope for the diploma prototype for now.

### Cancelled Scope
- [x] Cancel PR 3.5.A/B/C Arena Complex direction
- [x] Remove `Assets/Scripts/ProceduralArena/Complex/`
- [x] Remove `docs/ARENA_COMPLEX_TZ.md`
- [x] Keep existing single-arena mode as the only active procedural arena flow

Rationale: the connected-room prototype adds architecture/runtime risk and testing cost that is not justified right now. The current single-arena run loop is already functional and better aligned with the near-term diploma deadline.

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

| 2026-04-30 | **Post-review runtime bugfix pass** - fixed four findings from the Claude/Codex review: pooled enemies now return to `EnemyPool` instead of being disabled by `Health`, stagger outline material slots are restored on disable/reset, camera shake no longer accumulates permanent local-position drift, and pooled `NavMeshAgent.Warp` runs only after `SetActive(true)`. `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings). Unity Editor playtest is still needed for the PR 3.F / PR 5.A lifecycle. |

| Date | What was done |
|------|---------------|
| 2026-04-30 | **Arena Complex / PR 3.5 cancelled.** User decided to drop the connected-room Arena Complex direction. Removed the prototype `Assets/Scripts/ProceduralArena/Complex/` module and `docs/ARENA_COMPLEX_TZ.md`; restored `SoftLockBarrier` to the regular exit-barrier behavior. The active procedural arena architecture remains the existing single-arena `RunController` / `ArenaFlowController` pipeline. |
| 2026-04-30 | **PR 5.B — Brute Slam Shaders.** PR 5.A's `BruteSlamDecal` and `SlamImpactRing` looked weak next to the new dissolve / outline / force-field shaders, so swapped both to dedicated HLSL shaders. New `Assets/Shaders/SlamWarning.shader` — polar-coords decal animated by `_Progress`: outer ring + pulsing inner X-cross + clockwise sweep arc (acts as countdown — fills the outer band as wind-up progresses) + 12 rotating tick marks. New `Assets/Shaders/SlamShockwave.shader` — polar-coords impact effect animated by `_Progress` 0 → 1.05: hot core (shrinks + dims as ring expands) + primary expanding ring at radius=`_Progress` + trailing secondary ring + N radial lightning cracks (per-finger angle jitter via `hash11(slot)`, per-finger length jitter via `hash11(slot+17.31)`); overall fade kicks in after `_Progress > 0.7`. Both shaders use `unity_ObjectToWorld` to derive pivot / axes / horizontal scale, so polar coordinates work on the runtime cylinder primitive without authored UVs. `BruteSlamDecal.cs` refactored to detect `VoidSurvivor/SlamWarning` and feed it `_Progress`; falls back to the legacy URP Lit alpha+emission ramp if the shader is missing. `SlamImpactRing.cs` refactored similarly — disc mesh now stays at full slam radius and the shader animates expansion via `_Progress` instead of `transform.localScale`. Both fallback paths are byte-for-byte the original PR 4.A behavior so a stripped build never crashes. `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings). Pending in Editor: confirm the new wind-up rune and shockwave look distinctly more "real" than the old orange discs. |
| 2026-04-29 | **PR 5.A — Visual Polish Pass (HLSL shaders + spawn telegraph + charge beam).** Three new HLSL shaders under `Assets/Shaders/`: `EnemyDissolve.shader` (hash-based 3D world-space noise + clip threshold + emissive burning edge, simple Lambert + ambient lighting), `StaggerOutline.shader` (inverted-hull `Cull Front` outline pass, vertex inflated along normal, HDR pulsing color), `ForceField.shader` (animated hex-grid via `hexDistance(p)` + vertical scroll waves + Fresnel rim + slow color pulse, transparent two-sided). Companion runtime components: `EnemyDissolve.cs` (auto-attached by `EnemyBrainBase.Awake`; on `Health.onDeath` builds per-renderer instance materials with the dissolve shader, copies `_BaseColor` / `_BaseMap` / `_EmissionColor` from source, ramps `_DissolveAmount` 0 → 1.05 over 1.0s; `ResetForPool` restores original sharedMaterials), `StaggerOutline.cs` (auto-attached; on `EnemyStagger.OnStaggerChanged(true)` appends an outline material slot to every renderer's `sharedMaterials`, removes on end / pool return). `ArenaBuildMaterials.MakeForceField` replaces the plain `MakeEmissive` for the soft-lock barrier — falls back to emissive cube if the ForceField shader is missing. `SpitterChargeBeam.cs` (auto-attached by `RangedEnemyBrain.Awake`) draws a LineRenderer from muzzle to player during Telegraph; thickness + brightness ramp accelerates toward firing frame, ends on Attack / Stagger / Death / pool disable. `EnemyProjectile.cs` got `autoBuildTrail` — if no TrailRenderer exists at Awake, builds a cyan plasma trail (0.25s, 0.18→0.02 width, additive URP particle material). `SpawnTelegraph.cs` is a runtime-built marker spawned at every encounter spawn point ~0.7s before the enemy materializes — flat rotating emissive floor circle (0.4× → 1.05× scale) plus a thin vertical cylinder beam from the floor to ~5.5m with alpha + emission ramp; auto-destructs after duration + 0.25s tail. `GameManager` got `spawnTelegraphDuration` (default 0.7s) plus `SpawnEnemyWithTelegraph` / `SpawnEncounterEnemyWithTelegraph` coroutine wrappers — both legacy wave loop and encounter mode pre-pick the point, spawn telegraph, wait, then rent at the exact point so the player sees the spawn coming. `PooledEnemy.PrepareForReuse` calls `EnemyDissolve.ResetForPool` and `StaggerOutline.ResetForPool` so recycled instances are guaranteed to start with their original materials. `RangedEnemyBrain` overrides `HandleStaggerChanged` and `HandleDeath` to call `chargeBeam.EndCharge()`. `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) — verified via temp-injected csproj that includes the worktree absolute paths. Pending in Editor: dissolve / outline / force field / charge beam / spawn telegraph all visible in playtest. |
| 2026-04-29 | **Polish triple — PR 4.A Combat Feel Pass + PR 4.B Crawler Leap + PR 2.H1 Structures.** Spitter buffed in `Assets/EnemyData/Spitter.asset`: `attackRange 18→28`, `preferredDistance 12→20`, `projectileSpeed 10→14` so the Plasma Spitter actually plays as a long-range threat instead of a mid-range plinker. PR 4.A (combat readability layer): new `Assets/Scripts/Combat/Enemies/{HitFlash, EnemyDeathBurst, SpawnWarpIn}.cs` (auto-attached by `EnemyBrainBase.Awake` so existing prefabs get the upgrade for free) + `Assets/Scripts/Combat/Enemies/AI/SlamImpactRing.cs` (runtime expanding emissive ring spawned by `BruteEnemyBrain.TickAttack`) + `Assets/Scripts/Combat/Player/{CameraShake, PlayerHitFeedback}.cs` (CameraShake auto-attaches to `Camera.main` on first AddTrauma; PlayerHitFeedback auto-added by `GameManager.ResolveReferences`). `ArenaPostProcessingController` got `PulseDamageVignette(duration)` + an `Update()` that ramps the red flash back to baseline. PR 4.B: new `Assets/Scripts/Combat/Enemies/AI/LeapMeleeEnemyBrain.cs` — Crawler now does a 0.45s telegraph + 0.5s kinematic-arc leap at 14 m/s (snapshot landing pos so player can dodge perpendicular during travel) when the player is in the 4m..8m band, otherwise falls through to the regular `MeleeEnemyBrain` swing path inside `attackRange`. Slot manager / telegraph flash / fair-spawn delay all behave identically. PR 2.H1: new `Assets/Scripts/ProceduralArena/Arena/{StructureDefinition, BuiltInStructures, ArenaStructurePlanner}.cs`. `BuiltInStructures` ships 4 silhouettes (bunker, sandbag line, pillar cluster, sniper nest) as code-only POCOs so existing arenas immediately get structures without anyone authoring assets. `ArenaStructurePlanner.Plan` is deterministic (new `structureRng = seed ^ 0x8855`), weighted-pick over category-eligible pool, quantized 90° yaw, min 3-Manhattan from entry, min 3.5 macro-cells from exits, reserves footprint cells before cover so `ArenaCoverPlanner` fills around structures. `SingleArenaGenerator` plans structures between verticality reservation and cover, gated behind `AllowsStructures` (Combat/Elite/Boss/Parkour only — Start/Shop/Rest stay flat). `ArenaTypeProfile.structureBudget` Range(0..5) default 2. `ArenaRoomData.structurePlacements` list. `ArenaBuilder.BuildSingleStructures` uses `BuildUtils.SpawnBox` so `WorldUVScaler` + per-biome materials + instancing apply for free; `ResolveStructureMaterial` maps `StructureSlot → ArenaBuildMaterials` slot. Zero `UnityEngine.Random` in any new procedural code (TZ contract preserved). `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) for all three PRs combined. Pending in Editor: (1) PR 4.A — confirm hit flash + screen feedback + death burst + slam impact ring + spawn warp-in all visible in playtest. (2) PR 4.B — swap `MeleeEnemyBrain` → `LeapMeleeEnemyBrain` on `Assets/Prefabs/Enemy_Crawler.prefab` (drag the existing `Crawler.asset` SO into the `data` field; default leap tuning works for the existing Crawler stats). (3) PR 2.H1 — playtest combat/elite arenas to confirm 1-3 visible structures appear without breaking NavMesh / cover / spawn. |
| 2026-04-29 | **Phase 3 PR 3.F — Enemy + Projectile Pooling** — code landed, Editor playtest pending. New `Assets/Scripts/Combat/Enemies/Spawn/EnemyPool.cs` (auto-creating scene-singleton, per-prefab `Stack<GameObject>` rent/return, re-parents returned instances under the pool root for tidy hierarchy, skips null entries on Rent for scene-reload safety). New `Assets/Scripts/Combat/Enemies/Spawn/PooledEnemy.cs` — `[DefaultExecutionOrder(200)]` so its Awake fires after `EnemyBrainBase.Awake` and captures the post-data `baselineMaxHealth`; listens to its own `Health.onDeath`, cancels Health's `Invoke(Disable)` (pool owns disable timing now), schedules `EnemyPool.Return` after a 1.5s grace window so loot drops + glory-kill detector still get their tick. `PrepareForReuse()` is called by the pool before SetActive(true) and resets Health (`maxHealth = baselineMaxHealth`, `currentHealth = maxHealth`), `EnemyStagger`, `EnemyLootTable`, and warps the NavMeshAgent to the new spawn point. New `Assets/Scripts/Combat/Enemies/Projectiles/EnemyProjectilePool.cs` mirrors the same pattern for Spitter projectiles. `EnemyProjectile` now has `BindPool` + `ResetForPool` (clears owner/damage/direction, caches `TrailRenderer[]` and `Clear()`s each on rent so a recycled shot does not paint a trail from the previous muzzle), and replaces `Destroy(gameObject)` with `pool.Return` (falls back to Destroy when no pool is bound). `RangedEnemyBrain.SpawnProjectile` rents through `EnemyProjectilePool.Instance.Rent` instead of `Instantiate`. `Health.cs` got `ResetForPool()` (restores currentHealth + re-fires onHealthChanged) and `CancelAutoDisable()` (cancels the pending 1s SetActive(false) when a pool owns the lifecycle). `EnemyStagger.cs` got `ResetForPool()` (drops `IsStaggered`, blacks out the cached material emission, fires `OnStaggerChanged(false)` so brain leaves the `Staggered` state). `EnemyLootTable.cs` got `ResetForPool()` to clear the one-shot `rolled` guard so each death rolls loot. `GameManager.SpawnEnemy` and `SpawnEncounterEnemy` switched from `Instantiate` to `EnemyPool.Instance.Rent`; `Health.onDeath` listener uses Remove+Add so a recycled instance does not accumulate duplicate `OnEnemyDied` / `OnEncounterEnemyDied` subscriptions across rents (TZ §10 acceptance: kills count exactly once). `EnemyBrainBase` — fair-spawn init moved out of `Start` (which only runs on the first OnEnable per instance lifetime) into a per-OnEnable first-frame check inside `Update`. `OnEnable` resets `spawnInitialized = false` and `SetState(Spawn)`; `Update` waits one tick for `Target` to be resolved (autoResolve in Start, or `GameManager.SetTarget` right after Rent) before invoking the new `InitializeSpawn()`. This is what closes the "pooled enemy still gets fair-spawn warning" gap. `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) — verified 2026-04-29 via temp-injected csproj that includes the worktree absolute paths. Pending in Unity Editor: long run (5-arena loop ×2) — confirm no disabled enemy/projectile GameObjects accumulate in the Hierarchy; kill counts match enemies killed; HP orbs drop on every kill; recycled enemies start at full HP without stuck red stagger; fair-spawn delay still triggers on close spawns; projectile trails do not jump from the previous shot's last position. |
| 2026-04-29 | **Phase 3 PR 3.E — Combat Readability + Active Attack Slots — verified by user.** Slot caps (3/3/1/1), per-enemy `telegraphColor` pulse, Brute slam decal, and fair-spawn delay all behave as specified in `test.unity` playtest. |
| 2026-04-28 | **Phase 3 PR 3.E — Combat Readability + Active Attack Slots** — code landed, Editor playtest pending. New `Assets/Scripts/Combat/Enemies/AI/AttackSlotKind.cs` enum (Melee/Ranged/Heavy/Special). New `Assets/Scripts/Combat/Enemies/AI/ActiveAttackSlotManager.cs` — auto-creating scene-singleton MonoBehaviour with caps `meleeSlots=3 / rangedSlots=3 / heavySlots=1 / specialSlots=1` (TZ §8.3). Static `KindForRole(EnemyRole)` maps Fodder/Chaser → Melee, Ranged → Ranged, Tank → Heavy, Zoner → Special, Boss → Heavy. `TryAcquire(holder, kind)` is idempotent (already-holding brain re-acquires without double-counting); `Release(holder)` removes from all sets so callers don't have to remember which kind they hold. New `Assets/Scripts/Combat/Enemies/AI/TelegraphFlash.cs` — placeholder telegraph visual via MaterialPropertyBlock pulse on every Renderer in the enemy hierarchy (no runtime material instances; reads each renderer's base `_EmissionColor` at Awake and adds the per-call pulse color on top); strobe at 6Hz with intensity ramp toward impact. Auto-attached by `EnemyBrainBase.Awake` if none is present, so existing prefabs (Drone/Crawler/Spitter/Brute) get telegraph visuals "for free" without Editor work. New `Assets/Scripts/Combat/Enemies/AI/BruteSlamDecal.cs` — runtime-built ground decal (Cylinder primitive scaled to `slamRadius × 2 × 0.025`, transparent URP Lit emissive orange, alpha + emission ramp toward impact frame); `Show(duration)` on Brute Telegraph entry, `Hide()` on Attack/Recover/Stagger/Death. `EnemyData` got two new sections: `[Header("Combat Readability (PR 3.E)")]` with `telegraphColor` (HDR ColorUsage(true,true), default warm orange) so Drone/Crawler/Spitter/Brute can ship distinct wind-up colors; and the existing `[Header("Spawn ...")]` section grew `fairSpawnDistance` + `fairSpawnDelay` (defaults 5m / 0.6s, set 0 to disable) for the TZ §8.4 fair-spawn delay. `EnemyBrainBase` extended: cached `TelegraphFlash` reference + `SlotKind` derived from `data.role`; new helpers `TryAcquireAttackSlot()` / `ReleaseAttackSlot()` / `BeginTelegraphFlash()` / `EndTelegraphFlash()`; fair-spawn check in Start (if Target closer than `fairSpawnDistance`, hold in `Spawn` state until `Time.time >= spawnHoldUntil`, pulse telegraph during the hold); `OnDisable`, `HandleStaggerChanged`, `HandleDeath` now release the attack slot and end the telegraph pulse so the slot doesn't leak across stagger/death. `MeleeEnemyBrain` / `RangedEnemyBrain` / `BruteEnemyBrain` now gate Move → Telegraph behind `TryAcquireAttackSlot()` (if denied, brain stays in Move/Reposition until a slot frees up next frame), call `BeginTelegraphFlash()` on Telegraph entry, `EndTelegraphFlash()` on Attack frame, and `ReleaseAttackSlot()` on Recover → Move. `BruteEnemyBrain` additionally drives `BruteSlamDecal.Show/Hide` at the matching transitions (overrides `HandleStaggerChanged` / `HandleDeath` to also hide the decal). `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings, verified via temp-injected csproj that includes the worktree absolute paths). Pending in Unity Editor: spawn a heavy encounter (≥ 8 Drones + 1 Brute), confirm only 3 melee + 1 heavy resolve attacks at once, confirm slam decal is visible during the 0.9s wind-up, confirm a close-spawned enemy holds for ~0.6s with a flash before chasing. |
| 2026-04-28 | Phase 3 PR 3.A–3.D integration pass: committed the `claude/kind-elgamal-a56350` worktree state after review. `Drone` / `Crawler` / `Spitter` / `Brute` `EnemyData` assets and prefab variants are present, `EnemyProjectile.prefab` is present, and `SpawnProfile_Combat.asset` is wired into `Arena_Combat_M.asset` plus `Arena_Elite_L.asset`. Added a small `GameManager.BeginEncounter(roster, ...)` guard so restarting a roster-driven encounter over an active encounter cannot clear the new roster before the first spawn. External `dotnet build Assembly-CSharp.csproj` passed; Unity PR 3.D role-mix playtest remains pending. |
| 2026-04-27 | **Phase 3 PR 3.D — Spawn Composition (composer + per-arena profile)** — code landed, Editor wiring pending. New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnEntry.cs` (Serializable, with `Resolved*` accessors implementing the TZ §7.2 Revision override rule: value > 0 overrides `EnemyData`, value 0 inherits). New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnProfile.cs` SO holding the entry roster, budget curve (`baseBudget` + `budgetPerArenaIndex`), variety cap (`maxEnemyTypesPerEncounter`), and the `maxTanks`/`maxRanged` bad-combination guards from TZ §7.6. New `Assets/Scripts/Combat/Enemies/Spawn/EnemySpawnComposer.cs` — static, pure-logic utility. Algorithm: filter eligible entries by arenaIndex; pick `targetVariety` distinct roles (1/2/3 by arena index, capped by profile); spend `baseBudget + arenaIndex * budgetPerArenaIndex` budget by weighted random selection from the picked roles, respecting `maxAlive` per entry, `maxTanks`, `maxRanged`. Determinism: optional `System.Random` parameter for seeded runs. Every fallback path emits `Debug.LogWarning` with a specific reason (null profile / no entries pass arena gate / all caps blocked) per TZ §7.4 Revision — no silent fallbacks. New `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, ...)` overload; per-enemy prefab pulled from `encounterRoster[enemiesSpawned]` in `SpawnEncounterEnemy`; legacy `BeginEncounter(int count, ...)` preserved unchanged for the wave loop and for null-profile fallback. `EncounterController` got `arenaIndex` (single owner per TZ §7.4 Revision) and `spawnProfile` fields; `SpawnEnemiesViaGameManager` runs the composer when profile != null and passes the resolved roster, otherwise falls through to the legacy count-based call. `ArenaTypeProfile` got a new `spawnProfile` field so Combat / Elite / Boss arena types can each define their own composition. `ArenaFlowController.SetupEncounter` propagates `node.arenaIndex` and `profile.spawnProfile` into the runtime `EncounterController`. `dotnet build Assembly-CSharp.csproj` clean. Pending in Editor: create at least one `EnemySpawnProfile` asset, populate entries pointing at the existing Drone/Crawler/Spitter/Brute prefabs + EnemyData SOs, then drag the profile into existing `Arena_Combat_M` / `Arena_Elite_M` / `Arena_Boss_L` assets. arenaIndex curve from TZ §7.5: arena 0 → Drone+Crawler only, 1 → +Spitter, 2 → +Brute. Until profiles are wired the build behaves identically to PR 3.C (legacy single-prefab spawn). |
| 2026-04-27 | **Phase 3 PR 3.C — Station Brute (tank enemy)** — code landed, Editor wiring pending. New `Assets/Scripts/Combat/Enemies/AI/BruteEnemyBrain.cs` extends `EnemyBrainBase`. State flow: Move → Telegraph → Attack → Recover, with slow turning during Telegraph (`Quaternion.Slerp` 6/sec, half the speed of Spitter's 12/sec, so the Brute can't pivot to follow a strafing player perfectly). Slam damage applied ONCE at end of `telegraphTime` via `Physics.OverlapSphereNonAlloc` (32-collider buffer, static, allocation-free) at `transform.TransformPoint(slamOriginOffset)` sampled at impact frame — TZ §6.4 contract (player escapes by leaving `slamRadius` during wind-up, not by leaving at telegraph start). Hits are deduped by `Health` reference via a HashSet, because the player has multiple colliders (CharacterController + child weapon volumes) attached to the same Health and we don't want to deal damage twice. `slamHitMask` and `slamRadius` come from the existing `EnemyData` fields added in PR 3.A. `OnDrawGizmosSelected` renders a wire-sphere slam preview in the Scene view (orange while Telegraph, red otherwise) for tuning. Until PR 3.E ships the active-attack slot manager, the Brute `EnemyData.maxAlive = 1` is mandatory (TZ Revision Log v2) — enforced via SO field, not code. `dotnet build Assembly-CSharp.csproj` clean. Pending in Editor: create `Brute.asset` SO (Role=Tank, maxHealth 260, moveSpeed 2.2, damage 28, attackRange 3.0, slamRadius 4.0, attackCooldown 3.0, telegraphTime 0.9, recoveryTime 1.1, **slamHitMask = Default + Player layers**, spawnCost 6, minArenaIndex 2, maxAlive 1, spawnWeight 1) and `Enemy_Brute.prefab` variant. |
| 2026-04-27 | **Phase 3 PR 3.B — Plasma Spitter / Sentinel (ranged enemy)** — code landed, Editor wiring pending. New `Assets/Scripts/Combat/Enemies/AI/RangedEnemyBrain.cs` extends `EnemyBrainBase` with a hold-distance state machine (Move closes in if dist > preferredDistance + band; Reposition backs off if dist < preferredDistance − band; once inside the hysteresis band the brain stops, faces target, and considers firing). Telegraph entry requires a clear line-of-sight raycast from `muzzleOffset` (default 1m up) to target; LoS is throttled by `EnemyData.lineOfSightCheckInterval` (cached between checks). Attack re-checks LoS at impact — dashing behind cover during the wind-up cancels the projectile per TZ §6.3 ("Spitter does not shoot through walls if line-of-sight check fails") while still consuming the cooldown. Telegraph faces the target via Quaternion.Slerp on flat XZ. New `Assets/Scripts/Combat/Enemies/Projectiles/EnemyProjectile.cs` is owner-filtered: trigger SphereCollider, manual `transform.position` translation (avoids Rigidbody tunneling decisions for slow plasma — at default speed=10 step is ~0.17m/frame, smaller than recommended SphereCollider radius 0.25), passes through self + any other `EnemyBrainBase`/legacy `SimpleEnemyAI`, damages the first non-owner `Health` via `Health.TakeDamage` and self-destructs, also self-destructs after `lifetime` seconds. Pooling deferred to PR 3.F per TZ. `dotnet build Assembly-CSharp.csproj` clean. Pending in Unity Editor: (1) create `EnemyProjectile.prefab` (SphereCollider trigger r≈0.25 + visual mesh/Trail + the `EnemyProjectile` script); (2) create `Spitter.asset` SO (Role=Ranged, fill ranged stats from TZ §6.3 with the projectile prefab dragged into `projectilePrefab`); (3) create `Enemy_Spitter.prefab` variant from `Enemy.prefab`, swap `SimpleEnemyAI` → `RangedEnemyBrain`, drag in Spitter SO; (4) playtest behind cover (Spitter must not shoot through walls). |
| 2026-04-27 | **Phase 3 PR 3.A — Enemy state-machine base + Drone/Crawler scaffolding** — code landed, Editor wiring pending. New `Assets/Scripts/Combat/Enemies/AI/` module: `EnemyRole` enum (with `Boss` reserved for Phase 4), `EnemyAIState` enum (Spawn/Move/Telegraph/Attack/Recover/Reposition/Staggered/Dead), `IEnemyTargetReceiver` interface (preserves `SetTarget(Transform)` contract), `EnemyBrainBase` abstract MonoBehaviour (NavMeshAgent + Health auto-cache, throttled `RequestPathTo` with 0.2s default interval and `agent.isOnNavMesh` guard, state machine, `Health.onDeath` → `Dead` transition, `EnemyStagger.OnStaggerChanged` → `Staggered` transition with agent stop), `MeleeEnemyBrain` concrete brain (Move → Telegraph → Attack → Recover loop, single-shot impact with re-check at end of telegraph so player can escape during wind-up). New `Assets/Scripts/Combat/Enemies/Data/EnemyData.cs` ScriptableObject covering identity, stats, attack timing, ranged stub fields (for PR 3.B), heavy/slam stub fields (for PR 3.C), and spawn defaults (which `EnemySpawnEntry` will override per PR 3.D). `Assets/test/SimpleEnemyAI.cs` kept as Phase 3 compatibility wrapper but updated to (a) implement `IEnemyTargetReceiver`, (b) throttle `SetDestination` to `pathUpdateInterval` (0.2s default) — fixes the PR 3.A acceptance "no SetDestination every frame", (c) remove the per-attack `Debug.Log` spam. `Assets/test/GameManager.cs` `SpawnEnemy` and `SpawnEncounterEnemy` now resolve `IEnemyTargetReceiver` via `GetComponent` instead of `SimpleEnemyAI` directly — no fallback branch (TZ §5.4). `dotnet build Assembly-CSharp.csproj` ran clean (zero CS errors/warnings) after temp-injecting new files into csproj for verification; Unity will regenerate csproj on next refresh and pick up the files automatically. Pending in Unity Editor: create `Drone.asset` / `Crawler.asset` SOs via *Create > Void Survivor > Enemies > Enemy Data*, then either build prefab variants or swap `SimpleEnemyAI` → `MeleeEnemyBrain` on `Enemy.prefab`. Tuning note: spec values for Drone (3.6 m/s) and Crawler (4.8 m/s) are below `PlayerController.moveSpeed = 10`, so before locking final numbers verify against actual player movement; ENEMY_AI_TZ §6.2 already flags this. |
| 2026-04-27 | ENEMY_AI_TZ.md DRAFT v2 revision pass before PR 3.A (clarifications only, no structural changes): added `[Header("Ranged")]` and `[Header("Heavy / Brute")]` sections to shared `EnemyData`; documented `EnemySpawnEntry` override rule (>0 overrides `EnemyData`, 0 inherits); specified melee damage path (range re-check at impact, `Health.TakeDamage`, no ghost hits); added stagger integration paragraph (abort attack, release slot, stop agent); marked `EnemyRole.Boss` as reserved for Phase 4; specified Brute slam mechanic (`Physics.OverlapSphere` at impact frame using `slamHitMask`, `maxAlive=1` mandatory until PR 3.E slot manager); noted `SimpleEnemyAI` self-implements `IEnemyTargetReceiver` (no `GameManager` fallback branch); composer must `Debug.LogWarning` on Drone fallback; declared `EncounterController` as single owner of `arenaIndex`; extended PR 3.F scope to include `EnemyProjectilePool` for Spitter projectiles; added Crawler tuning note. |
| 2026-04-27 | Added `docs/ENEMY_AI_TZ.md` as the master Phase 3 Enemy AI specification. Scope is intentionally realistic for the current project: state-machine base, four main enemy roles, spawn composition by budget/weights/arenaIndex, combat readability/attack slots, and pooling after enemy contracts stabilize. Full AI Director, Arena Complex spawn logic, Shield Drone, complex Brute charge, and final art/VFX are explicitly deferred. |
| 2026-04-26 | Planning decision: **PR 2.H (beveled prefabs) deferred** until after first Phase 3 enemy-AI pass. Coupling between PR 2.H and Phase 3 is minimal (NavMesh per-arena re-bake handles new obstacles, `combatSpawnPoints` are independent, glory-kill / stagger / pooling are orthogonal); only real follow-up cost is a balance pass on ranged enemies once new cover changes line-of-sight density. Also captured new sub-task **PR 2.H1 — Hand-authored structures** (bunkers / sandbag lines / pillar clusters / broken arches / sniper nests + atmospheric variants like crashed pods, generator stacks, terminals) to be implemented as `StructureDefinition` SO with `List<BoxPart>` reusing `BuildUtils.SpawnBox` — no Blender, no Asset Store, deterministic via new `structureRng` sub-stream. May close ~60% of PR 2.H value before bevels. PR 2.G visual verify is informally accepted (re-open if regressions surface during Phase 3 playtests). Next active task: refactor `Assets/test/SimpleEnemyAI.cs` into a state machine (Phase 3 §Enemy Types). |
| 2026-04-26 | Captured future **Arena Complex / Connected Arena Rooms** direction from user sketch: larger maps made of several large combat rooms connected by direct wide gates instead of corridors. **Superseded 2026-04-30:** user cancelled this direction; keep the existing single-arena procedural run as the active architecture. |
| 2026-04-24 | Phase 2 PR 2.E (Visual Style Pass) marked verified after the user tested the result in Unity Editor and reported it is acceptable for closure. Review notes: check east/west ramp axis before re-enabling Parkour, and restore `RenderSettings.fogMode` if biome atmosphere starts affecting other scenes/controllers. |
| 2026-04-25 | Phase 2 PR 2.G second follow-up: switched ceiling fill lights from Point to Spot pointing straight down (110° outer / ~60° inner). Point lights at wh*0.85 lost ~95% of intensity to inverse-square before reaching the floor — that's why the previous pass looked dark. Spots now: intensity = max(2.5, biome.accentLightIntensity × 1.6) (≈3.5 default), range = wh + 6m, mounted at wh-0.45m. Added dedicated `mats.lampPanel` material (`MakeEmissive` with intensity 4.5, color ≈ warm white slightly pulled toward biome.ambientTint) so the panel itself reads as bright and biome-agnostic; previously panels used `mats.emissiveAccent` which on dim biomes barely glowed. Panel size bumped 1.4 → 2.2 m for floor-readability. |
| 2026-04-25 | Phase 2 PR 2.G follow-up: visible ceiling lamp fixtures. New `ArenaBuilder.SpawnCeilingLamp` co-spawns a dark mounting bracket (`mats.ceiling`/`mats.wall`) flush to the ceiling and an emissive panel (`mats.emissiveAccent`) hanging just below it at every fill-light position (1 for small Start/Shop/Rest arenas, 5 for medium/large). Lamps are visual-only — the underlying fill point light still does the illumination — so light-count budget per renderer is unchanged. Mount is 4cm below the ceiling tile to avoid z-fight. |
| 2026-04-25 | Phase 2 PR 2.G Anti-stretch + lighting fill — **code landed, Unity verify pending**. Added `WorldUVScaler` MonoBehaviour + `WorldUVDensityRegistry` (per-instance MaterialPropertyBlock that derives `_BaseMap_ST` / `_BumpMap_ST` / occlusion / metallic / emission ST from each box's `lossyScale`); `BuildUtils.SpawnBox` now auto-attaches `WorldUVScaler` to every cube, killing texture stretch on big floors and walls without changing meshes. `ArenaBuildMaterials.CreateSurface` registers per-material density derived from `slot.textureScale * 0.25` (tiles-per-meter), enables GPU instancing on all biome materials, and stops baking textureScale into `_BaseMap_ST` (per-instance MPB now drives tiling). New `ArenaBuilder.BuildSingleEdgeStrips` emits thin emissive strips along floor-to-wall seams (skips door cells) for "panel-light" feel. New `ArenaBuilder.BuildSingleFillLights` emits one center + four quadrant point lights at 0.85·wh height (intensity ≈ 0.45·biome.accentLightIntensity, color = `Lerp(neutralWhite, biome.ambientTint, 0.35)`, range ≈ arena half-span; quadrant lights only on arenas ≥6×6 cells). `PC_RPAsset.asset`: `m_AdditionalLightsPerObjectLimit` 4→8, shadow distance 50→60. |
| 2026-04-25 | Phase 2 PR 2.F Visual Fidelity Pass — **code landed, Unity verify pending**. Root cause of "robloxness" was that `Assets/test.unity` had `m_RenderPostProcessing: 0` on the main camera — fixed (now `1` + SMAA). `PC_Renderer.asset` SSAO intensity 0.4→0.85, radius 0.3→0.35, samples 1→2. New `ArenaPostProcessingController` (auto-added on `ArenaFlowController.Awake`) builds a runtime Global Volume with Bloom + ColorAdjustments + Vignette + ACES Tonemapping at priority 100; biome `colorFilter` now drives the per-arena vibe instead of `RenderSettings.ambient*`. New `BiomePostProcessing` block on `BiomeDefinition` (bloom / exposure / contrast / saturation / vignette) + `accentLightIntensity` / `accentLightRange` / `exitLightColor`. `ArenaFlowController.SpawnReflectionProbe` adds a realtime box-projected probe at arena center, one-shot `RenderProbe` on build. `ArenaBuilder.BuildSingleExits` + `SpawnAtmospherePylon` now attach URP Point Lights driven by biome. `BiomeSurfaceDefinition` extended with per-slot `bumpScale`, `parallaxStrength`, `detailAlbedoResourcePath`, `detailTextureScale`, `detailStrength`; `ApplyTextureSet` enables `_PARALLAXMAP`, `_DETAIL_MULX2`, sets `_BumpScale` from slot. Fog bug fixed in `ApplyBiomeAtmosphere`: `fogStrength` is now the lerp t (previously clamped to ≥0.92, meaning even fogStrength=0 biomes got 92% of biome.fogColor); ambient nudge weights softened (0.68/0.48/0.52 → 0.35/0.25/0.30) since color tint moved to post. |
| 2026-04-24 | Phase 2 PR 2.E closed after user playtest — visual style pass treated as complete. |
| 2026-04-24 | Phase 2 PR 2.E follow-up tuning: `Parkour` stayed disabled in run generation, `SingleArenaGenerator` + `ArenaBuilder` now hard-block accidental verticality for `Start` / `Shop` / `Rest`, `Start` no longer spawns perimeter decor, and `Biome_VoidStation` was retuned away from the bright blue `Panel_009` trims/props toward more neutral `Panel_007` slots with much weaker emissive accents. `dotnet build Assembly-CSharp.csproj` passed again; Unity-side visual verify is still pending. |
| 2026-04-23 | Phase 2 PR 2.E second pass (Visual Style Pass) - **code landed, Unity verify pending**. The first pass was extended into a fuller prototype PBR pipeline: companion texture sets (base, normal, metallic/spec, roughness, AO, height, emissive) are now copied under `Assets/Resources/ProceduralArena/Biomes/`, `ArenaBuildMaterials` now auto-resolves those maps, and `Assets/Editor/ProceduralArena/ProceduralArenaTextureImportUtility.cs` reimports them with readable/normal/linear settings for runtime packing. `ArenaBuilder.BuildSingle` now gives colliders to architectural pieces and prop blocks, reduces center accent coverage, localizes contamination more conservatively, and keeps overlays/non-solid atmosphere layers separate. `Biome_AlienNexus` was retuned from a fully pink organic look toward an infected-station look, and `ArenaFlowController` fog/ambient application was strengthened. `dotnet build Assembly-CSharp.csproj` passed; the remaining work is Unity-side import validation and a full 5-arena visual playtest/tuning pass. |
| 2026-04-23 | Phase 2 PR 2.E first pass (Visual Style Pass) — **code landed, Unity verify pending**. `BiomeDefinition` was expanded from plain colors to material-slot-driven biome data with optional Resources texture fallback. Approved PR 2.E textures were copied under `Assets/Resources/ProceduralArena/Biomes/`. `ArenaBuildMaterials` now resolves richer biome surfaces; `ArenaBuilder.BuildSingle` now emits `Architecture`, `FloorDetails`, `Decor`, and `Atmosphere` layers (door frames, ceiling beams, wall ribs, corner pillars, center/exit floor accents, perimeter props, emissive pylons, optional contamination patches). `ArenaFlowController` now applies biome fog/ambient tint, and `ArenaDebugGizmos` single-arena build path now uses biome materials too. `dotnet build Assembly-CSharp.csproj` passed; visual Play Mode verification is still required before PR 2.E can be marked complete. |
| 2026-04-22 | Phase 2 PR 2.D (Verticality + Biomes + Balance) — **verified 2026-04-22**. Added `BiomeDefinition` + 2 biome assets, `ArenaVerticalityPlanner`, platform/ramp placements in room data, biome-driven `ArenaBuildMaterials`, `ArenaRuntimeDebugOverlay`, new Elite/Parkour/Shop/Rest profile assets, and `RunConfig`/encounter scaling by `arenaIndex` (+enemy count, +enemy HP). `SingleArenaGenerator` now reserves verticality cells against cover/spawn placement and emits biome metadata. `ArenaBuilder.BuildSingle` now emits `Verticality/{Platforms,Ramps}` geometry. User playtest reported the new functionality works without problems, so PR 2.D is treated as complete. |
| 2026-04-22 | Phase 2 PR 2.C (NavMesh + Encounter Integration) — **verified 2026-04-22**. Async `NavMeshSurface.UpdateNavMesh`, `EncounterController` + `SoftLockBarrier`, `GameManager` encounter API, and `SimpleEnemyAI.isOnNavMesh` guard were playtested successfully together with the run flow: enemies spawned on baked NavMesh, barriers held until clear, and exit triggers behaved correctly. |
| 2026-04-21 | Phase 2 PR 2.B (Run Graph + Transitions) — verified 2026-04-21. New `Assets/Scripts/ProceduralArena/Run/` module: `RunStage` enum (Start/Mid1/Mid2/Mid3/Boss), `RunGraphNode` (id/stage/arenaIndex 0..4/arenaSeed/typeProfile/children), `RunGraph`, `RunConfig` SO (runSeed, startProfile, bossProfile, mid1/mid2/mid3 pools, fade timings, autoStartOnPlay, skipClearCondition), `RunGraphGenerator.Build` (8 nodes layout 1+2+2+2+1, shared-subtree wiring — каждый mid-узел ведёт в оба следующих mid; PickProfile избегает повтора профиля в том же stage), `ExitDoorTrigger` (OnTriggerEnter → `RunController.ChooseDoor(childIndex)` или `NotifyExitTriggeredOnBoss` для boss-арены), `DoorChoiceLabel` (world-space TextMesh "{category}\n[idx/5]" + Billboard к камере), `ArenaFlowController` (единственный ArenaRoot child; `EnterArena` coroutine — fade in → destroy → `SingleArenaGenerator.Generate` → `ArenaBuilder.BuildSingle` → `SpawnExitTriggers` → `TeleportPlayerToStart` (CC.enabled toggle) → fade out; auto-builds fade Canvas в Awake), `RunController` state-machine (Idle/Generating/Entering/Playing/Transitioning/Victory/GameOver, `StartRun(seed)`, `ChooseDoor`, `NotifyPlayerDied`, `NotifyExitTriggeredOnBoss`, `NotifyArenaClearedIfReady` — hook для PR 2.C; auto-builds Victory/GameOver canvas с Restart). `DefaultRunConfig.asset` ссылается на 3 preset-профиля. Post-verify fixes: Billboard label flip (text был mirrored), door-opening cut в shell wall (emissive был виден только снаружи), lintel над дверью (закрывает sky gap), solid `ExitBarrier_i` за trigger'ом (prevents falling off map during fade). Zero `UnityEngine.Random` в Run/. |
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
- Phase 2 **PR 2.E** visual style pass is verified in Unity Editor by the user (2026-04-24). Phase 2 r4 is complete through PR 2.E.
- Next: start **Phase 3 — Enemy AI**, beginning with a `SimpleEnemyAI` state-machine refactor that keeps the current encounter/run pipeline intact.
- Cancelled 2026-04-30: **Arena Complex / Connected Arena Rooms** prototype. Keep the single-arena procedural run as the active architecture unless the user explicitly reopens this direction.
- TZ: [ARENA_GENERATION_TZ.md](./ARENA_GENERATION_TZ.md) (APPROVED r4).

### Phase 2 PR 2 — Editor verification checklist

- [ ] Open `Assets/test.unity`, select `ArenaDebug` GameObject → context-click its `ArenaDebugGizmos` component → `Generate + Build`.
- [ ] Confirm an `ArenaRoot` child appears with `Room_N_*` subtrees + `Corridors/Corridor_i` subtrees, each `Shell` containing Floor/Ceiling/Wall cubes.
- [ ] Walk in Play mode from the emissive green start marker to the emissive red exit marker — no invisible walls blocking corridors, no holes in floor/walls.
- [ ] Same `config.seed` produces the exact same Hierarchy layout after `Clear Geometry` + `Generate + Build`.
- [ ] `Anchors` subfolders present per room (Corner / Wall / Ceiling / Floor / DoorFrame) and visible in Scene view as empty GOs.
- [ ] Cover pillars appear only in combat rooms, never on door cells.
- [ ] Console has one summary log per generation, no per-cell spam.
