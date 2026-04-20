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

## Current Status (2026-04-20)

- **Phase 1 is COMPLETE.** Movement upgrades, Weapon System (PR A + PR B), and Kill-to-Survive (PR A + PR B) are all shipped and playtested without known blocking bugs.
- Stable high-level knowledge base: [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/PROJECT_KNOWLEDGE_BASE.md)
- Roadmap and change log: [PROGRESS.md](C:/Users/assam/DiplomGame/PROGRESS.md)
- Subsystem specs (both marked COMPLETED):
  - [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)
  - [KILL_TO_SURVIVE_TZ.md](C:/Users/assam/DiplomGame/KILL_TO_SURVIVE_TZ.md)

---

## Current Goal

**Begin Phase 2 — Procedural Arena Generation.**

This is the main diploma novelty contribution (procedural generation algorithms in an arcade survival roguelike). No TZ exists yet for this phase — the first step is to write one, then split implementation into PRs.

Expected scope for Phase 2 (from GDD v2.0):

- BSP-based room/corridor layout
- Room placement with constraints (spawn room, exits, size ranges)
- Runtime NavMesh baking (so `SimpleEnemyAI` still works inside generated arenas)
- Integration with existing wave spawner (spawn points become generated)
- Deterministic seed support (useful for debugging and thesis screenshots)

---

## What Is Already Done (Phase 1 recap)

- Movement: walk/jump/double-jump/dash/slide/air-control all tuned and playtested.
- Weapon system: `WeaponManager`, `WeaponBase`, `WeaponDefinition` (ScriptableObject), `[SerializeReference]` FireModeBase, 5 weapons, switching, ammo, reload. Code lives in `Assets/Scripts/Combat/Weapons/`.
- Kill-to-Survive: HP orbs, heal method, loot tables, enemy stagger, glory-kill detector (side-channel, no weapon modifications), kill-streak tracker with speed boost. Code lives in `Assets/Scripts/Combat/{Pickups,Enemies,Player}/`.
- Three extensibility seams in place for future upgrade system: `PlayerStats` (central stats provider), `EnemyLootTable` (drop config per-enemy), `IGloryKillPolicy` (pluggable glory rules). `AlwaysAllowPolicy` is the current default.
- `PlayerController` combat logic has been extracted — the class is now mostly movement + input forwarding.

---

## What Is Not Done Yet

- Phase 2 Procedural Arena Generation — not started.
- `test.unity` still not in Build Settings (long-standing issue #3).
- Enemy pooling (issue #6) and projectile pooling (issue #7) — deferred to a later performance pass.
- `SimpleEnemyAI` still primitive (issue #5) — acceptable for Phase 2 integration, refactor belongs to Phase 3.

---

## Recommended Next Task

1. Draft `ARENA_GENERATION_TZ.md` modeled on the existing TZ files (goal, architecture, classes, file structure, PR split, acceptance criteria).
2. Confirm approach with user before implementation.
3. Implement in small PRs (layout → geometry → NavMesh → integration).

---

## Files Most Relevant For The Next Task

- [Void_Survivor_GDD_v2.docx](C:/Users/assam/OneDrive/Desktop/For%20IITU/diploma/Void_Survivor_GDD_v2.docx) — design intent for arenas
- [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs) — existing prototype procedural work (reference only; arenas are a different approach)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity) — integration target
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs) — wave spawner that will consume generated spawn points

---

## Do Not Break

- Movement feel (walk/jump/double-jump/dash/slide/air-control)
- Weapon framework under `Assets/Scripts/Combat/Weapons/` — do not modify `WeaponBase` / `FireMode*` for arena work
- Kill-to-Survive seams (`PlayerStats`, `EnemyLootTable`, `IGloryKillPolicy`) — consumers rely on these
- `Health` event contracts (`onHealthChanged`, `onDeath`, `onTakeDamage`)
- `GameManager.OnEnemyKilled` event — `KillStreakTracker` listens to it

---

## Important Current Project Facts

- `Assets/test.unity` is still the only real gameplay scene.
- Combat code lives under `Assets/Scripts/Combat/**`, not under `Assets/test/`.
- `Assets/test/` now contains: `Health`, `PlayerController` (slimmed), `GameManager`, `SimpleEnemyAI`, `Projectile`, `UIManager`, `EnemyHealthBarView`, input assets.
- Scene wiring for new Kill-to-Survive components auto-resolves via `GetComponent` in `Awake` — no drag-and-drop needed when prefab/scene is regenerated.

---

## Immediate Manual Setup Reminder

None right now. Phase 2 will introduce new manual steps (likely a generator prefab + a "generate on start" toggle), to be documented when that TZ is drafted.

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.
