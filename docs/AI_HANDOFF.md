# Void Survivor - AI Handoff

## Purpose

This file is a short operational handoff for the next AI agent or next chat.

Unlike the main knowledge base, this file should contain only the current working context:

- what is being worked on right now
- what has already been prepared
- what should be done next
- what should not be changed accidentally

This file is meant to stay short and current.

---

## Current Status (2026-04-22)

- **Phase 1 is COMPLETE.** Movement upgrades, Weapon System (PR A + PR B), Kill-to-Survive (PR A + PR B) shipped and playtested.
- **Phase 2 PIVOT'нут на TZ r4** (2026-04-20) — single big procedural arena per encounter + procedural run graph c door-choice (Hades/Roboquest-style), вместо multi-room BSP. BSP-код r1-r3 помечен `[DEPRECATED]`, оставлен для diploma reference.
- **Phase 2 PR 1 (Layout + Seed, BSP)** — done + editor-verified 2026-04-20 (теперь legacy).
- **Phase 2 PR 2 (BSP blockout)** — done 2026-04-20 (теперь legacy).
- **Phase 2 PR 2.A (r4 SingleArenaGenerator + shape/cover/exit planners + size presets + per-arena ceiling)** — verified 2026-04-21 ✅
- **Phase 2 PR 2.B (Run Graph + Transitions + fade + door-choice + Victory/GameOver)** — verified 2026-04-21 ✅. Post-verify фиксы: Billboard label flip, door-opening cut в shell wall, lintel над дверью, solid `ExitBarrier_i` barrier.
- **Phase 2 PR 2.C (async NavMesh + EncounterController + soft-lock barriers + GameManager encounter mode)** — verified 2026-04-22 ✅
- Stable high-level knowledge base: [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- Roadmap: [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- TZ: [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) — APPROVED r4.

---

## Current Goal

**Phase 2 — Procedural Arena Generation r4.** 4-PR split complete: 2.A + 2.B + 2.C + 2.D verified.

- PR 2.A — SingleArenaGenerator + shape / cover / exit planners + ArenaTypeProfile SO + S/M/L size presets + per-arena 10-25m ceiling — **DONE (verified 2026-04-21)**.
- PR 2.B — Run Graph (8 nodes, shared-subtree) + RunController state machine + ArenaFlowController fade/teleport + ExitDoorTrigger + DoorChoiceLabel + Victory/GameOver Canvas — **DONE (verified 2026-04-21)**.
- PR 2.C — Async `NavMeshSurface.UpdateNavMesh`, `GameManager.SetSpawnPoints/BeginEncounter/EndEncounter` API, `EncounterController` + `SoftLockBarrier` on exits, clear conditions (KillAll / ReachExit / None), `SimpleEnemyAI.isOnNavMesh` guard — **VERIFIED 2026-04-22**.
- PR 2.D — `ArenaVerticalityPlanner` (platforms/ramps), `BiomeDefinition` + 2 biome presets, Elite/Parkour/Shop/Rest profiles, difficulty scaling by `arenaIndex`, runtime debug UI (seed/index/biome), biome-driven materials — **VERIFIED 2026-04-22**.

Scene wiring reference (`test.unity`):
- `Run` GameObject with `RunController` (config → `DefaultRunConfig`, flow → `ArenaHost`).
- `Run/ArenaHost` GameObject with `ArenaFlowController` (buildConfig → `TestArenaConfig`).
- `GameManager` — set `useEncounterMode = true` для PR 2.C flow (legacy wave loop остаётся при `false`).
- Legacy `ArenaDebug` GameObject — оставлен выключённым (BSP debug); не включать одновременно с `Run` иначе две арены наложатся.
- Player must have tag `Player`. CharacterController teleports via `cc.enabled=false/true`.

---

## What Is Already Done (Phase 1 + Phase 2 recap)

**Phase 1:**
- Movement: walk/jump/double-jump/dash/slide/air-control all tuned and playtested.
- Weapon system: `WeaponManager`, `WeaponBase`, `WeaponDefinition` (ScriptableObject), `[SerializeReference]` FireModeBase, 5 weapons, switching, ammo, reload. Code lives in `Assets/Scripts/Combat/Weapons/`.
- Kill-to-Survive: HP orbs, heal method, loot tables, enemy stagger, glory-kill detector, kill-streak tracker. Code lives in `Assets/Scripts/Combat/{Pickups,Enemies,Player}/`.
- Three extensibility seams for future upgrades: `PlayerStats`, `EnemyLootTable`, `IGloryKillPolicy`.
- `PlayerController` slimmed to movement + input forwarding.

**Phase 2 (r4):**
- `Assets/Scripts/ProceduralArena/Arena/` — `SingleArenaGenerator` + shape/cover/exit planners + `ArenaTypeProfile` SO + size presets (S/M/L → 40/60/80м) + per-arena ceiling 10-25м. Zero `UnityEngine.Random`.
- `Assets/Scripts/ProceduralArena/Run/` — `RunGraph`, `RunGraphGenerator` (8 nodes: 1+2+2+2+1, shared subtree), `RunController` state machine, `ArenaFlowController` fade+teleport, `ExitDoorTrigger`, `DoorChoiceLabel`, `DefaultRunConfig.asset`.
- `Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs` — async `UpdateNavMesh` on ArenaRoot (com.unity.ai.navigation 2.0.9).
- `Assets/Scripts/ProceduralArena/Encounter/` — `EncounterController`, `SoftLockBarrier`, `EncounterTrigger` (scaffolded, bypassed by teleport flow).
- `GameManager` — `useEncounterMode` flag, `SetSpawnPoints` / `BeginEncounter(count, spawns, onKilled)` / `EndEncounter()` API.
- `SimpleEnemyAI.Update` — `agent.isOnNavMesh` guard (prevents exceptions during runtime bake).
- 3 arena preset assets: `Assets/ArenaProfiles/{Arena_Start_S, Arena_Combat_M, Arena_Boss_L}.asset`.
- BSP code r1-r3 помечен `[DEPRECATED]` в `Layout/`, `Core/ArenaGenerator.cs`, `Build/CorridorBlockoutBuilder.cs` — оставлен для diploma reference.

---

## What Is Not Done Yet

- Phase 2 r4 PR chain is complete.
- No known unfinished tasks inside PR 2.C / PR 2.D after successful playtest.
- `test.unity` still not in Build Settings (long-standing issue #3).
- Enemy pooling (issue #6) and projectile pooling (issue #7) — deferred to a later performance pass.
- `SimpleEnemyAI` still primitive (issue #5) — acceptable for Phase 2 integration, refactor belongs to Phase 3.

---

## Recommended Next Task

Start planning the next milestone above the completed Phase 2 pipeline:

1. **PR 2.E — Visual Style Pass For Pre-Defense** per [VISUAL_STYLE_PASS_TZ.md](C:/Users/assam/DiplomGame/docs/VISUAL_STYLE_PASS_TZ.md): strengthen biome materials, builder architecture details, floor patterns, rule-based decor, and atmosphere.
2. After PR 2.E, continue to Phase 3 enemy AI refactor (`SimpleEnemyAI` → state-machine base + enemy archetypes).
3. Then Phase 4 roguelike progression (upgrades / shop logic) using the stable run graph.

---

## Files Most Relevant For The Next Task

- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) — single source of truth for PR 2.D spec
- [Assets/Scripts/ProceduralArena/Run/RunController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/RunController.cs)
- [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- [Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs)
- [Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs) — `useEncounterMode` gating
- [Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/ArenaVerticalityPlanner.cs)
- [Assets/Scripts/ProceduralArena/Arena/BiomeDefinition.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Arena/BiomeDefinition.cs)
- [Assets/Scripts/ProceduralArena/Run/ArenaRuntimeDebugOverlay.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaRuntimeDebugOverlay.cs)
- [Assets/ArenaProfiles/](C:/Users/assam/DiplomGame/Assets/ArenaProfiles) — Start/Combat/Boss updated, plus Elite/Parkour/Shop/Rest + 2 biome assets

---

## Do Not Break

- Movement feel (walk/jump/double-jump/dash/slide/air-control)
- Weapon framework under `Assets/Scripts/Combat/Weapons/` — do not modify `WeaponBase` / `FireMode*` for arena work
- Kill-to-Survive seams (`PlayerStats`, `EnemyLootTable`, `IGloryKillPolicy`) — consumers rely on these
- `Health` event contracts (`onHealthChanged`, `onDeath`, `onTakeDamage`)
- `GameManager.OnEnemyKilled` event — `KillStreakTracker` listens to it
- `GameManager` legacy wave loop (when `useEncounterMode = false`) — fallback для ручного тестирования без Run graph
- Zero `UnityEngine.Random` rule внутри `Assets/Scripts/ProceduralArena/**` — всё через `System.Random` sub-streams из `ArenaRuntimeContext` / `RunGraphGenerator`
- `[DEPRECATED]` headers в BSP-коде — не удалять, diploma-reference ценность

---

## Important Current Project Facts

- `Assets/test.unity` is still the only real gameplay scene.
- Combat code lives under `Assets/Scripts/Combat/**`, not under `Assets/test/`.
- Procedural arena code lives under `Assets/Scripts/ProceduralArena/{Core,Layout,Build,Arena,Run,Navigation,Encounter,Debug}/`.
- `Assets/test/` now contains: `Health`, `PlayerController` (slimmed), `GameManager` (с encounter API), `SimpleEnemyAI` (с isOnNavMesh guard), `Projectile`, `UIManager`, `EnemyHealthBarView`, input assets.
- Scene wiring for new Kill-to-Survive components auto-resolves via `GetComponent` in `Awake` — no drag-and-drop needed when prefab/scene is regenerated.
- `com.unity.ai.navigation` 2.0.9 already в `Packages/manifest.json` — runtime bake через `NavMeshSurface.UpdateNavMesh`.

---

## Immediate Manual Setup Reminder

**Current manual reminders:**
- На `GameManager` в `test.unity` выставить `useEncounterMode = true`.
- Убедиться, что `Run` GameObject активен, а legacy `ArenaDebug` — выключен (иначе две арены наложатся).
- Для walk-through debug без убийств врагов — `skipClearCondition = true` в `DefaultRunConfig.asset`.
- `test.unity` still should be added to Build Settings later (issue #3), but this is outside PR 2.C/2.D completion.

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.
