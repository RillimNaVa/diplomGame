# AI Handoff

Short-lived current-task handoff for the next AI session.
For stable architecture / roadmap / known issues, see:

- [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/docs/KNOWN_ISSUES.md)
- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md)

---

## Current Status (2026-04-25)

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

---

## Current Goal

**Unity-side verification of PR 2.G Anti-stretch + lighting fill.** Reload Unity (so `WorldUVScaler` is recognised as a `MonoBehaviour`), play through a 5-arena run, and confirm:
1. Big floor/wall textures no longer stretch — bricks/panels read at a consistent scale regardless of arena size.
2. Each arena has a soft fill of light from above (no pitch-black corners), but no surface looks blown out.
3. Thin emissive strips along the floor-to-wall seam are visible and pick up the biome accent color.
4. FPS is within ±5–10% of PR 2.F baseline (extra point lights × 5 per arena should fit under the bumped `AdditionalLightsPerObjectLimit=8`).
5. No URP "too many additional lights" warnings in Console.
6. Small arenas (Start/Shop/Rest, < 6×6 cells) only get the center fill light, not all 5.

After PR 2.G verify: pick the path for **PR 2.H — Beveled prefabs** (Asset Store modular sci-fi pack vs. Blender custom meshes). Platforms specifically must move off scaled `CreatePrimitive(Cube)` ASAP — they're the worst-looking element in the current build (per user screenshot 2026-04-25).

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

---

## Recommended Next Task

1. Start Phase 3 with `SimpleEnemyAI` refactor into a small state-machine base while preserving the current `Health` and `GameManager.OnEnemyKilled` contracts.
2. Add the first enemy-type split gradually: keep the current melee chaser as Drone/Grunt, then introduce ranged Sentinel after the base is stable.
3. Keep `GameManager` encounter API compatible with the Phase 2 `ArenaFlowController` / `EncounterController` path.
4. Re-enable or revisit Parkour only after the ramp-axis review and gameplay readability pass.

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
