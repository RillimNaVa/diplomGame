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

## Current Status

- The project already has a stable high-level knowledge base in [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/PROJECT_KNOWLEDGE_BASE.md)
- The project roadmap is tracked in [PROGRESS.md](C:/Users/assam/DiplomGame/PROGRESS.md)
- The next major architectural task is the weapon-system refactor
- The detailed technical specification for that refactor already exists in [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

---

## Current Goal

Prepare the project for the modular weapon-system implementation described in `WEAPON_SYSTEM_TZ.md`.

The intended architecture is:

- `WeaponManager`
- `WeaponBase`
- `WeaponDefinition`
- reusable `FireMode` classes

---

## What Is Already Done

- Project structure and current codebase were reviewed
- Performance audit was done at a static code/settings level
- Main gameplay files were identified and documented
- A detailed weapon-system technical specification was written
- A project knowledge-base file was created to help future AI agents enter the project faster

---

## What Is Not Done Yet

- Weapon system is not implemented yet
- `PlayerController` still contains combat logic
- `test.unity` is still not in Build Settings
- Current melee is still temporary and not yet migrated into a proper weapon

---

## Recommended Next Task

Implement the weapon system exactly according to [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md).

Recommended order:

1. Create weapon core classes
2. Create fire-mode classes
3. Migrate combat out of `PlayerController`
4. Add 5 planned weapons
5. Add switching and ammo
6. Wire the system into `Assets/test.unity`

---

## Files Most Relevant For The Next Task

- [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)
- [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/PROJECT_KNOWLEDGE_BASE.md)
- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)
- [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

---

## Do Not Break

- Player movement, jump, dash, slide, and air control
- Existing `Health`-based damage flow
- Existing projectile prefab workflow unless intentionally replaced in a compatible way
- The playable state of `Assets/test.unity`

---

## Important Current Project Facts

- `Assets/test.unity` is the actual gameplay prototype scene
- `Assets/Scenes/SampleScene.unity` is mostly the terrain/prototype scene
- Only `SampleScene` is currently in Build Settings
- `PlayerController` is the main overloaded class and the main refactor target
- The movement system is already relatively mature
- The combat system works, but its architecture is still temporary

---

## Immediate Manual Setup Reminder

After any weapon-system integration work, verify:

- `Assets/test.unity` player references are still assigned correctly
- weapon holder / camera / muzzle references are valid
- the scene still plays without missing reference errors

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.

