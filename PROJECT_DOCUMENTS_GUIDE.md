# Void Survivor - Project Documents Guide

## Purpose

This file explains the role of the main project-level documentation files in the repository.

It answers:

- what each file is for
- what should be written there
- when it should be updated
- why that file is useful

The goal is to keep project context stable across multiple AI chats and reduce token waste from repeated explanations.

---

## Why These Files Exist

A Unity project contains two different kinds of knowledge:

1. Stable knowledge
   - project structure
   - architecture
   - current systems
   - scene roles

2. Changing knowledge
   - what is being worked on right now
   - what is blocked
   - known bugs
   - feature specifications

If everything is stored in one giant file, that file becomes bloated and outdated.

So the documentation is split by purpose.

---

## Main Documentation Files

## [PROGRESS.md](C:/Users/assam/DiplomGame/PROGRESS.md)

### What It Is

The high-level development roadmap of the game.

### What It Should Contain

- development phases
- milestone checklist items
- completed features at phase level
- change log of meaningful progress
- short "what is next" direction
- manual Unity setup reminders that affect roadmap progress

### What It Should Not Contain

- full architectural descriptions
- deep file-by-file explanations
- low-level bug tracking
- large temporary working notes

### When To Update It

Update `PROGRESS.md` when:

- a planned step is completed
- a phase changes
- roadmap priorities change
- a significant milestone is achieved

### Why It Is Useful

- shows where the project is headed
- helps align implementation with the diploma plan
- keeps phase-based progress visible

Short summary:

- `PROGRESS.md` = development plan by phases and milestones

---

## [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/PROJECT_KNOWLEDGE_BASE.md)

### What It Is

The main technical overview of the project.

### What It Should Contain

- what the project is
- which scenes are important
- which folders matter
- explanation of current major gameplay scripts
- explanation of implemented systems
- current technical state of the project
- known architectural context
- what future AI agents should read first

### What It Should Not Contain

- full feature specs for new systems
- temporary "current task" notes
- every bug in detail
- every tiny code change

### When To Update It

Update `PROJECT_KNOWLEDGE_BASE.md` when:

- a major subsystem changes
- new core scripts become important
- scene roles change
- architecture changes in a lasting way
- a future AI would otherwise misunderstand the project if reading the old version

### Why It Is Useful

- gives new AI agents a fast mental model of the project
- prevents repeated full rescans
- reduces token usage in fresh chats

Short summary:

- `PROJECT_KNOWLEDGE_BASE.md` = stable technical map of the project

---

## [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

### What It Is

A technical specification for one major planned subsystem: the modular weapon system.

### What It Should Contain

- goal of the subsystem
- architecture choice
- responsibilities of classes
- file structure expectations
- implementation constraints
- migration plan
- acceptance criteria

### What It Should Not Contain

- unrelated project-wide knowledge
- bug backlog for other systems
- vague brainstorming without implementation direction

### When To Update It

Update this file when:

- the weapon-system plan changes
- implementation intentionally deviates from the current spec
- the spec needs clarification before handing off to another AI

### Why It Is Useful

- prevents re-explaining the full weapon-system design in every new chat
- gives another AI a direct implementation target

Short summary:

- `WEAPON_SYSTEM_TZ.md` = detailed implementation spec for the weapon system

---

## [AI_HANDOFF.md](C:/Users/assam/DiplomGame/AI_HANDOFF.md)

### What It Is

A short operational handoff for the next AI or next chat.

### What It Should Contain

- current active goal
- what was already prepared
- what is not done yet
- what should be done next
- which files matter for the current task
- what must not be broken
- short current warnings or temporary constraints

### What It Should Not Contain

- complete architecture explanations
- long-term roadmap
- giant bug list
- every historical change ever made

### When To Update It

Update `AI_HANDOFF.md` when:

- the active task changes
- work pauses in the middle of a task
- a future AI/chat must continue from the exact current state
- temporary constraints or warnings appear

### Why It Is Useful

- gives immediate short-term context
- prevents a new AI from wasting tokens reconstructing "what was happening right now"

Short summary:

- `AI_HANDOFF.md` = current working context for the next AI/chat

---

## [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/KNOWN_ISSUES.md)

### What It Is

A structured list of known bugs, tech debt, performance risks, and unresolved engineering problems.

### What It Should Contain

- concrete issue title
- current status
- severity
- affected files
- actual problem
- why it matters
- suggested fix direction if known

### What It Should Not Contain

- roadmap tasks that are not actually issues
- vague complaints without impact
- stable architecture explanations
- active step-by-step work notes

### When To Update It

Update `KNOWN_ISSUES.md` when:

- a bug is confirmed
- a technical debt item is identified
- a performance risk is discovered
- a temporary workaround is known but the root issue is still unresolved
- an issue is partially fixed or closed

### Why It Is Useful

- keeps bugs and tech debt out of the main knowledge base
- lets future AI agents quickly see what is already known
- avoids rediscovering the same problems repeatedly

Short summary:

- `KNOWN_ISSUES.md` = bugs, risks, and technical debt backlog

---

## How These Files Work Together

Think of them like layers:

- `PROGRESS.md`
  - where the game is going
- `PROJECT_KNOWLEDGE_BASE.md`
  - what the project currently is
- subsystem TZ files like `WEAPON_SYSTEM_TZ.md`
  - how one major thing should be built
- `AI_HANDOFF.md`
  - what we are doing right now
- `KNOWN_ISSUES.md`
  - what is broken, risky, or weak

This separation is useful because each file stays focused and shorter.

---

## What To Record In Which File

### Example 1

Information:

- "Phase 1 movement upgrades are done"

Where it belongs:

- `PROGRESS.md`

Why:

- it is roadmap progress

### Example 2

Information:

- "`PlayerController` is overloaded and currently contains combat logic"

Where it belongs:

- `PROJECT_KNOWLEDGE_BASE.md`

Why:

- it is a stable fact about current architecture

### Example 3

Information:

- "The next AI should implement the weapon system and must not break movement"

Where it belongs:

- `AI_HANDOFF.md`

Why:

- it is immediate task context

### Example 4

Information:

- "`SimpleEnemyAI` calls `SetDestination()` every frame and scales badly"

Where it belongs:

- `KNOWN_ISSUES.md`

Why:

- it is a confirmed technical issue

### Example 5

Information:

- "Weapon system should use `WeaponManager + WeaponBase + WeaponDefinition + FireMode`"

Where it belongs:

- `WEAPON_SYSTEM_TZ.md`

Why:

- it is implementation specification for a major subsystem

---

## Recommended Update Rules

To keep documentation useful:

- update `PROGRESS.md` after milestones
- update `PROJECT_KNOWLEDGE_BASE.md` after major lasting architecture changes
- update `AI_HANDOFF.md` when current work focus changes
- update `KNOWN_ISSUES.md` when problems are discovered or resolved
- update subsystem TZ files when planned implementation direction changes

---

## Why This Reduces Token Usage For AI

Without these files, every new AI chat must spend tokens on:

- rescanning folders
- guessing which scene is important
- rediscovering architecture
- rediscovering bugs
- reconstructing current task context

With these files, a new AI can read:

1. `PROJECT_KNOWLEDGE_BASE.md`
2. `AI_HANDOFF.md`
3. the relevant TZ or issue file

That usually gives enough context to start useful work with much less repeated token spend.

---

## Practical Guidance For Future Expansion

If the project grows, other useful document files may be added later, for example:

- `UNITY_SETUP.md`
- `ARCHITECTURE_DECISIONS.md`
- `TESTING_CHECKLIST.md`
- subsystem-specific technical specs

Do not create too many files unless they solve a real recurring context problem.

The current set is enough for now.

---

## Final Summary

Use the files like this:

- `PROGRESS.md` = roadmap and phase progress
- `PROJECT_KNOWLEDGE_BASE.md` = stable technical understanding of the project
- `WEAPON_SYSTEM_TZ.md` = detailed spec for the weapon system
- `AI_HANDOFF.md` = current working handoff for the next AI/chat
- `KNOWN_ISSUES.md` = bugs and technical debt

If these files are kept updated, new AI chats will need much less rediscovery work and will spend more tokens on actual implementation instead of reconstruction.

