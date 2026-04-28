# Void Survivor - Known Issues

## Purpose

This file tracks known bugs, technical debt, architectural weaknesses, and important unresolved project risks.

Use this file when:

- a problem is confirmed but not fixed yet
- a system is known to be temporary
- a performance risk is identified
- a future refactor is clearly needed

This file should be concrete and action-oriented.

---

## Status Legend

- `Open` - confirmed and not yet addressed
- `Partial` - some mitigation exists, but the issue is not fully resolved
- `Planned` - not fixed yet, but there is already a defined direction or specification
- `Closed` - fixed and safe to archive or remove later

---

## Recent Closures (2026-04-17 → 2026-04-24)

- Issue #1 (`PlayerController` overloaded) — **Closed**, combat extracted during Weapon System refactor.
- Issue #2 (No real weapon system) — **Closed**, `WEAPON_SYSTEM_TZ.md` implementation shipped.
- Issue #8 (Temporary melee) — **Closed**, replaced by `Void Blade` weapon.
- Issue #13 (PR 2.E visual pass pending Unity verify) — **Closed**, user verified the result in Unity Editor on 2026-04-24.

Kept open for Phase 3+: #3, #4, #5, #6, #7, #9, #10, #11, #12, #14, #15, #16, #17, #18, #19, #20.

---

## Issues

## 1. `PlayerController` has too many responsibilities

- Status: `Closed` (2026-04-17)
- Severity: High
- Affected files:
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- Resolution:
  - Combat logic (shooting, melee, projectile spawning, tracer lifecycle, recoil triggering) was extracted during the Weapon System refactor. `PlayerController` now owns only movement, look, dash/slide, input forwarding, and the new `SetSpeedMultiplier` hook used by `KillStreakTracker`. The class is now reasonably sized.

## 2. Real weapon system does not exist yet

- Status: `Closed` (2026-04-17)
- Severity: High
- Affected files:
  - [Assets/Scripts/Combat/Weapons/](C:/Users/assam/DiplomGame/Assets/Scripts/Combat/Weapons/)
  - [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/docs/WEAPON_SYSTEM_TZ.md)
- Resolution:
  - Modular weapon system shipped as specified: `WeaponManager`, `WeaponBase`, `WeaponDefinition` (ScriptableObject), `[SerializeReference]` `FireModeBase` hierarchy, five weapons, switching, ammo, reload. Playtested. TZ marked COMPLETED.

## 3. `Assets/test.unity` is not added to Build Settings

- Status: `Open`
- Severity: Medium
- Affected files:
  - [ProjectSettings/EditorBuildSettings.asset](C:/Users/assam/DiplomGame/ProjectSettings/EditorBuildSettings.asset)
  - [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)
  - [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- Problem:
  - Scene reload uses active scene build index, but the actual gameplay scene is not in Build Settings
- Impact:
  - Reload behavior can fail or behave inconsistently
- Fix direction:
  - Add `Assets/test.unity` to Build Settings / Build Profiles scene list

## 4. Serialized move speed in scene does not match intended default

- Status: `Open`
- Severity: Low
- Affected files:
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
  - [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)
- Problem:
  - Code default is `10`, but the scene still has serialized `moveSpeed: 6`
- Impact:
  - Confusing behavior
  - Progress tracker may say movement speed upgrade is done while scene runtime still behaves differently
- Fix direction:
  - Update inspector scene value intentionally and verify in play mode

## 5. `SimpleEnemyAI` scales poorly

- Status: `Partial` (2026-04-28)
- Severity: Medium
- Affected files:
  - [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- Problem:
  - Original prototype behavior called `SetDestination()` every frame, computed distance every frame, logged every attack, and fetched `Health` during the attack path.
  - PR 3.A mitigated the worst prototype costs: `SimpleEnemyAI` now implements `IEnemyTargetReceiver`, throttles `SetDestination()` through `pathUpdateInterval`, and no longer logs every attack.
  - Full enemy behavior is now moving to `EnemyBrainBase` / `MeleeEnemyBrain` / `RangedEnemyBrain` / `BruteEnemyBrain`, but the legacy wrapper still exists for compatibility and still is not the long-term scalable AI layer.
- Impact:
  - CPU waste
  - Worse scaling with more enemies
  - Editor slowdown due to logging
- Fix direction:
  - Continue migration to the Phase 3 brain classes, then retire or keep `SimpleEnemyAI` only as a legacy fallback.
  - PR 3.E should add active attack slots / readability so many enemies do not resolve attacks at once.

## 6. Dead enemies accumulate in the scene

- Status: `Open`
- Severity: Medium
- Affected files:
  - [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
  - [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- Problem:
  - Enemies are disabled after death instead of being pooled or cleaned up
- Impact:
  - Scene hierarchy growth
  - Memory growth over longer sessions
  - Poor scalability for endless waves
- Fix direction:
  - Later object pooling or enemy lifecycle redesign

## 7. Projectile and combat effects still use instantiate/destroy flow

- Status: `Open`
- Severity: Medium
- Affected files:
  - [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- Problem:
  - Combat objects are repeatedly created and destroyed
- Impact:
  - GC pressure
  - poor scaling for sustained firefights
- Fix direction:
  - Introduce object pooling in a later performance phase

## 8. Current melee is temporary and should be replaced

- Status: `Closed` (2026-04-17)
- Severity: Medium
- Affected files:
  - [Assets/Scripts/Combat/Weapons/](C:/Users/assam/DiplomGame/Assets/Scripts/Combat/Weapons/)
- Resolution:
  - Temporary melee was removed from `PlayerController` and replaced by the `Void Blade` weapon using `MeleeArcFireMode` inside the modular weapon system. `GloryKillDetector` observes `WeaponBase.OnFired` for the `void_blade` id to apply bonus damage and heal via `PlayerStats`.

## 9. Terrain generation in `SampleScene` is expensive in Editor

- Status: `Open`
- Severity: Medium
- Affected files:
  - [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)
  - [Assets/Scenes/SampleScene.unity](C:/Users/assam/DiplomGame/Assets/Scenes/SampleScene.unity)
- Problem:
  - `[ExecuteAlways]` + `autoUpdate` terrain generation can trigger heavy editor-side work
- Impact:
  - High CPU load
  - noisy/fan-heavy editor behavior
- Fix direction:
  - Disable auto-update or redesign terrain workflow if still needed

## 10. Current URP/render settings are heavy for a prototype

- Status: `Open`
- Severity: Medium
- Affected files:
  - [Assets/Settings/PC_RPAsset.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_RPAsset.asset)
  - [Assets/Settings/PC_Renderer.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_Renderer.asset)
  - [Assets/Settings/SampleSceneProfile.asset](C:/Users/assam/DiplomGame/Assets/Settings/SampleSceneProfile.asset)
  - [ProjectSettings/QualitySettings.asset](C:/Users/assam/DiplomGame/ProjectSettings/QualitySettings.asset)
- Problem:
  - VSync off, SSAO enabled, shadows and post-processing increase render cost
- Impact:
  - High power draw in Editor
  - harder performance profiling because baseline is already expensive
- Fix direction:
  - Tune after gameplay systems stabilize

## 11. World-space enemy health bars may not scale well

- Status: `Open`
- Severity: Low
- Affected files:
  - [Assets/Prefabs/UI/EnemyHealthBar.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/UI/EnemyHealthBar.prefab)
  - [Assets/test/EnemyHealthBarView.cs](C:/Users/assam/DiplomGame/Assets/test/EnemyHealthBarView.cs)
- Problem:
  - One world-space canvas per enemy is not ideal at higher enemy counts
- Impact:
  - UI overhead as waves grow
- Fix direction:
  - Keep for prototype; optimize later if enemy counts increase significantly

## 12. Generated Unity `.csproj` can go stale after manual file creation outside Editor

- Status: `Partial` (2026-04-22)
- Severity: Low
- Affected files:
  - [Assembly-CSharp.csproj](C:/Users/assam/DiplomGame/Assembly-CSharp.csproj)
  - [Assets/Scripts/ProceduralArena/Arena/BiomeDefinition.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/BiomeDefinition.cs)
  - [Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs)
  - [Assets/Scripts/ProceduralArena/Run/ArenaRuntimeDebugOverlay.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaRuntimeDebugOverlay.cs)
- Problem:
  - After PR 2.D files were added manually, `dotnet build Assembly-CSharp.csproj` still used a stale generated project and did not include the new scripts.
- Impact:
  - External C# compile checks can report false negatives until Unity regenerates project files.
- Fix direction:
  - Open Unity and trigger Refresh/Reimport or regenerate project files; do not hand-edit generated `.csproj` as a durable fix.

## 16. Platforms still authored as scaled `CreatePrimitive(Cube)` slabs

- Status: `Open` (2026-04-25)
- Severity: Medium (visual)
- Affected files:
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs) — `BuildSingleVerticality` -> `mats.platform`
- Problem:
  - Verticality platforms (and ramps) are thin scaled cube primitives. Even with PR 2.G per-instance UV tiling, they read as thin texture-stretched slabs without volume, edges, or supports — by far the worst-looking element in the current build (confirmed via user 2026-04-25 screenshot).
- Impact:
  - Single-handedly breaks visual fidelity across `Combat` / `Elite` / `Parkour` / `Boss` arenas.
- Fix direction:
  - PR 2.H: replace `mats.platform` cube spawn with a beveled-edge prefab (Kenney / Synty / Blender), keeping the same placement metadata (`p.center`, `p.size`, `p.yawDeg`). Same for ramps.

## 17. PR 2.G per-instance MPB disables SRP batcher path on textured cubes

- Status: `Open` (2026-04-25)
- Severity: Low (perf)
- Affected files:
  - [Assets/Scripts/ProceduralArena/Build/WorldUVScaler.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/WorldUVScaler.cs)
  - [Assets/Scripts/ProceduralArena/Build/BuildUtils.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/BuildUtils.cs)
- Problem:
  - `WorldUVScaler` writes `_BaseMap_ST` per renderer via `MaterialPropertyBlock`. Unity URP's SRP batcher excludes renderers with non-empty MPB, so each textured cube falls off the batched path. We still get GPU instancing fallback, but draw calls increase vs. PR 2.F.
- Impact:
  - At ~200 cubes/arena typical, no measurable cost yet. Could matter once we add Phase 3 enemies + projectiles.
- Fix direction:
  - Replace `WorldUVScaler` with a custom URP ShaderGraph that does world-space triplanar sampling — keeps SRP batcher path, also fixes corner seams. Defer until profiling shows it matters.

## 14. PR 2.F runtime cost: per-arena reflection probe + extra point lights

- Status: `Open` (2026-04-25)
- Severity: Low
- Affected files:
  - [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
- Problem:
  - PR 2.F spawns one `Realtime` `ReflectionProbe` per arena (one-shot bake via `RenderProbe()`), and adds Point Lights on every exit and every atmosphere pylon. Shadow-casting is disabled on these point lights, but on URP Forward there is still a per-light cost and the realtime probe re-bakes once on arena entry.
- Impact:
  - At default URP additional-light limits this is fine, but very high pylon counts on `AlienNexus`/`VoidStation` plus 4 exits could push close to the per-camera additional-light cap and add a one-frame hitch when the probe bakes.
- Fix direction:
  - If profiling shows a cost: switch the probe to `Custom` baked cubemaps per biome, or downgrade to a single static probe shared by the whole scene; cap exit/pylon point lights via biome budget.

## 15. Texture binaries still duplicated under `Assets/Resources/ProceduralArena/Biomes/`

- Status: `Open` (2026-04-25, carried over from PR 2.E review)
- Severity: Low
- Affected files:
  - [Assets/Resources/ProceduralArena/Biomes/](C:/Users/assam/DiplomGame/Assets/Resources/ProceduralArena/Biomes)
- Problem:
  - PR 2.E copied the same PBR texture sets into per-biome resource folders instead of referencing a shared library. Repo size is inflated and updating a shared map means editing N copies.
- Impact:
  - Larger git history, easier drift between biomes, slower asset reimport.
- Fix direction:
  - After Phase 2 visuals are locked, deduplicate to a single `Assets/Resources/ProceduralArena/SharedTextures/` and point biome surface definitions at shared resource paths.

## 13. PR 2.E visual pass still needs first Unity import and readability tuning

- Status: `Closed` (2026-04-24)
- Severity: Medium
- Affected files:
  - [Assets/Resources/ProceduralArena/Biomes/](C:/Users/assam/DiplomGame/Assets/Resources/ProceduralArena/Biomes)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuildMaterials.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuildMaterials.cs)
  - [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
  - [Assets/ArenaProfiles/Biome_VoidStation.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_VoidStation.asset)
  - [Assets/ArenaProfiles/Biome_AlienNexus.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_AlienNexus.asset)
- Problem:
  - PR 2.E previously needed Unity-side visual verification after the prototype runtime PBR pipeline, texture import helper, `VoidStation` retune, and flat-arena safeguards were added.
- Impact:
  - Closed after the user tested the result in Unity Editor on 2026-04-24 and accepted it for milestone closure.
- Resolution:
  - Phase 2 PR 2.E is now treated as verified. Any further visual/material work should be handled as Phase 5 polish or as targeted follow-up bugs, not as a blocker for Phase 3.

## 14. East/west vertical ramps may pitch around the wrong axis

- Status: `Open` (2026-04-24)
- Severity: Medium
- Affected files:
  - [Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
- Problem:
  - `ArenaVerticalityPlanner` stores east/west ramp run length on the X axis, but `ArenaBuilder` always applies ramp pitch as `Quaternion.Euler(pitch, yaw, 0)`, which pitches around local X. North/south ramps are likely correct; east/west ramps can become sideways/slanted incorrectly.
- Impact:
  - Low immediate impact because `Parkour` is disabled in generated runs, but Boss can still use verticality and future Parkour re-enable may expose bad ramp geometry or unreliable NavMesh.
- Fix direction:
  - Normalize ramp mesh orientation so the ramp run is always along local Z before yaw, or add axis-aware rotation/size handling for east/west ramps.

## 15. Biome atmosphere does not restore `RenderSettings.fogMode`

- Status: `Open` (2026-04-24)
- Severity: Low
- Affected files:
  - [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- Problem:
  - `ArenaFlowController` caches and restores fog enabled/color/density and ambient colors, but `ApplyBiomeAtmosphere` overwrites `RenderSettings.fogMode` with `ExponentialSquared` and `RestoreAtmosphereDefaults` does not restore the original fog mode.
- Impact:
  - Usually harmless in the current single-scene flow, but it leaks global render state if another scene, tool, or visual controller expects a different fog mode.
- Fix direction:
  - Cache `RenderSettings.fogMode` in `CacheAtmosphereDefaults()` and restore it in `RestoreAtmosphereDefaults()`.

## 16. PR 2.E texture assets are duplicated across runtime biome folders and source docs

- Status: `Open` (2026-04-24)
- Severity: High
- Affected files:
  - [Assets/Resources/ProceduralArena/Biomes/AlienNexus/](C:/Users/assam/DiplomGame/Assets/Resources/ProceduralArena/Biomes/AlienNexus)
  - [Assets/Resources/ProceduralArena/Biomes/VoidStation/](C:/Users/assam/DiplomGame/Assets/Resources/ProceduralArena/Biomes/VoidStation)
  - [docs/textures/](C:/Users/assam/DiplomGame/docs/textures)
  - [Assets/ArenaProfiles/Biome_AlienNexus.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_AlienNexus.asset)
  - [Assets/ArenaProfiles/Biome_VoidStation.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_VoidStation.asset)
- Problem:
  - Several sci-fi texture sets are stored in both biome-specific runtime folders and again under `docs/textures/`; hash checks show real duplicate binary content, not just similar names. This bloats git history and makes future texture changes easy to desynchronize.
- Impact:
  - Larger repository size, slower clone/sync, duplicated import work, and confusing source-of-truth for texture assets.
- Fix direction:
  - Move shared sci-fi runtime textures into a single `Assets/Resources/ProceduralArena/Biomes/Shared/` folder and update biome `resourcePath` references. Keep biome folders only for unique assets. Move source/reference textures out of runtime paths and consider Git LFS or external storage for large source texture archives.

## 17. Runtime PBR map packing is CPU-heavy and keeps generated textures cached

- Status: `Open` (2026-04-24)
- Severity: Medium
- Affected files:
  - [Assets/Scripts/ProceduralArena/Build/BiomeTextureSetResolver.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/BiomeTextureSetResolver.cs)
  - [Assets/Editor/ProceduralArena/ProceduralArenaTextureImportUtility.cs](C:/Users/assam/DiplomGame/Assets/Editor/ProceduralArena/ProceduralArenaTextureImportUtility.cs)
- Problem:
  - `BiomeTextureSetResolver` builds metallic/gloss and emission maps at runtime with `GetPixels` / `SetPixels`, while the import helper marks all biome textures readable. Generated textures are kept in static caches with no explicit cleanup path.
- Impact:
  - Higher memory use, possible first-arena hitch when maps are generated, and stale generated textures during long Editor sessions or when domain reload is disabled.
- Fix direction:
  - Pre-pack metallic/smoothness/mask/emission maps offline or in an Editor-only bake step. Import runtime textures as non-readable where possible. If runtime generation remains as a development fallback, add an explicit cache clear/destroy method for generated `Texture2D` instances.

## 18. Biome fog color blend ignores most of `fogStrength`

- Status: `Open` (2026-04-24)
- Severity: Low
- Affected files:
  - [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- Problem:
  - `ApplyBiomeAtmosphere` uses `Color.Lerp(defaultFogColor, biome.fogColor, Mathf.Clamp01(0.92f + biome.fogStrength * 0.08f))`, so the fog color is almost fully biome-colored even at very low `fogStrength`.
- Impact:
  - Visual tuning is less intuitive: `fogStrength` controls density, but barely controls color influence. This is hard to explain and can make subtle biome atmosphere settings behave too strongly.
- Fix direction:
  - Decide whether biome fog color should be hard-swapped or blended. If blended, use `biome.fogStrength` or a named curve/constant instead of the current hidden 0.92 baseline.

## 19. `ArenaBuilder` is becoming a god-object

- Status: `Open` (2026-04-24)
- Severity: Medium
- Affected files:
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
- Problem:
  - `ArenaBuilder` now contains shell, verticality, cover, exits, start marker, architecture, floor patterns, decor, atmosphere, anchors, and legacy BSP build logic in one large class.
- Impact:
  - Harder review, harder future visual iteration, and a weaker architecture story for the diploma if asked why one builder owns so many unrelated responsibilities.
- Fix direction:
  - Keep `ArenaBuilder.BuildSingle` as a facade/orchestrator, but split implementation into focused builder classes such as shell, exits, architecture, floor patterns, decor, atmosphere, and verticality builders. Partial classes are acceptable as a temporary readability step, but separate builders are cleaner.

## 20. Decor yaw uses a magic deterministic constant instead of a named/randomized source

- Status: `Open` (2026-04-24)
- Severity: Low
- Affected files:
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
- Problem:
  - PR 2.E decor rotates props with `placed * 37f`. This is deterministic, but the number is undocumented and not tied to the seeded procedural sub-streams used elsewhere.
- Impact:
  - Minor maintainability issue and a small inconsistency in the determinism story: future agents may not know whether this was intentional visual distribution or an arbitrary placeholder.
- Fix direction:
  - Replace the literal with a named constant or move decor variation into a deterministic decor RNG/sub-stream, then document that visual-only variation remains seed-stable.

---

## How To Use This File

When adding a new issue, include:

- short title
- status
- severity
- affected files
- exact problem
- gameplay/technical impact
- intended fix direction if known

Update issue status when work begins or ends.

Remove issues only when they are truly obsolete or fully replaced by newer documented issues.

