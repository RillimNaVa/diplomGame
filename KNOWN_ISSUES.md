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

## Issues

## 1. `PlayerController` has too many responsibilities

- Status: `Planned`
- Severity: High
- Affected files:
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- Problem:
  - The script currently handles movement, look, shooting, projectile spawning, melee, recoil triggering, and input callbacks
- Impact:
  - Hard to maintain
  - Hard to extend
  - High risk of regressions during combat changes
- Planned direction:
  - Move combat into the modular weapon system defined in [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

## 2. Real weapon system does not exist yet

- Status: `Planned`
- Severity: High
- Affected files:
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
  - [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)
- Problem:
  - Current combat is hardcoded into player logic
  - No proper switching, weapon runtime state, or reusable fire-mode abstraction exists
- Impact:
  - Blocks clean implementation of Phase 1 weapon system
  - Makes future upgrades and ammo systems harder
- Planned direction:
  - Implement the hybrid weapon architecture already documented in `WEAPON_SYSTEM_TZ.md`

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

- Status: `Planned`
- Severity: Medium
- Affected files:
  - [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- Problem:
  - Melee exists as an embedded temporary combat action, not a proper weapon implementation
- Impact:
  - Does not fit planned weapon architecture
  - Makes future glory-kill/stagger systems awkward
- Planned direction:
  - Replace with `Void Blade` via weapon system

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

