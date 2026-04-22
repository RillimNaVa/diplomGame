# Workflow Rules

> Shared rules for any AI agent working on this project. Keeps progress tracked across sessions so nothing gets lost when context resets or we switch between tools.

## Update-tracking-files rule

**After finishing any meaningful task** (feature, PR, bugfix, milestone), unless the user objects, update the relevant tracking files so progress survives context compaction and new chat sessions:

1. **`docs/PROGRESS.md`** — tick completed checklist items; add a dated Change Log entry.
2. **`docs/PROJECT_KNOWLEDGE_BASE.md`** — if the task changed architecture, scripts, folders, or scene wiring in a lasting way.
3. **`docs/AI_HANDOFF.md`** — rewrite the "Current Status" / "Current Goal" / "What Is Not Done Yet" / "Recommended Next Task" / "Manual Setup Reminder" sections so the next session immediately knows where to resume.
4. **`docs/KNOWN_ISSUES.md`** — close/adjust any issues the task resolved; add new ones it surfaced.
5. **Relevant TZ file** in `docs/` — if the task completes a subsystem spec, mark it `Status: COMPLETED (YYYY-MM-DD)` at the top instead of deleting the file. Historical specs help later sessions understand intent.
6. **`AGENTS.md`** (repo root) — update only if global agent rules / reading order / hard rules changed. Otherwise this file is stable.

## Why

- Conversations hit context limits and get summarized; tracking files are the durable memory.
- Future AI sessions read these files on wake-up and would otherwise waste tokens reconstructing state or, worse, silently work against stale assumptions.
- The user is working toward a diploma deadline (June 2026) — losing track of what was done is expensive.
- Multiple agents (Codex, Claude) share the same tracking files — divergence between them causes rework.

## How to apply

- Ask the user if something is actually done (playtested vs just code-complete) before ticking a checklist box. Code-complete → note "awaiting playtest" or "awaiting Editor verify".
- Keep Change Log entries short: date + what + why. Implementation details live in `PROJECT_KNOWLEDGE_BASE.md`.
- Don't duplicate full architecture explanations across files — each file has a role (see `docs/PROJECT_DOCUMENTS_GUIDE.md`).
- If unsure whether to update a file, ask the user once rather than silently skipping it.

## Hard project rules (don't violate these)

- Zero `UnityEngine.Random` inside `Assets/Scripts/ProceduralArena/**` — always use `System.Random` sub-streams from `ArenaRuntimeContext` / `RunGraphGenerator` (determinism for thesis).
- BSP r1-r3 code marked `[DEPRECATED]` in `ProceduralArena/{Layout,Core,Build}` must NOT be deleted — kept for diploma reference (demonstrates algorithmic exploration).
- `Assets/test.unity` is the only real gameplay scene. `SampleScene.unity` is a legacy prototype.
- Full do-not-break list lives in `docs/AI_HANDOFF.md`.
