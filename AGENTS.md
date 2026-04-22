# Agent Context — Void Survivor (DiplomGame)

Entry point for any AI agent (Codex, Claude Code, etc.) working in this repo.
Read the files in the order below **before** touching any code.

## Project one-liner

Unity 6 URP diploma project: fast FPS roguelike (DOOM Eternal / Ultrakill style) with procedural arena generation. Deadline ~June 2026.

## Reading order

1. [docs/ai/user_profile.md](docs/ai/user_profile.md) — who the user is, how to collaborate
2. [docs/ai/workflow_rules.md](docs/ai/workflow_rules.md) — what tracking files to update after each task
3. [docs/AI_HANDOFF.md](docs/AI_HANDOFF.md) — current active task, do-not-break list, manual setup reminders
4. [docs/PROJECT_KNOWLEDGE_BASE.md](docs/PROJECT_KNOWLEDGE_BASE.md) — stable architecture overview
5. [docs/PROGRESS.md](docs/PROGRESS.md) — full roadmap + Change Log
6. [docs/ARENA_GENERATION_TZ.md](docs/ARENA_GENERATION_TZ.md) — active Phase 2 spec (r4 APPROVED)

Historical specs (read only if directly relevant):
- [docs/WEAPON_SYSTEM_TZ.md](docs/WEAPON_SYSTEM_TZ.md) — COMPLETED
- [docs/KILL_TO_SURVIVE_TZ.md](docs/KILL_TO_SURVIVE_TZ.md) — COMPLETED

Index of all docs: [docs/PROJECT_DOCUMENTS_GUIDE.md](docs/PROJECT_DOCUMENTS_GUIDE.md).

## Current phase

**Phase 2 — Procedural Arena Generation r4.**
PR 2.A + 2.B verified. PR 2.C code merged 2026-04-22 (Editor verify pending). PR 2.D next.

## Hard rules

- Zero `UnityEngine.Random` inside `Assets/Scripts/ProceduralArena/**` — always use `System.Random` sub-streams (determinism for thesis figures).
- BSP r1-r3 code marked `[DEPRECATED]` — DO NOT delete, kept for diploma reference.
- Do-not-break list lives in [docs/AI_HANDOFF.md](docs/AI_HANDOFF.md).
- After finishing a task, update the tracking files per [docs/ai/workflow_rules.md](docs/ai/workflow_rules.md).

## Repo map (what lives where)

- `Assets/test.unity` — the only real gameplay scene.
- `Assets/Scripts/Combat/**` — Phase 1 (weapon framework, kill-to-survive).
- `Assets/Scripts/ProceduralArena/**` — Phase 2 code (r4 active + legacy BSP marked `[DEPRECATED]`).
  - `Core/` — `ArenaRunConfig`, `ArenaRuntimeContext`, `ArenaRoomData`
  - `Layout/` — [DEPRECATED] BSP layout
  - `Build/` — `ArenaBuilder` (with `BuildSingle` r4 entry), blockout builders, materials
  - `Arena/` — r4 `SingleArenaGenerator` + shape/cover/exit planners + `ArenaTypeProfile` SO
  - `Run/` — r4 run-graph: `RunGraph`, `RunController`, `ArenaFlowController`, `ExitDoorTrigger`
  - `Navigation/` — `ArenaNavMeshController` (async NavMesh bake)
  - `Encounter/` — `EncounterController`, `SoftLockBarrier`, `EncounterTrigger`
  - `Debug/` — `ArenaDebugGizmos`, `ArenaGenerationLog`
- `Assets/test/` — legacy Phase 0 code: `Health`, `PlayerController` (slimmed), `GameManager` (with encounter API), `SimpleEnemyAI`, `Projectile`, `UIManager`, input assets.
- `Assets/ArenaProfiles/` — 3 hand-authored arena preset assets.
- `docs/` — all project documentation (tracking files + TZ specs + AI context).

## Engine / packages notes

- Unity 6 (6000.2.5f1), URP.
- `com.unity.ai.navigation` 2.0.9 already in `Packages/manifest.json` — runtime NavMesh bake used by PR 2.C.
- Input System (new) via `PlayerInputActions.inputactions`.
