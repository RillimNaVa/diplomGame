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
- **Phase 2 PR 2.C (async NavMesh + EncounterController + soft-lock barriers + GameManager encounter mode)** — code done 2026-04-22 ✅, **Editor verification pending**.
- Stable high-level knowledge base: [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- Roadmap: [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- TZ: [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) — APPROVED r4.

---

## Current Goal

**Phase 2 — Procedural Arena Generation r4.** 4-PR split: 2.A + 2.B verified, 2.C code merged (pending Editor verify), 2.D next after verify.

- PR 2.A — SingleArenaGenerator + shape / cover / exit planners + ArenaTypeProfile SO + S/M/L size presets + per-arena 10-25m ceiling — **DONE (verified 2026-04-21)**.
- PR 2.B — Run Graph (8 nodes, shared-subtree) + RunController state machine + ArenaFlowController fade/teleport + ExitDoorTrigger + DoorChoiceLabel + Victory/GameOver Canvas — **DONE (verified 2026-04-21)**.
- PR 2.C — Async `NavMeshSurface.UpdateNavMesh`, `GameManager.SetSpawnPoints/BeginEncounter/EndEncounter` API, `EncounterController` + `SoftLockBarrier` on exits, clear conditions (KillAll / ReachExit / None), `SimpleEnemyAI.isOnNavMesh` guard — **CODE MERGED 2026-04-22 (commit b1ae99f), Editor verify pending.**
- PR 2.D — **NEXT after 2.C verify.** Verticality (`ArenaVerticalityPlanner` platforms/ramps), biomes + 2 biome presets, Elite/Parkour/Shop/Rest profiles, difficulty scaling by `arenaIndex`, debug UI (seed/index/biome).

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

- **PR 2.C Editor verification** — см. checklist ниже.
- **PR 2.D** — verticality / biomes / elite+parkour+shop+rest profiles / difficulty scaling — not started.
- `test.unity` still not in Build Settings (long-standing issue #3).
- Enemy pooling (issue #6) and projectile pooling (issue #7) — deferred to a later performance pass.
- `SimpleEnemyAI` still primitive (issue #5) — acceptable for Phase 2 integration, refactor belongs to Phase 3.

---

## Recommended Next Task

**PR 2.C Editor verification** (before starting PR 2.D):

1. Open `Assets/test.unity`. На `GameManager` выставить `useEncounterMode = true`.
2. Убедиться, что на Player есть tag `Player`, на `Run` — `RunController` + ссылка на `DefaultRunConfig` + `ArenaHost`.
3. Play Mode. Пройти run через 5 арен. Проверить:
   - (a) Враги спавнятся на NavMesh и ходят (нет `SetDestination on inactive agent` в Console).
   - (b) `SoftLockBarrier` на выходных дверях закрыты до последнего kill, затем открываются.
   - (c) `ExitDoorTrigger` срабатывает только после того, как barrier открылся.
   - (d) `skipClearCondition=true` в `RunConfig` по-прежнему даёт walk-through debug-режим.
   - (e) Fade между аренами не ломает spawn-lifecycle врагов.
4. После successful verify — начать PR 2.D (verticality planner, biome SO, 4 новых type-profile, debug UI overlay).

---

## Files Most Relevant For The Next Task

- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) — single source of truth for PR 2.D spec
- [Assets/Scripts/ProceduralArena/Run/RunController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/RunController.cs)
- [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- [Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs)
- [Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Navigation/ArenaNavMeshController.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs) — `useEncounterMode` gating
- [Assets/ArenaProfiles/](C:/Users/assam/DiplomGame/Assets/ArenaProfiles) — existing preset assets (reference for PR 2.D Elite/Parkour/Shop/Rest)

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

**Before PR 2.C verify:**
- На `GameManager` в `test.unity` выставить `useEncounterMode = true`.
- Убедиться, что `Run` GameObject активен, а legacy `ArenaDebug` — выключен (иначе две арены наложатся).
- Для walk-through debug без убийств врагов — `skipClearCondition = true` в `DefaultRunConfig.asset`.

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.
