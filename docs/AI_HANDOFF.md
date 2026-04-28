# AI Handoff

Short-lived current-task handoff for the next AI session.
For stable architecture / roadmap / known issues, see:

- [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/docs/KNOWN_ISSUES.md)
- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md)

---

## Current Status (2026-04-27)

- **Phase 3 PR 3.A — Enemy state-machine base — VERIFIED 2026-04-27 by user.** Drone + Crawler SOs + prefab variants, playtest passed.
- **Phase 3 PR 3.B — Plasma Spitter / Sentinel — VERIFIED 2026-04-27 by user.** Spitter SO + projectile prefab + Enemy_Spitter variant, LoS/dodge/wall-block confirmed.
- **Phase 3 PR 3.C — Station Brute — VERIFIED 2026-04-27 by user.** Brute SO + Enemy_Brute variant, slam telegraph + escape window playtested. Slam telegraph **visual** explicitly deferred to PR 3.E (will ship as a unified telegraph layer for all enemies — likely URP Decal Projector approach, see Recommended Next Task in older revision).
- **Phase 3 PR 3.D — Spawn Composition — code + Unity asset wiring landed 2026-04-27, role-mix playtest pending.**
  - New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnEntry.cs` — Serializable row with `Resolved*` accessors implementing the override rule (TZ §7.2 Revision: entry value > 0 overrides `EnemyData`, 0 inherits).
  - New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnProfile.cs` — SO holding entry roster, budget curve (`baseBudget` + `budgetPerArenaIndex`), variety cap, and `maxTanks` / `maxRanged` guards (TZ §7.6).
  - New `Assets/Scripts/Combat/Enemies/Spawn/EnemySpawnComposer.cs` — static pure-logic utility. Filters eligible entries by arenaIndex; picks `targetVariety` distinct roles (1/2/3 by arena index, capped by profile); spends budget weighted-random with maxAlive / role caps. Optional `System.Random` for seeded runs. **Every fallback path emits `Debug.LogWarning` with a specific reason** — no silent fallbacks (TZ §7.4 Revision).
  - New `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, …)` overload; `SpawnEncounterEnemy` pulls per-enemy prefab from `encounterRoster[enemiesSpawned]`. Legacy `BeginEncounter(int count, …)` preserved unchanged for the wave loop and for null-profile fallback.
  - `EncounterController` got `arenaIndex` and `spawnProfile` fields. `arenaIndex` is the single source of truth (TZ §7.4 Revision). `SpawnEnemiesViaGameManager` runs the composer when profile != null and passes the resolved roster; otherwise falls through to legacy count-based path.
  - `ArenaTypeProfile.spawnProfile` field added — Combat / Elite / Boss profiles can each carry their own composition.
  - `ArenaFlowController.SetupEncounter` propagates `node.arenaIndex` and `profile.spawnProfile` into the runtime `EncounterController`.
  - `Assets/EnemyData/SpawnProfile_Combat.asset` authored with Drone / Crawler / Spitter / Brute entries, and wired into `Assets/ArenaProfiles/Arena_Combat_M.asset` + `Assets/ArenaProfiles/Arena_Elite_L.asset`.
  - `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, …)` now ends any already-active encounter before preserving the new roster, so an overlap restart cannot clear the composition list.
  - `dotnet build Assembly-CSharp.csproj` clean.
  - **Pending in Unity Editor (next step):** playtest a generated run with the profile active. Confirm mixed roles appear by arenaIndex, no `[EnemySpawnComposer] Fallback` warnings appear in normal play, Brute stays capped at one, and HP-orb / stagger / glory-kill / barrier-open contracts still work.
- **Phase 3 PR 3.C — Station Brute — code landed 2026-04-27, Editor wiring pending.**
  - New `Assets/Scripts/Combat/Enemies/AI/BruteEnemyBrain.cs` extends `EnemyBrainBase`. State flow: Move → Telegraph → Attack (area slam) → Recover. Telegraph faces target via slow `Quaternion.Slerp` (rate 6/sec — half of Spitter's, so a strafing player can break the lock during a 0.9s wind-up).
  - Slam damage applied ONCE at end of `telegraphTime` via `Physics.OverlapSphereNonAlloc` (static 32-collider buffer, no per-frame GC) at `transform.TransformPoint(slamOriginOffset)` sampled at the impact frame, **not** at telegraph start. Player escapes by leaving `slamRadius` during the wind-up.
  - Multi-collider dedup: `HashSet<Health>` so the player's CharacterController + child weapon volumes don't double-damage on one slam.
  - `slamRadius` and `slamHitMask` come from `EnemyData` fields added in PR 3.A — no schema changes.
  - `OnDrawGizmosSelected` draws a wire-sphere slam preview in Scene view (orange during Telegraph, red baseline). Useful when tuning the SO.
  - Until PR 3.E lands the active-attack slot manager, `EnemyData.maxAlive = 1` for Brute is mandatory (TZ Revision Log v2) — enforced via the SO field in Editor, not via code.
  - `dotnet build Assembly-CSharp.csproj` clean.
  - **Pending in Unity Editor (next step):** create `Brute.asset` SO + `Enemy_Brute.prefab` variant, then playtest.
- **Phase 3 PR 3.B — Plasma Spitter / Sentinel — code landed 2026-04-27, Editor wiring pending.**
  - New `Assets/Scripts/Combat/Enemies/AI/RangedEnemyBrain.cs` extends `EnemyBrainBase`. State flow: Move (close in if dist > pref+band) → Reposition (back off if dist < pref-band) → Telegraph (face target, wait `telegraphTime`) → Attack (re-check LoS, spawn projectile) → Recover → Move. Hysteresis `distanceBand` (default 1.5m) prevents jitter at the band edge.
  - LoS check: `Physics.Raycast` from `muzzleOffset` (default `Vector3.up * 1.0`) toward `target.position + Vector3.up`. Throttled by `EnemyData.lineOfSightCheckInterval`, result cached between checks. Refuses Telegraph entry without clear LoS, AND re-checks at Attack frame so dashing-behind-cover during wind-up cancels the shot (TZ §6.3) while still consuming the cooldown.
  - New `Assets/Scripts/Combat/Enemies/Projectiles/EnemyProjectile.cs`. Owner-filtered: trigger `SphereCollider`, manual `transform.position` translation (avoids non-kinematic Rigidbody surprises). Passes through self and any other enemy (`EnemyBrainBase` or legacy `SimpleEnemyAI` in parent chain). Damages the first non-owner `Health` via `Health.TakeDamage`. Self-destructs on damage hit OR world hit OR `lifetime` timeout (default 4s). No pooling — that's PR 3.F.
  - `dotnet build Assembly-CSharp.csproj` clean (zero CS errors/warnings) after temp-injecting new files into csproj for verification.
  - **Pending in Unity Editor (next step, see `Recommended Next Task`):** create `EnemyProjectile.prefab`, `Spitter.asset` SO, `Enemy_Spitter.prefab` variant, then playtest LoS / dodgeability.
- **Phase 3 PR 3.A — Enemy state-machine base — code landed 2026-04-27, Editor wiring pending.**
  - New module `Assets/Scripts/Combat/Enemies/AI/`: `EnemyRole` (Fodder/Chaser/Ranged/Tank/Zoner/Boss-reserved), `EnemyAIState` (Spawn/Move/Telegraph/Attack/Recover/Reposition/Staggered/Dead), `IEnemyTargetReceiver` (`SetTarget(Transform)`), `EnemyBrainBase` abstract MonoBehaviour, `MeleeEnemyBrain` concrete brain.
  - New `Assets/Scripts/Combat/Enemies/Data/EnemyData.cs` ScriptableObject — single SO type for all enemies; ranged/heavy fields stay default-zero on melee enemies. `[CreateAssetMenu]` path: *Create > Void Survivor > Enemies > Enemy Data*.
  - `EnemyBrainBase`: `[RequireComponent(NavMeshAgent, Health)]`, throttled `RequestPathTo` (0.2s default `pathUpdateInterval`, `agent.isOnNavMesh` guard inherited from PR 2.C), `Health.onDeath` → `EnemyAIState.Dead` listener, `EnemyStagger.OnStaggerChanged` → `EnemyAIState.Staggered` listener (aborts in-progress attack, calls `agent.ResetPath`).
  - `MeleeEnemyBrain`: Move → Telegraph → Attack → Recover loop. Damage applies once at end of `telegraphTime` with a fresh range check, so a target moving out during wind-up causes the swing to whiff (TZ §5.6 contract). Drone and Crawler will share this class and differ only by their `EnemyData` SO.
  - `Assets/test/SimpleEnemyAI.cs` is now a Phase 3 compatibility wrapper: implements `IEnemyTargetReceiver`, `SetDestination` throttled to `pathUpdateInterval` (default 0.2s — fixes "no SetDestination every frame" acceptance), per-attack `Debug.Log` removed. Behavior on `Enemy.prefab` unchanged otherwise; the prefab keeps working until it is migrated to `MeleeEnemyBrain` in Editor.
  - `Assets/test/GameManager.cs` `SpawnEnemy` and `SpawnEncounterEnemy` resolve `IEnemyTargetReceiver` via `GetComponent` — no `SimpleEnemyAI` fallback branch (TZ §5.4 simplified migration).
  - `dotnet build Assembly-CSharp.csproj` ran clean (zero CS errors/warnings) after temporarily injecting the new files into the csproj for verification; Unity will regenerate the csproj on next Editor refresh and pick the files up automatically.
  - **Pending in Unity Editor (next step):** (1) create `Drone.asset` and `Crawler.asset` `EnemyData` SOs via the *Create > Void Survivor > Enemies > Enemy Data* menu — fill in role/stats/attack/spawn fields per ENEMY_AI_TZ §6.1/§6.2 (but verify Crawler/Drone speeds against `PlayerController.moveSpeed = 10` before locking values; spec defaults of 3.6/4.8 are unkiteable-by-walking but also un-catch-uppable since player walk = 10); (2) either build `Enemy_Drone.prefab` / `Enemy_Crawler.prefab` variants of `Enemy.prefab` with `MeleeEnemyBrain` + the SOs, or swap the component on `Enemy.prefab` itself; (3) playtest legacy wave + encounter modes; HP orb / stagger / glory-kill / `OnEnemyKilled` should all still fire because `Health` and `GameManager` event contracts were not touched.

## Previous Status (2026-04-26)

- **Phase 1 is COMPLETE.** Movement upgrades, Weapon System (PR A + PR B), Kill-to-Survive (PR A + PR B) shipped and playtested.
- **Phase 2 procedural arena pipeline r4 is complete (PR 2.A–2.E).** PR 2.E was closed 2026-04-24 after user's in-Editor playtest.
- **Phase 2 PR 2.F Visual Fidelity Pass — verified 2026-04-25 by user; "looks better than before, but platforms still flat slabs and some textures stretch".**
- **Phase 2 PR 2.G Anti-stretch + lighting fill — code landed 2026-04-25, Unity verify pending.**
  - `WorldUVScaler` MonoBehaviour + `WorldUVDensityRegistry` (`Assets/Scripts/ProceduralArena/Build/WorldUVScaler.cs`): per-instance MaterialPropertyBlock derives `_BaseMap_ST` (and `_BumpMap_ST` / occlusion / metallic / emission ST) from each box' `lossyScale`. Dominant face = smallest axis = surface normal; UV tile width/height = world width/height of the dominant face × tilesPerMeter from the registry. Kills "Roblox" stretch on big floors and walls without changing meshes.
  - `BuildUtils.SpawnBox` now auto-attaches `WorldUVScaler` to every spawned cube.
  - `ArenaBuildMaterials.CreateSurface` registers per-material density (`slot.textureScale × 0.25` → tiles per meter), enables `enableInstancing = true`, and stops baking `textureScale` into the material's `_BaseMap_ST` (per-instance MPB now drives tiling).
  - New `ArenaBuilder.BuildSingleEdgeStrips` — thin emissive strips along floor-to-wall seams (skips door cells) for "panel-light" feel.
  - New `ArenaBuilder.BuildSingleFillLights` — center + four quadrant **Spot** lights pointing straight down (110° outer / ~60° inner) at `wh-0.45` height; intensity = `max(2.5, biome.accentLightIntensity × 1.6)`, range = `wh + 6m`. Quadrant lights only on arenas ≥ 6×6 cells; small Start/Shop/Rest arenas get just the center fill. Spots replace earlier Point fill lights — points lose ~95% to inverse-square falloff before reaching player height, spots focus the cone where it matters.
  - New `ArenaBuilder.SpawnCeilingLamp` — co-spawned at every fill-light position. Visible fixture: dark mounting bracket (uses `mats.ceiling`/`mats.wall`) flush to the ceiling + bright emissive panel (`mats.lampPanel`, 2.2 m square) hanging 8 cm below. Mounted 4 cm below the ceiling tile to avoid z-fight. New `mats.lampPanel` is intentionally biome-agnostic warm-white at intensity 4.5 so panels read as actually lit on every biome.
  - `PC_RPAsset.asset`: `m_AdditionalLightsPerObjectLimit` 4→8 (so fill + exit + pylon point lights coexist on one renderer), `m_ShadowDistance` 50→60.
- **Phase 2 PR 2.F Visual Fidelity Pass — closed 2026-04-25 after user playtest.**
  - Camera `m_RenderPostProcessing` enabled in `Assets/test.unity` + SMAA on (Antialiasing=2); post-processing is now actually rendered.
  - `PC_Renderer.asset` SSAO intensity raised 0.4 → 0.85, DirectLightingStrength 0.25 → 0.35, Radius 0.3 → 0.35.
  - `BiomeSurfaceDefinition` got `bumpScale`, `parallaxStrength`, `detailAlbedoResourcePath`, `detailTextureScale`, `detailStrength` per-slot fields. `ArenaBuildMaterials.ApplyTextureSet` wires them into URP Lit (`_BumpScale`, `_Parallax`, `_DetailAlbedoMap`, `_DETAIL_MULX2` keyword).
  - `BiomeDefinition` got `BiomePostProcessing` block (bloom / colorFilter / exposure / contrast / saturation / vignette) + `accentLightIntensity` / `accentLightRange` / `exitLightColor` for runtime point lights.
  - New `ArenaPostProcessingController` (auto-added to `ArenaFlowController`) spawns a runtime Global Volume with Bloom + ColorAdjustments + Vignette + ACES Tonemapping, priority 100. Biome tint now rides through `ColorAdjustments.colorFilter` instead of abusing `RenderSettings.ambient*`.
  - `ArenaFlowController.SpawnReflectionProbe` adds a realtime box-projected reflection probe at arena center after each build (one-shot `RenderProbe`), so metal reacts.
  - `ArenaBuilder.BuildSingleExits` + `SpawnAtmospherePylon` now attach actual URP Point Lights on exit markers and atmosphere pylons, color/intensity/range driven by biome.
  - Fog bug fixed: `ApplyBiomeAtmosphere` now uses `fogStrength` as the Lerp t (previously clamped to ≥0.92, so any biome with fogStrength=0 still got 92% of biome.fogColor).
  - Ambient tint nudge softened (0.35/0.25/0.30) because the heavy color work now lives in post-processing.
- **Phase 2 PR 2.E closed 2026-04-24** after user playtest.
  - `BiomeDefinition` uses material-slot-driven biome data.
  - Approved PR 2.E textures plus companion maps were copied under `Assets/Resources/ProceduralArena/Biomes/`.
  - `ArenaBuildMaterials` now resolves full companion texture sets with Resources fallback.
  - `Assets/Editor/ProceduralArena/ProceduralArenaTextureImportUtility.cs` now auto-reimports PR 2.E textures with readable/normal/linear import rules.
  - `ArenaBuilder.BuildSingle` now splits collidable architecture from non-solid overlays/decor and tones down center accents / contamination.
  - `ArenaFlowController` now applies a stronger biome fog and ambient tint pass during arena transitions.
  - `ArenaDebugGizmos -> r4 / Generate + Build Single Arena` now uses biome-aware materials too.
  - Follow-up tuning on 2026-04-24 reduced over-bright cyan in `Biome_VoidStation`, temporarily disabled `Parkour` selection in run generation, and added hard safeguards so `Start` / `Shop` / `Rest` arenas stay flat with reduced edge decor.
  - `dotnet build Assembly-CSharp.csproj` passed on 2026-04-24 after the follow-up fixes.
  - User Unity Editor test reported the PR 2.E result is "more or less normal", so the milestone is accepted for now.
- Stable high-level knowledge base: [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- Roadmap: [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- TZ: [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) - APPROVED r4.

### Future Design Note: Arena Complex / Connected Arena Rooms

Captured 2026-04-26 from the user's sketch. The desired future direction is a larger generated map made from several large combat rooms / arena halls connected directly by wide gates in shared walls, not by long corridors.

- Treat this as deferred architecture, not the immediate task.
- Finish PR 2.G verification, PR 2.H beveled prefabs, and at least the first Phase 3 enemy-AI layer before implementing it.
- Preferred future shape: one `ArenaRoot`, one runtime NavMesh bake, 3-6 large room nodes, direct `ArenaDoorLink` gates, staged room clears, and one final run exit.
- Do not revive old corridor-heavy BSP as the main path. Reuse the current single-room builder/material/post-fx/UV/light/prefab work as reusable room-level building blocks.

---

## Current Goal

**Phase 3 — Enemy AI.** Decided 2026-04-26: PR 2.H (beveled prefabs) and the hand-authored structures idea are deferred until at least the first Phase 3 enemy-AI pass lands. Visual coupling between Phase 2.H and Phase 3 is minimal (NavMesh re-bakes per arena, `combatSpawnPoints` are independent, glory-kill / stagger / pooling are orthogonal), so deferring is safe. Only real follow-up cost: a balance pass on ranged enemies once new cover structures change line-of-sight density — that's normal Phase 6 work.

First Phase 3 task per [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md) §"Phase 3: Enemy AI": **refactor `Assets/test/SimpleEnemyAI.cs` into a state-machine base** (issue #5), then build out the four-role core (Drone, Crawler, Plasma Spitter/Sentinel, Station Brute) on top of it. AI improvements + spawning composition come after. Gravity Node is optional after the core is stable; Void Warden is not part of the near-term Phase 3 scope.

Phase 3 master spec now lives in [ENEMY_AI_TZ.md](C:/Users/assam/DiplomGame/docs/ENEMY_AI_TZ.md) (created 2026-04-27). It supersedes broad brainstorming for the near-term implementation scope: four main enemy roles, simple state-machine AI, spawn composition by budget/weights/arenaIndex, readable telegraphs/attack slots, and pooling only after enemy contracts stabilize. Full AI Director, Arena Complex spawn logic, Shield Drone, complex Brute charge, and final art/VFX are deferred.

### Deferred Phase 2 work (resume after first Phase 3 enemy-AI pass)

- **PR 2.H — Beveled prefabs.** Replace `GameObject.CreatePrimitive(Cube)` slabs with proper meshes that have chamfered edges. Platforms (`mats.platform` in `BuildSingleVerticality`) are priority #1 — they're the worst-looking element per user screenshot 2026-04-25. Open question when resuming: Asset Store modular sci-fi pack (Kenney/Synty, CC0/cheap, fast, may fight the runtime PBR pipeline from PR 2.E) vs. Blender custom meshes (full control, diploma-friendly, learning curve since user is first-time in Blender).
- **PR 2.H1 — Hand-authored structures (idea captured 2026-04-26).** Pre-made structure variants (bunkers, sandbag lines, pillar clusters, broken arches, sniper nests; atmospheric: crashed pods, generator stacks, terminals, dead drone heaps) spawned into existing cover/decor slots via `ArenaCoverPlanner` budget. Implementation sketch: `StructureDefinition` SO with `List<BoxPart>` (offset, size, materialSlot) — stays in code, reuses `BuildUtils.SpawnBox` so `WorldUVScaler` + per-biome materials work for free, no Blender / no Asset Store, fully deterministic via a new `structureRng` sub-stream in `SingleArenaGenerator`. Bias: 1–2 structures on M arenas, 2–3 on L; biome / arena-category gates which set is eligible. This may close ~60% of PR 2.H value (silhouette readability) without bevels — re-evaluate scope of PR 2.H once H1 lands.
- **Arena Complex / Connected Arena Rooms** (existing deferred design note from 2026-04-26 sketch) stays deferred to Phase 3.5 — revisit after first enemy-AI pass + PR 2.H/H1.

### What's still open from Phase 2

- PR 2.G visual verify is treated as informally accepted (user moved on to Phase 3 planning); if regressions surface during Phase 3 playtests, re-open against `WorldUVScaler` / fill-light tuning.
- `Parkour` profile is still excluded from `DefaultRunConfig` — keep excluded until PR 2.D ramp-axis review (see "What Is Not Done Yet" below).

- PR 2.A - SingleArenaGenerator + shape / cover / exit planners + size presets + per-arena 10-25m ceiling - **DONE (verified 2026-04-21)**.
- PR 2.B - Run Graph + transitions + fade + door-choice + Victory/GameOver - **DONE (verified 2026-04-21)**.
- PR 2.C - Async `NavMeshSurface.UpdateNavMesh`, encounter integration, soft-lock barriers, clear conditions, `SimpleEnemyAI.isOnNavMesh` guard - **DONE (verified 2026-04-22)**.
- PR 2.D - Verticality + biome selection + new arena type profiles + `arenaIndex` scaling + runtime debug overlay - **DONE (verified 2026-04-22)**.
- PR 2.E - second pass adds companion PBR maps, texture import automation, collidable architecture/props, quieter Alien Nexus composition, stronger fog/emissive readability, Parkour disable guard, and flat-arena safeguards - **DONE (verified 2026-04-24 by user Unity Editor test)**.

Scene wiring reference (`test.unity`):
- `Run` GameObject with `RunController` (config -> `DefaultRunConfig`, flow -> `ArenaHost`).
- `Run/ArenaHost` GameObject with `ArenaFlowController` (buildConfig -> `TestArenaConfig`).
- `GameManager` - set `useEncounterMode = true` for PR 2.C/2.E flow (legacy wave loop still works when `false`).
- Legacy `ArenaDebug` GameObject - keep disabled while `Run` is active, otherwise two arenas overlap.
- Player must have tag `Player`. CharacterController teleports via `cc.enabled = false/true`.

---

## What Is Already Done

**Phase 1:**
- Movement is tuned and playtested.
- Weapon system is modular and shipped under `Assets/Scripts/Combat/Weapons/`.
- Kill-to-Survive systems are in place under `Assets/Scripts/Combat/{Pickups,Enemies,Player}/`.

**Phase 2 (r4):**
- `Assets/Scripts/ProceduralArena/Arena/` - single-arena generation, shapes, cover, exits, verticality, biome metadata.
- `Assets/Scripts/ProceduralArena/Run/` - run graph, transitions, door choice, runtime debug overlay.
- `Assets/Scripts/ProceduralArena/Navigation/` - async NavMesh bake.
- `Assets/Scripts/ProceduralArena/Encounter/` - encounter orchestration and soft-lock barriers.
- `Assets/Scripts/ProceduralArena/Build/` - single-arena build path plus PR 2.E visual layers, companion PBR map resolver, and architecture collision split.
- `Assets/Editor/ProceduralArena/` - PR 2.E texture import helper for readable normal/mask maps.
- `Assets/Resources/ProceduralArena/Biomes/` - approved PR 2.E texture copies plus companion maps for runtime biome fallback.
- `Assets/ArenaProfiles/` - Start/Combat/Boss/Elite/Parkour/Shop/Rest profiles plus `Biome_VoidStation` and `Biome_AlienNexus`.
- `Assets/ArenaProfiles/DefaultRunConfig.asset` - `Parkour` removed from active run pools for now.

---

## What Is Not Done Yet

- `Parkour` is intentionally disabled in generated runs for now; if it appears again, check `DefaultRunConfig.asset` and `RunGraphGenerator.IsSelectableProfile(...)`.
- PR 2.D/2.E review notes to keep in mind before re-enabling Parkour or extending verticality:
  - east/west ramp geometry should be rechecked because ramp pitch currently assumes the north/south local axis.
  - biome atmosphere mutates global `RenderSettings`; restore `fogMode` too if scenes start sharing multiple visual controllers.
- If bright cyan `VoidStation` blocks/trim reappear, verify that Unity reimported the updated `Biome_VoidStation.asset` and the copied textures; the intended follow-up uses neutral `Panel_007` for `floorAccent`, `wallTrim`, and `propMaterial`, with much weaker emissive accents.
- `test.unity` is still not in Build Settings (issue #3).
- Enemy pooling (issue #6), projectile pooling (issue #7), and `SimpleEnemyAI` refactor (issue #5) remain deferred beyond this milestone.
- Arena Complex / Connected Arena Rooms is only a captured future direction for now. Do not implement it before PR 2.G/2.H and the first Phase 3 enemy-AI architecture pass unless the user explicitly reprioritizes it.

---

## Recommended Next Task

PR 3.A/B/C are verified. PR 3.D code and baseline Unity wiring are in: `SpawnProfile_Combat.asset` references Drone / Crawler / Spitter / Brute and is assigned to `Arena_Combat_M.asset` plus `Arena_Elite_L.asset`. Next is a focused PR 3.D playtest of the role-mix curve.

**A. Playtest PR 3.D composition.**
1. `GameManager.useEncounterMode = true`. Start a fresh run (`Ctrl+P`).
2. Walk into the first Combat arena (arenaIndex 1 typically — Start is 0).
   - Expected: roster mix of Drone + Crawler + (≥arenaIndex 1) Spitter. Budget = 8 + 1×3 = 11.
   - With Drone cost 1, Crawler 2, Spitter 3, the composer should produce ~5-7 enemies of mixed types.
3. Arena 2+: Brute can appear (cost 6, max 1). With budget = 8 + 2×3 = 14 you should occasionally see 1 Brute + 4 fodder.
4. Verify in Console:
   - **No `[EnemySpawnComposer] Fallback`** warnings during normal play (a warning means a profile is misconfigured — null roster, all entries gated out, etc.).
   - **No** errors. HP-orb / stagger / glory-kill still fire.
5. Try a misconfiguration on purpose: temporarily clear all entries on `SpawnProfile_Combat`. Save. Reload run. Console should print:
   `[EnemySpawnComposer] Fallback to caller's default — spawn profile is null or has no entries.` — then the legacy `GameManager.enemyPrefab` spawns. Confirms the warning path works. Restore the entries.

**B. Optional tuning after the playtest.**
- If Combat and Elite feel too similar, clone `SpawnProfile_Combat.asset` into a separate Elite profile and tune weights / `maxRanged` there.
- Keep `Arena_Boss_L.asset` on the legacy path for now; boss composition belongs with the later boss enemy task.

**C. Then PR 3.E (Combat Readability + Active Attack Slots).**
- `ActiveAttackSlotManager` so heavy/melee/ranged slots aren't all resolving at once.
- **Brute slam telegraph visual** (deferred from PR 3.C — ground decal/circle while telegraphing).
- General telegraph layer for all enemies (placeholder emissive flash on Crawler / Spitter wind-ups).
- Fair-spawn delay for close spawns.

---

### Earlier-PR notes (kept for context)

PR 3.C Editor steps (now done — Brute):

**A. Author the Brute `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Brute`. Fields per TZ §6.4:

**A. Author the Brute `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Brute`. Fields per TZ §6.4:
- Identity: enemyId `brute`, displayName `Station Brute`, role `Tank`.
- Stats: maxHealth 260, moveSpeed 2.2, damage 28.
- Attack: attackRange 3.0, attackCooldown 3.0, telegraphTime 0.9, recoveryTime 1.1.
- Ranged: leave zero (Brute has no projectile).
- Heavy / Brute: **slamRadius 4.0**, **slamHitMask = Default + Player layers** (open the LayerMask dropdown, tick the layers that should be hit by the slam — at minimum the player's layer; add any destructible layers later if introduced).
- Spawn: spawnCost 6, minArenaIndex 2, **maxAlive 1** ← do NOT raise this until PR 3.E ships the slot manager. spawnWeight 1.

**B. Build the Brute prefab variant.** Right-click `Assets/Prefabs/Enemy.prefab` → **Create → Prefab Variant** → rename `Enemy_Brute`. Open it. Remove `SimpleEnemyAI`. Add `BruteEnemyBrain`. Drag `Brute.asset` into `data`. Leave `Slam Origin Offset` at (0, 0, 0) — that's the feet for the standard 1×2×1 capsule. Leave `Draw Slam Gizmo` ✓ (Editor-only, no runtime cost). Save.

Optional polish: scale the visual mesh up (Transform Scale 1.4 / 2.0 / 1.4) so the Brute reads as visually heavier than a Drone. Health bar prefab will scale with parent.

**C. Playtest.** Temporarily set `GameManager.enemyPrefab` to `Enemy_Brute.prefab` and run.
- Brute should approach slowly (~2.2 m/s, easy to outwalk).
- At ≤3m it stops, faces you over ~0.9s, then slams.
- If you stay within ~4m of the slam origin at impact, you take 28 damage. Step out → no damage.
- Open the Scene view tab while paused — you should see an orange wire-sphere around the Brute during Telegraph showing the slam radius.
- Health/stagger/glory-kill/HP-orb still work.

**D. Test combination encounters (optional, before PR 3.D).** Set `GameManager.enemyPrefab` back to Drone and manually drop a single Brute into the scene at runtime via Hierarchy duplication, OR temporarily edit `EncounterController` to spawn a mix. Verify the Brute creates target-priority pressure when combined with Drones.

After playtest passes, PR 3.A/B/C are all clear of code work. Next is **PR 3.D — Spawn Composition**: `EnemySpawnEntry`, `EnemySpawnProfile`, `EnemySpawnComposer`, and the `EncounterController` → `GameManager.BeginEncounter` path that drives role mix by `arenaIndex`. After 3.D the per-arena spawn finally produces real role mixes instead of one prefab repeated.

**E. Then PR 3.E (combat readability + active attack slots)** ships the `ActiveAttackSlotManager` so heavy/melee/ranged attacks don't all resolve at once even with many enemies alive.

---

### Earlier-PR notes (kept for context)

PR 3.B Editor steps (now done — Spitter):

**A. Build the projectile prefab.**
1. Project window → right-click in `Assets/Prefabs/` → **Create → 3D Object → Sphere**. Actually simpler: GameObject → 3D Object → Sphere in the Hierarchy (any scene), scale to ~0.4, then drag into `Assets/Prefabs/` to make a prefab; delete the scene instance.
2. Open the new prefab. On the root: set the `SphereCollider` to `Is Trigger = true`, radius ~0.25. Remove the default Mesh Collider if Unity added one.
3. Add Component → `Enemy Projectile` script. `Lifetime` = 4 seconds (default).
4. Optional polish: assign a bright emissive material to the MeshRenderer so the projectile reads as plasma. A `TrailRenderer` child (short, color-matched) helps dodgeability.
5. Rename the prefab `EnemyProjectile.prefab`. Save.

**B. Author the Spitter `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Spitter`. Fields per TZ §6.3:
- Identity: enemyId `spitter`, displayName `Plasma Spitter`, role `Ranged`.
- Stats: maxHealth 90, moveSpeed 3.0, damage 12.
- Attack: attackRange 18, attackCooldown 2.0, telegraphTime 0.7, recoveryTime 0.5.
- Ranged: drag `EnemyProjectile.prefab` into `projectilePrefab`. projectileSpeed 10. preferredDistance 12. lineOfSightCheckInterval 0.2.
- Heavy / Brute: leave zero.
- Spawn: spawnCost 3, minArenaIndex 1 (TZ §6.3 acceptance), maxAlive 4, spawnWeight 1.

**C. Build the Spitter prefab variant.** Right-click `Assets/Prefabs/Enemy.prefab` → **Create → Prefab Variant** → rename `Enemy_Spitter`. Open it. Remove `SimpleEnemyAI`. Add `RangedEnemyBrain`. Drag `Spitter.asset` into `data`. Leave `Muzzle Offset` at (0, 1, 0). `Distance Band` = 1.5. `LoS Block Mask` — set to **Default** layer (so walls/cover block, but enemy layer 6 is excluded). Save.

**D. Playtest.** Easiest: temporarily set `GameManager.enemyPrefab` to `Enemy_Spitter.prefab` and run.
- Spitter should hold ~12m distance, back off if you rush in, fire visible plasma every ~2s.
- Stand behind a cover pillar — Spitter must NOT shoot through it.
- Strafe perpendicular to the projectile during flight — it should be dodgeable.
- Health, stagger, glory-kill, HP-orb drop should still work (contracts unchanged).

After playtest passes, restore `GameManager.enemyPrefab` to whatever the spawn composer (PR 3.D, future) will use, OR leave it on Drone for now and let PR 3.D drive variety.

**E. Then PR 3.C (Station Brute).** New `BruteEnemyBrain` with `Physics.OverlapSphere` slam at impact frame. `EnemyData` already has `slamRadius`/`slamHitMask` fields ready.

---

PR 3.A Editor steps (now done — Drone/Crawler):

1. **Create EnemyData SOs in Unity Editor.** *Right-click in Project view → Create → Void Survivor → Enemies → Enemy Data* twice. Name them `Drone.asset` and `Crawler.asset` (suggested location: `Assets/EnemyData/`). Tune via ENEMY_AI_TZ §6.1 / §6.2 — but **adjust Drone `moveSpeed`/Crawler `moveSpeed` upward** before locking values, because `PlayerController.moveSpeed = 10`. Suggested override: Drone ≈ 6.5, Crawler ≈ 9.0 (faster than nothing but still slower than dash burst of 25 m/s). Document the deviation in PROGRESS change log when committed.
2. **Build prefab variants or swap component on `Enemy.prefab`.** Easiest: open `Assets/Prefabs/Enemy.prefab`, replace `SimpleEnemyAI` MonoBehaviour with `MeleeEnemyBrain`, drag in the Drone EnemyData asset, save as `Enemy_Drone.prefab`. Repeat with Crawler SO for `Enemy_Crawler.prefab`. Wire `GameManager.enemyPrefab` to `Enemy_Drone.prefab` for now.
3. **Playtest both modes.** Set `useEncounterMode = true`, run through the 5-arena run. Verify (a) enemies spawn on NavMesh, (b) `Health.onDeath` still triggers HP-orb drops + barrier-open, (c) stagger pulses at low HP, (d) glory-kill detector still works, (e) no Console spam, (f) no `SetDestination on inactive agent`. Then flip `useEncounterMode = false` and confirm legacy wave loop also works.
4. **Then PR 3.B (Plasma Spitter).** New `RangedEnemyBrain` + `EnemyProjectile` + LoS + hold-distance. The `EnemyData` SO already has the ranged stub fields (`projectilePrefab`, `projectileSpeed`, `preferredDistance`, `lineOfSightCheckInterval`); no further `EnemyData` schema changes expected.
5. Keep `GameManager` encounter API compatible with the Phase 2 `ArenaFlowController` / `EncounterController` path — already done in PR 3.A via `IEnemyTargetReceiver`, just don't accidentally re-introduce `SimpleEnemyAI`-typed `GetComponent` calls.
6. Re-enable or revisit Parkour only after the ramp-axis review and gameplay readability pass (unrelated to Phase 3, parked).
7. Keep the future Arena Complex idea in mind when designing PR 3.D spawn composition: room-local spawn groups and staged clears should remain possible later.

---

## Files Most Relevant For The Next Task

- [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- [Assets/Prefabs/Enemy.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Enemy.prefab)
- [Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs)
- [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- [Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs)
- [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/docs/KNOWN_ISSUES.md)

---

## Do Not Break

- Movement feel (walk/jump/double-jump/dash/slide/air-control)
- Weapon framework under `Assets/Scripts/Combat/Weapons/`
- Kill-to-Survive seams (`PlayerStats`, `EnemyLootTable`, `IGloryKillPolicy`)
- `Health` event contracts (`onHealthChanged`, `onDeath`, `onTakeDamage`)
- `GameManager.OnEnemyKilled` event
- `GameManager` legacy wave loop when `useEncounterMode = false`
- Zero `UnityEngine.Random` inside `Assets/Scripts/ProceduralArena/**`
- `[DEPRECATED]` BSP headers and files kept for diploma reference

---

## Immediate Manual Setup Reminder

- On `GameManager` in `test.unity`, ensure `useEncounterMode = true`.
- Keep `Run` active and legacy `ArenaDebug` disabled during runtime tests.
- For walk-through debug without killing enemies, set `skipClearCondition = true` in `DefaultRunConfig.asset`; keep it `false` for normal encounter testing.
- Start a fresh run after code changes; `RunGraph` and already-built arena roots should not be reused when checking enemy AI behavior.
- `test.unity` still should be added to Build Settings later (outside PR 2.E scope).

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.
