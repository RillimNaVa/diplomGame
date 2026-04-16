# Void Survivor - Project Knowledge Base

## Purpose

This file is a persistent knowledge base for the `Void Survivor` Unity project.

It is intended for:

- future AI agents starting in a fresh chat with no prior context
- the project owner as a fast technical overview
- reducing repeated full-project rescans when context windows reset

This document describes the current project structure, important files, implemented systems, scene setup, known issues, and current development direction.

---

## How To Use This File

If you are a new AI agent entering the project, use this order:

1. Read this file first
2. Read [PROGRESS.md](C:/Users/assam/DiplomGame/PROGRESS.md)
3. Read [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md) if working on combat/weapon refactor
4. Only then inspect the exact files related to the requested task

Do not start with a blind full scan of `Library`, `obj`, or package cache.

---

## Project Identity

- Project name: `DiplomGame`
- Game concept: fast first-person arcade survival / roguelike
- Inspiration: `DOOM Eternal` + `Ultrakill`
- Engine: Unity 6 with URP
- Main short-term target: finish core weapon system and continue Phase 1
- Main long-term target: diploma-ready playable prototype by June 2026

---

## Current Project Status Summary

As of the current state of the repository:

- basic first-person movement exists
- jump, double jump, dash, slide, and air control exist
- basic shooting exists
- projectile-based shooting exists
- temporary melee exists
- health system exists
- simple enemy AI exists
- basic wave spawner exists
- minimal HUD exists
- a prototype procedural terrain scene exists
- a playable combat prototype scene exists
- weapon system architecture has been specified, but not yet implemented

What is not yet properly built:

- real modular weapon system
- proper weapon switching
- ammo pickups
- kill-to-survive mechanics
- advanced enemy types
- proper roguelike progression systems

---

## Important Root Files

### [PROGRESS.md](C:/Users/assam/DiplomGame/PROGRESS.md)

Main project roadmap and progress tracker.

Contains:

- phase breakdown
- completed and planned steps
- manual Unity setup reminders
- latest development notes

Important note:

- some checklist items in `PROGRESS.md` may be marked done while still being only partially reflected in serialized scene data
- example: movement speed default in code is `10`, but the active scene still has a serialized value of `6`

### [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

Detailed technical specification for the planned hybrid weapon system architecture.

Use this when implementing:

- `WeaponManager`
- `WeaponBase`
- `WeaponDefinition`
- fire modes
- weapon switching
- ammo
- melee migration into weapon system

### [Void_Survivor_GDD_v2.docx](C:/Users/assam/DiplomGame/Void_Survivor_GDD_v2.docx)

The game design document.

Use this for:

- gameplay intent
- design-aligned implementation
- feature validation against diploma goals

---

## Folders That Matter

### `Assets/test`

This is currently the most important gameplay code folder.

It contains:

- player controller
- enemy AI
- health
- projectile
- UI manager
- game manager
- input assets
- animation controller used by recoil

This is where most current gameplay logic lives.

### `Assets/Scripts`

Currently contains:

- [TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)

This script is for the prototype terrain scene and not for the long-term arena-based game architecture.

### `Assets/Prefabs`

Contains gameplay prefabs:

- enemy prefab
- projectile prefab
- enemy health bar prefab

### `Assets/Scenes`

Contains:

- `SampleScene.unity`

This is the terrain/prototype showcase scene, not the main combat prototype scene.

### `Assets`

Also contains:

- [test.unity](C:/Users/assam/DiplomGame/Assets/test.unity), which is the actual current gameplay scene
- [Recoil.anim](C:/Users/assam/DiplomGame/Assets/Recoil.anim)
- [untitledWithAnimation1.fbx](C:/Users/assam/DiplomGame/Assets/untitledWithAnimation1.fbx), a model asset currently connected to the weapon/animation setup

---

## Folders That Usually Do Not Matter For Gameplay Tasks

These folders should normally be ignored by future AI agents unless the task explicitly requires them:

- `Library`
- `Logs`
- `obj`
- `.vs`
- package cache content inside `Library/PackageCache`

Also usually low priority unless specifically asked:

- `Assets/TutorialInfo`
- `Assets/TextMesh Pro`
- `Assets/_Recovery`

These are mostly template/demo/support assets and not core gameplay logic.

---

## Main Scenes

## 1. [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

This is the current playable gameplay prototype scene.

Purpose:

- active combat sandbox
- player movement and shooting testbed
- enemy spawning and basic wave loop
- HUD validation

Key contents:

- player object
- player camera
- `PlayerInput`
- `PlayerController`
- `Health`
- `GameManager`
- spawn points
- HUD canvas
- basic geometry and materials
- weapon holder / viewmodel-ish setup
- recoil animator setup

Important note:

- this scene is currently not added to Build Settings
- only `Assets/Scenes/SampleScene.unity` is in Build Settings
- because of this, active-scene reload logic may be unreliable if playing `test.unity`

## 2. [Assets/Scenes/SampleScene.unity](C:/Users/assam/DiplomGame/Assets/Scenes/SampleScene.unity)

This is the terrain/procedural world prototype scene.

Purpose:

- test procedural terrain generation
- test URP scene lighting and environment

Key contents:

- `Terrain`
- `TerrainGenerator`
- directional light
- global volume
- scene camera

Important note:

- this scene is currently the only one in Build Settings
- it has much heavier rendering/settings cost than the combat scene
- terrain auto-generation is currently enabled here

---

## Core Gameplay Scripts

## [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)

### Role

This is currently the central gameplay script for the player.

### Current Responsibilities

- character movement
- jump and double jump
- dash
- slide
- camera look
- weapon tilt/sway
- shooting
- melee
- projectile spawning
- tracer spawning
- shoot animation triggering
- input callbacks

### Why It Matters

This is the most overloaded gameplay script in the project.

It currently acts as:

- movement controller
- combat controller
- partial weapon system

### Known Architectural Problem

This class is too large in responsibility and is the main reason the weapon system needs refactoring.

### Current Working Features

- WASD movement
- mouse look
- jump and double jump
- dash with 2 charges and recharge
- slide with held input
- air control
- momentum preservation
- camera-forward shooting correction
- temporary melee
- recoil animation trigger

### Known Issues / Notes

- serialized move speed in scene is still `6`, even though script default is `10`
- current melee is temporary and should be replaced by `Void Blade` in the upcoming weapon system
- combat logic here should eventually be reduced to input forwarding only

---

## [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)

### Role

Controls wave flow, enemy spawning, player death behavior, and HUD updates.

### Responsibilities

- starts waves
- counts enemies to spawn
- tracks alive enemies
- updates timer text
- reloads scene on death or on `R`
- finds player references if not assigned

### Strengths

- small and readable
- event-driven integration with `Health`

### Important Notes

- reload uses active scene build index
- this conflicts with the fact that `test.unity` is not in Build Settings
- enemies are instantiated every wave and only deactivated on death, not destroyed permanently

### Known Performance Debt

- continuous wave scaling with no pooling
- inactive dead enemies accumulate in scene hierarchy over time

---

## [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)

### Role

Very simple NavMesh-driven enemy behavior.

### Responsibilities

- follow player
- attack when in range

### Behavior

- gets target player transform
- every `Update()` calls `agent.SetDestination(player.position)`
- checks distance
- damages player if in attack range and cooldown elapsed

### Current State

- functional but primitive
- enough for Phase 0 prototype

### Known Problems

- recalculates destination every frame
- logs every attack
- does `GetComponent<Health>()` during attack instead of caching
- not suitable for large enemy counts
- no state machine, strafing, ranged logic, or crowd handling

This script is a future refactor target for Phase 3.

---

## [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)

### Role

Generic event-driven health component for player and enemies.

### Responsibilities

- store current/max health
- take damage
- emit health-change events
- emit death event
- disable GameObject on death after short delay

### Strengths

- simple
- reusable
- already event-driven

### Important Notes

- this is currently the damage integration point for all combat
- future systems should remain compatible with it unless there is a strong reason to replace it

### Known Problem

- dead objects are disabled, not cleaned up or pooled
- death logs may become expensive in mass combat

---

## [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)

### Role

Handles projectile movement and direct-hit collision damage.

### Responsibilities

- set rigidbody velocity
- expire after lifetime
- damage hit targets
- optionally spawn impact VFX
- destroy itself on impact

### Current State

- works for current prototype
- currently used by player combat code

### Known Notes

- uses instantiate/destroy lifecycle
- later likely candidate for pooling
- splash logic is not implemented yet

This script should remain reusable for the future `Plasma Launcher`.

---

## [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)

### Role

Small UI wrapper for the combat HUD.

### Responsibilities

- update player health slider
- update wave text
- update timer text
- display generic wave state messages

### Current State

- minimal but functional

### Future Direction

This will likely be expanded or replaced when the HUD grows to include:

- ammo
- current weapon
- dash charges
- kill points
- damage numbers

---

## [Assets/test/EnemyHealthBarView.cs](C:/Users/assam/DiplomGame/Assets/test/EnemyHealthBarView.cs)

### Role

Connects enemy `Health` to a UI fill image.

### Responsibilities

- subscribe to enemy health events
- update fill amount
- cleanly unsubscribe on disable

### Current State

- technically correct
- simple and reusable

### Known Performance Note

- each enemy uses a world-space canvas health bar
- this is acceptable at prototype scale, but not ideal for large enemy counts

---

## Prototype/Environment Script

## [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)

### Role

Prototype terrain generation and texturing utility.

### Responsibilities

- generate Perlin/fBM-based terrain heightmap
- stretch terrain height
- apply simple procedural texture layers

### Important Notes

- marked with `[ExecuteAlways]`
- `SampleScene` currently has `autoUpdate` enabled
- this can trigger heavy editor-side terrain generation

### Strategic Note

This system is temporary.

According to project direction:

- procedural terrain will eventually be replaced by arena generation / BSP-based rooms
- this file is useful for prototype history, but not a long-term core system

---

## Prefabs

## [Assets/Prefabs/Enemy.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Enemy.prefab)

### Contents

- basic mesh
- collider
- `NavMeshAgent`
- `Health`
- `SimpleEnemyAI`
- child world-space health bar prefab

### Role

Current prototype enemy unit.

### Notes

- enemy is still very placeholder-like
- designed for simple wave combat only

## [Assets/Prefabs/Projectile.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Projectile.prefab)

### Contents

- mesh
- collider
- rigidbody
- `Projectile` component

### Role

Reusable projectile prefab for current and future weapon logic.

## [Assets/Prefabs/UI/EnemyHealthBar.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/UI/EnemyHealthBar.prefab)

### Role

World-space health bar shown above enemies.

### Notes

- connected to `EnemyHealthBarView`
- uses world-space canvas

---

## Input Assets

## `PlayerInput`

The player in `test.unity` uses Unity Input System `PlayerInput`.

### Important Input-Related Files

- [Assets/test/PlayerInputActions.inputactions](C:/Users/assam/DiplomGame/Assets/test/PlayerInputActions.inputactions)
- [Assets/test/PlayerInputActions.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerInputActions.cs)
- [Assets/test/InputSystem.inputsettings.asset](C:/Users/assam/DiplomGame/Assets/test/InputSystem.inputsettings.asset)

### Notes

- `PlayerInputActions.cs` is generated code
- generated files should generally not be manually edited
- prefer editing the `.inputactions` asset or scene input bindings

There are also other input assets:

- `New Actions.inputactions`
- `InputSystem_Actions.inputactions`

These appear to be extra or legacy assets and are currently lower priority than the active `PlayerInputActions` setup.

---

## Animation / Model Assets

## [Assets/Recoil.anim](C:/Users/assam/DiplomGame/Assets/Recoil.anim)

Animation asset used for weapon recoil behavior.

## [Assets/test/recoil.controller](C:/Users/assam/DiplomGame/Assets/test/recoil.controller)

Animator controller for recoil playback.

## [Assets/untitledWithAnimation1.fbx](C:/Users/assam/DiplomGame/Assets/untitledWithAnimation1.fbx)

Model asset currently tied into the player weapon/animation setup in the gameplay scene.

### Important Note

There is already some viewmodel-style setup in the scene.

This matters for future weapon-system work because:

- it is better to reuse and formalize the existing viewmodel holder than to discard it blindly

---

## Render / URP Settings

Important assets:

- [Assets/Settings/PC_RPAsset.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_RPAsset.asset)
- [Assets/Settings/PC_Renderer.asset](C:/Users/assam/DiplomGame/Assets/Settings/PC_Renderer.asset)
- [Assets/Settings/DefaultVolumeProfile.asset](C:/Users/assam/DiplomGame/Assets/Settings/DefaultVolumeProfile.asset)
- [Assets/Settings/SampleSceneProfile.asset](C:/Users/assam/DiplomGame/Assets/Settings/SampleSceneProfile.asset)

### Current Situation

The project currently uses a relatively heavy URP setup for a prototype:

- shadows enabled
- multiple cascades
- SSAO enabled in renderer
- post-processing assets present
- VSync off in quality settings

### Practical Meaning

- editor CPU/GPU load can be high even in simple scenes
- `SampleScene` is particularly expensive due to terrain + URP settings

These settings are not the main focus of the next gameplay task, but they are important context for performance discussions.

---

## Implemented Systems - Functional Summary

## Movement

Current movement system supports:

- walk
- jump
- double jump
- dash with charge cooldown
- slide
- air control
- momentum preservation
- camera pitch weapon tilt

Movement is currently the most complete gameplay subsystem.

## Combat

Current combat system supports:

- basic hitscan firing
- basic projectile firing
- temporary melee
- recoil animation trigger

Combat is functional but architecturally immature.

## Enemies

Current enemy system supports:

- spawning by waves
- player chasing via NavMeshAgent
- direct melee-style damage to player
- enemy health bars

Enemy system is still prototype-grade.

## UI

Current HUD supports:

- player health
- wave number / wave state text
- next-wave timer text

UI is deliberately minimal.

## Terrain / Environment

Current environment work includes:

- prototype procedural terrain generation
- prototype terrain texturing
- simple scene geometry in combat scene

Long-term arena generation is not implemented yet.

---

## Known Technical Debt

These are the most important known architecture or engineering debts in the project.

### 1. `PlayerController` is overloaded

It should not remain the owner of:

- shooting logic
- melee logic
- projectile logic
- tracer lifecycle
- combat cooldown state

### 2. No real weapon system yet

Current combat is hardwired into the player.

Planned replacement:

- hybrid modular weapon system defined in [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

### 3. `test.unity` is not in Build Settings

Current reload behavior assumes active scene build index works correctly.

But:

- only `SampleScene` is currently registered in Build Settings

### 4. Enemies accumulate

Dead enemies are disabled, not removed or pooled.

This causes:

- scene hierarchy growth
- memory growth over long sessions
- worse scalability over time

### 5. Enemy AI scales poorly

`SimpleEnemyAI` updates path destination every frame and logs frequently.

### 6. Editor-side terrain generation is expensive

`TerrainGenerator` uses `[ExecuteAlways]` and auto-update in `SampleScene`.

### 7. World-space enemy health bars do not scale well

Acceptable now, but not ideal for later waves with many enemies.

---

## Known Performance Findings

From prior audit:

- `SampleScene` can heat CPU significantly because of:
  - terrain generation
  - terrain rendering
  - URP settings
  - post-processing
  - VSync disabled
- `test.unity` is lighter, but combat scalability is limited by:
  - `SimpleEnemyAI`
  - instantiate/destroy workflow
  - logging
  - enemy accumulation

Important practical interpretation:

- high CPU power draw in Unity Editor is not by itself proof of a bug
- but in this project there are real code and settings reasons for elevated load

---

## Current Development Direction

The next major structured task is:

- implement the weapon system defined in [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)

That means:

- extract combat out of `PlayerController`
- add `WeaponManager`
- add `WeaponDefinition`
- add fire modes
- support 5 weapons
- support switching and ammo
- replace temporary melee with `Void Blade`

This is the highest-value architectural upgrade for the current project state.

---

## Manual Unity Setup Notes

These are important current project reminders.

- add `Assets/test.unity` to Build Settings / Scene List
- verify serialized `moveSpeed` in `test.unity` if movement speed seems wrong
- verify weapon holder assignment if weapon tilt is expected

These are mentioned because some project state depends on scene/inspector wiring, not only code.

---

## What Future AI Agents Should Usually Inspect First By Task Type

## If task is about movement

Read:

- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about weapons/combat

Read:

- [WEAPON_SYSTEM_TZ.md](C:/Users/assam/DiplomGame/WEAPON_SYSTEM_TZ.md)
- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test/Projectile.cs](C:/Users/assam/DiplomGame/Assets/test/Projectile.cs)
- [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about enemies or waves

Read:

- [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/Prefabs/Enemy.prefab](C:/Users/assam/DiplomGame/Assets/Prefabs/Enemy.prefab)

## If task is about UI

Read:

- [Assets/test/UIManager.cs](C:/Users/assam/DiplomGame/Assets/test/UIManager.cs)
- [Assets/test/EnemyHealthBarView.cs](C:/Users/assam/DiplomGame/Assets/test/EnemyHealthBarView.cs)
- [Assets/test.unity](C:/Users/assam/DiplomGame/Assets/test.unity)

## If task is about terrain or world generation

Read:

- [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)
- [Assets/Scenes/SampleScene.unity](C:/Users/assam/DiplomGame/Assets/Scenes/SampleScene.unity)

## If task is about performance

Read:

- this file
- [Assets/test/PlayerController.cs](C:/Users/assam/DiplomGame/Assets/test/PlayerController.cs)
- [Assets/test/SimpleEnemyAI.cs](C:/Users/assam/DiplomGame/Assets/test/SimpleEnemyAI.cs)
- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/Scripts/TerrainGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/TerrainGenerator.cs)
- URP assets in `Assets/Settings`

---

## Suggested Maintenance Strategy For This Knowledge Base

This file will stay useful only if it is kept compact, practical, and updated after major changes.

Recommended maintenance rules:

- update it after each major subsystem refactor
- do not log every tiny code change here
- keep it focused on architecture, scene wiring, and important facts
- keep roadmap details in `PROGRESS.md`
- keep implementation specs in dedicated files like `WEAPON_SYSTEM_TZ.md`

Recommended future companion files:

- `KNOWN_ISSUES.md` for live bugs and technical debt
- `AI_HANDOFF.md` for short-term current-task context
- subsystem-specific specs if other big refactors appear

---

## Final Summary

This project is currently a playable Unity prototype with:

- strong movement prototype
- basic combat prototype
- basic wave/enemy loop
- simple HUD
- temporary terrain prototype

The most important current truth is:

- movement is relatively mature
- combat works but is not architecturally mature
- the next major step is the modular weapon system

This file should let future AI agents understand the project quickly without doing a blind full scan first.

