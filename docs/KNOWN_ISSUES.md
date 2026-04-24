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

## Recent Closures (2026-04-17 → 2026-04-19)

- Issue #1 (`PlayerController` overloaded) — **Closed**, combat extracted during Weapon System refactor.
- Issue #2 (No real weapon system) — **Closed**, `WEAPON_SYSTEM_TZ.md` implementation shipped.
- Issue #8 (Temporary melee) — **Closed**, replaced by `Void Blade` weapon.

Kept open for Phase 2+: #3, #4, #5, #6, #7, #9, #10, #11.

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

- Status: `Open`
- Severity: Medium
- Affected files:
  - [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- Problem:
  - Calls `SetDestination()` every frame
  - Computes distance every frame
  - Logs every attack
  - Fetches `Health` during attack path instead of caching it
- Impact:
  - CPU waste
  - Worse scaling with more enemies
  - Editor slowdown due to logging
- Fix direction:
  - Future AI refactor with state machine, target caching, and reduced path updates

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

## 13. PR 2.E visual pass still needs first Unity import and readability tuning

- Status: `Open` (2026-04-24)
- Severity: Medium
- Affected files:
  - [Assets/Resources/ProceduralArena/Biomes/](C:/Users/assam/DiplomGame/Assets/Resources/ProceduralArena/Biomes)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
  - [Assets/Scripts/ProceduralArena/Build/ArenaBuildMaterials.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuildMaterials.cs)
  - [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
  - [Assets/ArenaProfiles/Biome_VoidStation.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_VoidStation.asset)
  - [Assets/ArenaProfiles/Biome_AlienNexus.asset](C:/Users/assam/DiplomGame/Assets/ArenaProfiles/Biome_AlienNexus.asset)
- Problem:
  - PR 2.E now includes a prototype runtime PBR pipeline and texture import helper, and follow-up tuning on 2026-04-24 already neutralized `VoidStation` trim/prop slots plus added flat-arena safeguards for `Start` / `Shop` / `Rest`, but the copied biome textures still need their first Unity reimport under the new rules and the scene still has not gone through a full Play Mode readability/collision pass.
- Impact:
  - Terminal-side `dotnet build` is green, but the real gameplay scene can still need import cleanup or balancing for PBR map correctness, fog density, contamination coverage, emissive accents, remaining `VoidStation` readability, prop density, floor pattern readability, and the feel of newly solid architectural pieces.
- Fix direction:
  - Open `Assets/test.unity`, let Unity import the copied textures, start a fresh run, then verify first that `Start` / `Shop` / `Rest` stay flat and that large cyan `VoidStation` trim blocks are gone before doing the broader 5-arena visual tuning pass.

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

