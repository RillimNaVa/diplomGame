# Void Survivor - Phase 3 Enemy AI (Technical Specification)

**Status:** DRAFT v2 / READY FOR REVIEW  
**Date:** 2026-04-27 (revised 2026-04-27)  
**Phase:** 3  
**Scope horizon:** PR 3.A -> PR 3.F, plus one optional special enemy after the core set is stable  
**Owner:** gameplay / enemy AI  

---

## 1. Goal

Phase 3 turns the current prototype enemy into a small but readable enemy system for a fast FPS roguelike.

The goal is not realistic AI. The goal is a set of clear combat roles that force the player to move, choose targets, manage distance, and take risks for drops.

The first shippable Phase 3 target is:

- one reusable enemy state-machine base;
- four main enemy roles;
- simple spawn composition by budget, weights, and arena index;
- fair telegraphs, recovery windows, and active-attack limits;
- full compatibility with the existing Phase 2 arena encounter pipeline.

---

## 2. Current Project Context

### Existing Enemy Pipeline

Current enemy behavior is centered on:

- `Assets/test/SimpleEnemyAI.cs`
  - gets a player target;
  - uses `NavMeshAgent`;
  - calls `SetDestination(player.position)` every frame;
  - attacks in melee range;
  - logs every attack.
- `Assets/test/GameManager.cs`
  - spawns one `enemyPrefab`;
  - calls `SimpleEnemyAI.SetTarget(playerTransform)`;
  - listens to enemy `Health.onDeath`;
  - fires `OnEnemyKilled`;
  - exposes encounter mode through `BeginEncounter(...)`.
- `Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs`
  - starts arena encounters;
  - counts enemy deaths;
  - opens soft-lock barriers after clear.
- `Assets/test/Health.cs`
  - owns health, damage, death event, and disable-after-death behavior.
- `Assets/Scripts/Combat/Enemies/EnemyStagger.cs`
  - listens to health changes;
  - enters one-way stagger state at low HP;
  - drives current glory-kill readability.

### Existing Phase 2 Constraints

Phase 3 must keep the Phase 2 arena/run flow working:

- runtime generated arena -> async NavMesh bake -> `EncounterController.BeginEncounter()` -> `GameManager.BeginEncounter(...)`;
- `combatSpawnPoints` remain the source of spawn positions;
- `GameManager.OnEnemyKilled` remains the kill event for kill streaks and future HUD/progression;
- `Health.onDeath` remains the authoritative death event;
- `PR 2.H` beveled prefabs and Arena Complex are not prerequisites.

---

## 3. Design Direction

### Core Rule

Enemies should be simple, readable combat problems, not complex tactical agents.

A good enemy in this project must:

- have a clear role;
- be readable by behavior, color, silhouette, and attack timing;
- have obvious counterplay;
- combine well with 1-2 other enemy types;
- avoid unfair damage from invisible, instant, or behind-the-back attacks.

### Main Enemy Roles

Phase 3 ships four main roles:

| Role | Enemy | Purpose |
|---|---|---|
| Fodder / melee mass | Drone | Creates kills, pressure, and drop opportunities |
| Fast melee pressure | Crawler | Forces movement and distance management |
| Ranged threat | Plasma Spitter / Sentinel | Punishes standing still and creates target priority |
| Tank / bruiser | Station Brute | Controls space and breaks simple circular kiting |

Optional after the core set is stable:

| Role | Enemy | Purpose |
|---|---|---|
| Zoner / special | Gravity Node | Creates slow/pull zones and supports the void-station fantasy |

### Why Not Five Main Enemies Immediately

The project currently has only one prototype enemy. Building five full behaviors, full AI Director logic, pooling, and arena-complex spawn rules at once would create too much coupling and too many untested variables.

Phase 3 should first prove that four roles feel good in the current single-arena encounter loop.

---

## 4. Explicitly Out Of Scope

Do not include these in the first Phase 3 implementation unless the user explicitly reprioritizes:

- full Left 4 Dead-style AI Director;
- Arena Complex / multi-room spawn logic;
- Shield Drone support enemy;
- complex Brute charge/pathfinding;
- final enemy models, animations, or high-quality VFX;
- enemy pooling in PR 3.A;
- projectile pooling;
- advanced squad tactics, cover usage, or flanking;
- biome-specific AI behavior.

Deferred does not mean rejected. These are later candidates after the core enemy loop is stable.

---

## 5. Architecture

### 5.1. File Structure

Recommended new code location:

```text
Assets/Scripts/Combat/Enemies/
  AI/
    EnemyRole.cs
    EnemyAIState.cs
    IEnemyTargetReceiver.cs
    EnemyBrainBase.cs
    MeleeEnemyBrain.cs
    RangedEnemyBrain.cs
    BruteEnemyBrain.cs
    ActiveAttackSlotManager.cs
  Data/
    EnemyData.cs
    EnemySpawnProfile.cs
    EnemySpawnEntry.cs
  Projectiles/
    EnemyProjectile.cs
  Spawn/
    EnemySpawnComposer.cs
    EnemyPool.cs              (PR 3.F only)
```

Keep legacy files in `Assets/test/` compatible until scene/prefab migration is verified.

### 5.2. EnemyRole

```csharp
public enum EnemyRole
{
    Fodder,
    Chaser,
    Ranged,
    Tank,
    Zoner,
    Boss   // reserved for Phase 4 — not used in Phase 3 spawn composition
}
```

### 5.3. EnemyAIState

Use a simple shared state vocabulary:

```csharp
public enum EnemyAIState
{
    Spawn,
    Move,
    Telegraph,
    Attack,
    Recover,
    Reposition,
    Staggered,
    Dead
}
```

Not every enemy must use every state.

### 5.4. IEnemyTargetReceiver

`GameManager` currently calls `SimpleEnemyAI.SetTarget(Transform)`. The new system must keep an equivalent public contract.

```csharp
public interface IEnemyTargetReceiver
{
    void SetTarget(Transform target);
}
```

During migration `SimpleEnemyAI` itself implements `IEnemyTargetReceiver`. `GameManager` then talks only to the interface — no `if (component is SimpleEnemyAI) ... else ...` fallback branch. Once all prefabs/scenes are migrated to the new brain classes, `SimpleEnemyAI` can be deleted without touching `GameManager` again.

### 5.5. EnemyData ScriptableObject

`EnemyData` stores role and tuning, not behavior code.

Minimum fields:

```csharp
[CreateAssetMenu(menuName = "Void Survivor/Enemies/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyId;
    public string displayName;
    public EnemyRole role;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    public float damage = 10f;

    [Header("Attack")]
    public float attackRange = 1.5f;
    public float attackCooldown = 1f;
    public float telegraphTime = 0.25f;
    public float recoveryTime = 0.35f;

    [Header("Ranged")]
    public GameObject projectilePrefab;     // null for melee enemies
    public float projectileSpeed = 10f;
    public float preferredDistance = 12f;   // ranged hold-distance target
    public float lineOfSightCheckInterval = 0.2f;

    [Header("Heavy / Brute")]
    public float slamRadius = 0f;           // 0 disables area attack
    public LayerMask slamHitMask;           // layers eligible for slam damage

    [Header("Spawn")]
    public int spawnCost = 1;
    public int minArenaIndex = 0;
    public int maxAlive = 20;
    public float spawnWeight = 1f;
}
```

Notes:

- All enemies share one `EnemyData` SO type — no `RangedEnemyData : EnemyData` subclass. Ranged/Heavy fields stay default-zero on melee enemies and are simply ignored by `MeleeEnemyBrain`.
- Spawn-related fields (`spawnCost`, `minArenaIndex`, `maxAlive`) act as **defaults**. `EnemySpawnEntry` (see §7.2) may override any of them per profile — see override rule below.
- Optional fields can still be added per behavior later, but avoid turning `EnemyData` into a giant universal config before the first enemies are working.

### 5.6. EnemyBrainBase

`EnemyBrainBase` owns shared runtime concerns:

- target assignment and player auto-resolve fallback;
- cached `NavMeshAgent`;
- cached own `Health`;
- cached target `Health`;
- state transitions;
- throttled path updates;
- `agent.isOnNavMesh` guard from PR 2.C;
- stopping movement during telegraph/attack/recover;
- death-state transition;
- optional stagger hook.

Rules:

- no `SetDestination()` every frame;
- no attack log spam;
- no `GetComponent<Health>()` inside attack every time;
- no direct replacement of `Health` death contract.

#### Damage application (melee)

Melee damage is applied during the `Attack` state through the same `Health.TakeDamage(...)` contract used elsewhere. The brain re-checks range at the moment of impact (after `telegraphTime`) — if the target left the attack range during the wind-up, the hit whiffs and the brain still goes to `Recover`. No direct manipulation of player health components from the brain.

#### Stagger integration

When `EnemyStagger` reports a state change at low HP, the brain must:

- transition to `EnemyAIState.Staggered` and abort any in-progress `Telegraph` / `Attack`;
- cancel pending damage application for that attack (no "ghost hit" after stagger);
- release any active attack slot held via `ActiveAttackSlotManager` (PR 3.E);
- stop `NavMeshAgent` movement until stagger ends or until `Health.onDeath` fires.

`EnemyStagger` remains the source of truth for the stagger flag. The brain only reacts to it.

### 5.7. Behavior Classes

Use a small inheritance split:

- `MeleeEnemyBrain`
  - Drone and Crawler both use this class with different `EnemyData`;
  - approach target;
  - telegraph;
  - apply melee damage if still in range;
  - recover.
- `RangedEnemyBrain`
  - Plasma Spitter / Sentinel;
  - keeps preferred distance;
  - rotates toward player;
  - checks line of sight;
  - telegraphs and fires `EnemyProjectile`;
  - repositions if player is too close.
- `BruteEnemyBrain`
  - heavy melee/tank;
  - slow approach;
  - telegraphed area slam;
  - long recovery;
  - optional short shove/step, but no complex charge in v1.

### 5.8. SimpleEnemyAI Migration

`SimpleEnemyAI.cs` must not disappear in the same PR that introduces the base system unless every prefab and scene reference is migrated and verified.

Recommended safe path:

1. Add new AI classes.
2. Convert `SimpleEnemyAI` into a compatibility wrapper or subclass-like adapter.
3. Keep `SetTarget(Transform)` public.
4. Update `Enemy.prefab` only after compile succeeds.
5. Keep legacy wave mode working when `GameManager.useEncounterMode = false`.

---

## 6. Enemy Specifications

### 6.1. Drone

**Role:** fodder / swarm / drop source  
**Implementation:** `MeleeEnemyBrain` + low-cost `EnemyData`

Purpose:

- creates mass and tempo;
- gives frequent kills;
- supports HP orb / future pickup risk loop;
- makes AoE and shotgun-style weapons feel useful.

Behavior:

- directly approaches player;
- may use slight repath offset later, but v1 can chase directly;
- low HP;
- low damage;
- short attack range;
- low spawn cost;
- medium/high max alive.

Suggested starting tuning:

| Field | Value |
|---|---|
| maxHealth | 40 |
| moveSpeed | 3.6 |
| damage | 6 |
| attackRange | 1.35 |
| attackCooldown | 1.0 |
| telegraphTime | 0.15 |
| recoveryTime | 0.25 |
| spawnCost | 1 |
| minArenaIndex | 0 |
| maxAlive | 16 |

Acceptance:

- current encounter can spawn several Drones;
- kills still trigger `OnEnemyKilled`;
- `EnemyStagger` still works if attached;
- no Console spam while many Drones attack.

### 6.2. Crawler

**Role:** fast melee pressure  
**Implementation:** `MeleeEnemyBrain` + higher-pressure `EnemyData`

Purpose:

- forces player to keep moving;
- makes distance management matter;
- teaches telegraph -> dodge -> punish rhythm.

Behavior:

- faster than Drone;
- medium HP;
- medium damage;
- short telegraph before attack;
- recovery after missed/finished attack.

Suggested starting tuning:

| Field | Value |
|---|---|
| maxHealth | 80 |
| moveSpeed | 4.8 |
| damage | 14 |
| attackRange | 1.7 |
| attackCooldown | 1.25 |
| telegraphTime | 0.35 |
| recoveryTime | 0.45 |
| spawnCost | 2 |
| minArenaIndex | 0 |
| maxAlive | 8 |

Acceptance:

- player can read the attack wind-up;
- Crawler is dangerous in close range but not instant/unfair;
- Crawler does not stunlock the player when several are alive.

Tuning note: before locking `moveSpeed = 4.8`, verify against the current `PlayerMovement` base walk speed and dash/slide cooldown. Crawler must be **faster than the player's base walk** (so kiting requires effort) but **slower than dash burst** (so dash always creates separation). If player base speed is below ~5.0, drop Crawler to ~1.1× player walk speed instead of using the literal value above.

### 6.3. Plasma Spitter / Sentinel

**Role:** ranged pressure  
**Implementation:** `RangedEnemyBrain` + `EnemyProjectile`

Purpose:

- punishes standing still;
- creates target priority;
- makes cover and diagonal movement meaningful.

Behavior:

- tries to stay at a preferred distance;
- stops or slows while charging a shot;
- fires a slow visible projectile;
- repositions if the player gets too close;
- should not use hitscan in v1.

Minimum projectile rules:

- projectile has owner/team filtering so enemies do not damage themselves unless explicitly intended;
- projectile is visible and dodgeable;
- projectile lifetime is finite;
- projectile damage goes through `Health.TakeDamage`.

Suggested starting tuning:

| Field | Value |
|---|---|
| maxHealth | 90 |
| moveSpeed | 3.0 |
| damage | 12 |
| attackRange | 18 |
| preferredDistance | 12 |
| attackCooldown | 2.0 |
| telegraphTime | 0.7 |
| recoveryTime | 0.5 |
| projectileSpeed | 10 |
| spawnCost | 3 |
| minArenaIndex | 1 |
| maxAlive | 4 |

Acceptance:

- player can dodge projectiles by strafing/dashing;
- Spitter does not shoot through walls if line-of-sight check fails;
- Spitter creates pressure without dominating every encounter.

### 6.4. Station Brute

**Role:** tank / bruiser / space control  
**Implementation:** `BruteEnemyBrain`

Purpose:

- blocks simple circular kiting;
- forces the player to route around a large threat;
- creates a high-priority target when combined with Drones or Spitters.

Behavior:

- slow approach;
- high HP;
- telegraphed area slam;
- long recovery after slam;
- no complex charge/pathfinding in v1.

Suggested starting tuning:

| Field | Value |
|---|---|
| maxHealth | 260 |
| moveSpeed | 2.2 |
| damage | 28 |
| attackRange | 3.0 |
| slamRadius | 4.0 |
| attackCooldown | 3.0 |
| telegraphTime | 0.9 |
| recoveryTime | 1.1 |
| spawnCost | 6 |
| minArenaIndex | 2 |
| maxAlive | 1 |

Slam damage application:

- damage is applied **once**, at the end of `telegraphTime`, not continuously;
- use `Physics.OverlapSphere(slamOrigin, slamRadius, EnemyData.slamHitMask)` to find candidate hits;
- for each hit, resolve `Health` (cached per overlap, not per frame) and call `Health.TakeDamage(EnemyData.damage, ...)`;
- ignore self and other enemies via `slamHitMask` (player layer + destructibles only);
- `slamOrigin` is the Brute's feet position at the impact frame, not at telegraph start — this lets the player escape by movement during the wind-up.

`heavySlots = 1` from §8.3 will enforce only one Brute slamming at once, but until PR 3.E ships the slot manager, **`EnemyData.maxAlive = 1` for Brute is mandatory** to prevent simultaneous slams in PR 3.C.

Acceptance:

- slam is clearly telegraphed;
- player can escape the slam radius with movement;
- Brute never attacks silently or instantly;
- only one Brute is active in normal encounters unless explicitly tuned otherwise.

### 6.5. Gravity Node (Optional)

**Role:** zoner / special  
**Implementation timing:** after Drone, Crawler, Spitter, Brute, and spawn composition are stable

Purpose:

- creates slow/pull zones;
- breaks infinite circle-running;
- strengthens the void-station identity.

Rules for optional implementation:

- zone must be clearly visible on the floor;
- effect must be mild enough not to invalidate dash/slide controls;
- Node should be destroyable;
- do not spawn it in small Start/Shop/Rest arenas;
- do not combine it with too many fast enemies in small spaces.

Gravity Node is explicitly not required for the first Phase 3 acceptance.

---

## 7. Spawn Composition

### 7.1. Why Spawn Composition Is Needed

The current `GameManager` spawns one prefab repeatedly. That is not enough once enemies have roles.

Phase 3 needs controlled composition:

- which enemy types can appear;
- how many can appear;
- when each type unlocks;
- how much each type costs;
- which combinations are allowed.

### 7.2. EnemySpawnEntry

```csharp
[Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public EnemyData data;
    public int minArenaIndex;   // 0 = inherit from EnemyData
    public int spawnCost;       // 0 = inherit from EnemyData
    public int maxAlive;        // 0 = inherit from EnemyData
    public float weight;        // 0 = inherit from EnemyData.spawnWeight
}
```

**Override rule:** for `minArenaIndex`, `spawnCost`, `maxAlive`, `weight` — if the value on `EnemySpawnEntry` is `> 0`, it overrides `EnemyData`. If it is `0` (default), the value falls through to `EnemyData`. This keeps `EnemyData` as the single source of baseline tuning while letting one profile rebalance specific encounters without cloning SOs.

### 7.3. EnemySpawnProfile

```csharp
[CreateAssetMenu(menuName = "Void Survivor/Enemies/Enemy Spawn Profile")]
public class EnemySpawnProfile : ScriptableObject
{
    public EnemySpawnEntry[] entries;
    public int baseBudget = 8;
    public int budgetPerArenaIndex = 3;
    public int maxEnemyTypesPerEncounter = 3;
}
```

### 7.4. Composer Rules

`EnemySpawnComposer` resolves an encounter roster before spawning.

Rules:

- budget = `baseBudget + arenaIndex * budgetPerArenaIndex`;
- ignore entries where `arenaIndex < minArenaIndex`;
- do not exceed `maxAlive`;
- do not include more than `maxEnemyTypesPerEncounter`;
- prefer 1-2 roles in early arenas, 2-3 roles later;
- fallback to Drone if no entry is valid, **and emit `Debug.LogWarning`** with the reason (no profile / all entries gated out / budget too low) — silent fallback hides spawn-profile bugs.

**arenaIndex ownership:** `EncounterController` is the single authority for the current `arenaIndex`. It passes the value into `GameManager.BeginEncounter(...)` (or directly into `EnemySpawnComposer.Compose(arenaIndex, profile)`). No other system reads or writes arenaIndex independently — Run state may store it for save/HUD, but the composer takes its input only from `EncounterController`.

Recommended costs:

| Enemy | Cost |
|---|---|
| Drone | 1 |
| Crawler | 2 |
| Plasma Spitter | 3 |
| Station Brute | 6 |
| Gravity Node | 5 |

### 7.5. Introduction Curve

| Arena index | Allowed enemy types |
|---|---|
| 0 | Drone, Crawler |
| 1 | Drone, Crawler, Spitter |
| 2 | Drone, Crawler, Spitter, Brute |
| 3 | Drone, Crawler, Spitter, Brute |
| 4 / Boss | Brute + curated support, or Warden if added later |

`Void Warden` from older roadmap notes is deferred until the four-role core is stable. If implemented later, it should be treated as a boss/miniboss profile, not part of normal spawn composition.

### 7.6. Bad Combination Guards

Avoid unfair mixes:

- no Brute + Gravity Node + many Crawlers in small arenas;
- no more than one Brute in normal combat;
- no more than three Spitters shooting at once;
- do not spawn every enemy type in one encounter;
- do not spawn enemies directly behind the player without warning.

---

## 8. Combat Readability

### 8.1. Telegraphs

Every non-trivial attack needs a warning:

- Drone: very short wind-up is enough;
- Crawler: 0.3-0.5s wind-up before melee hit;
- Spitter: visible charge before projectile;
- Brute: long wind-up before slam.

Placeholder telegraph is acceptable:

- emissive material pulse;
- scale pulse;
- color flash;
- simple warning decal/circle for Brute slam;
- short audio cue later in Phase 5.

### 8.2. Recovery

Strong attacks need recovery windows:

- Crawler pauses after attack;
- Spitter has cooldown after shot;
- Brute has long recovery after slam.

This lets the player punish enemies and makes damage feel fair.

### 8.3. Active Attack Slots

Even if many enemies are alive, not all should attack at once.

Recommended v1 limits:

```text
meleeSlots = 3
rangedSlots = 3
heavySlots = 1
specialSlots = 1
```

Implementation can be simple:

- `ActiveAttackSlotManager` is a scene singleton or `GameManager` child;
- enemies request a slot before entering `Telegraph`;
- if no slot is available, enemy keeps moving/repositioning;
- slot is released after `Recover` or `Dead`.

This can ship in PR 3.E after basic enemy types are working.

### 8.4. Fair Spawn Rules

Enemies may spawn at generated combat spawn points, but the player must not feel ambushed unfairly.

Rules:

- avoid spawning very close to player;
- if close spawn is unavoidable, use a spawn delay/telegraph;
- do not activate damage during `Spawn` state;
- future arena-complex spawn points should support room-local groups, but Phase 3 does not implement arena complex.

---

## 9. PR Split

### PR 3.A - Enemy State Machine Base + Drone/Crawler Variant

Scope:

- add `EnemyRole`, `EnemyAIState`, `IEnemyTargetReceiver`;
- add `EnemyData`;
- add `EnemyBrainBase`;
- add `MeleeEnemyBrain`;
- migrate current enemy behavior safely;
- keep `SimpleEnemyAI.SetTarget(Transform)` compatibility;
- create Drone and Crawler data/prefab variants, or one prefab with clear data slots if prefab creation is deferred.

Acceptance:

- current `Enemy.prefab` still works or has a verified replacement;
- enemies still spawn through `GameManager` in legacy and encounter mode;
- `Health.onDeath`, `GameManager.OnEnemyKilled`, `EnemyLootTable`, and `EnemyStagger` still work;
- no `SetDestination()` every frame;
- no repeated attack log spam;
- `dotnet build Assembly-CSharp.csproj` passes after Unity project files are refreshed if needed.

### PR 3.B - Plasma Spitter / Sentinel

Scope:

- add `RangedEnemyBrain`;
- add `EnemyProjectile`;
- add Spitter/Sentinel data and prefab variant;
- add line-of-sight check;
- add simple hold-distance behavior.

Acceptance:

- projectiles are visible and dodgeable;
- projectiles damage player through `Health.TakeDamage`;
- enemy does not shoot through solid walls when LoS is blocked;
- Spitter is introduced only from arenaIndex >= 1 unless manually spawned.

### PR 3.C - Station Brute

Scope:

- add `BruteEnemyBrain`;
- add area slam with telegraph and recovery;
- add Brute data and prefab variant;
- enforce low maxAlive / high spawn cost.

Acceptance:

- Brute is slow, readable, and dangerous;
- slam can be avoided with movement;
- no complex charge/pathfinding in v1;
- normal encounters do not spawn more than one Brute by default.

### PR 3.D - Spawn Composition

Scope:

- add `EnemySpawnEntry`;
- add `EnemySpawnProfile`;
- add `EnemySpawnComposer`;
- update `GameManager.BeginEncounter(...)` path to accept or resolve a spawn profile while preserving old single-prefab fallback;
- connect arenaIndex to spawn composition.

Acceptance:

- encounters spawn a role mix by budget;
- old `enemyPrefab` fallback still works;
- enemy introduction curve works by arena index;
- bad-combination guards are enforced.

### PR 3.E - Combat Readability + Active Attack Slots

Scope:

- add `ActiveAttackSlotManager`;
- add attack slot requests for melee/ranged/heavy;
- add placeholder telegraph visuals;
- add fair-spawn delay for close spawns if needed.

Acceptance:

- many enemies can be alive, but only limited attacks resolve at once;
- player deaths feel traceable to readable threats;
- no encounter uses all enemy types at once by default.

### PR 3.F - Enemy Pooling + Projectile Pooling

Scope:

- add `EnemyPool`;
- add a small `EnemyProjectilePool` (Spitter projectiles fire on a 2s cadence with up to 4 Spitters alive — ~120 instantiates/min in long fights, worth pooling alongside enemies);
- replace repeated instantiate/disable accumulation where safe;
- reset `Health`, brain state, `EnemyStagger`, health bar, NavMeshAgent, loot/drop lifecycle, and projectile collider/trail state on reuse;
- keep death events firing exactly once per kill.

Acceptance:

- long runs do not accumulate disabled enemy or projectile objects unbounded;
- kills still count correctly;
- HP orbs/drop logic still works;
- pooled enemies start with clean health and AI state;
- pooled projectiles start with cleared owner/team filter and zero velocity until launched.

### Optional PR 3.G - Gravity Node

Only start after PR 3.A-3.E are stable.

Scope:

- add zone enemy;
- add visible slow/pull area;
- add destroyable core;
- gate it out of small/safe arenas.

Acceptance:

- zone is readable;
- movement remains fun;
- encounter composition avoids unfair Gravity Node combinations.

---

## 10. Testing Checklist

### Compile / Static Checks

- `dotnet build Assembly-CSharp.csproj`
- If the build misses new scripts, refresh/reimport in Unity before trusting the external build result.

### Manual Unity Play Mode

Use `Assets/test.unity`.

Setup reminders:

- `GameManager.useEncounterMode = true` for run/arena tests;
- `Run` active;
- legacy `ArenaDebug` disabled during normal run tests;
- start a fresh run after AI code changes.

Test scenarios:

- legacy wave mode (`useEncounterMode = false`) still spawns enemies;
- encounter mode spawns enemies after fade-out;
- enemies walk on runtime-baked NavMesh;
- enemies do not throw `SetDestination on inactive agent`;
- killing enemies opens barriers;
- player death still triggers reload flow;
- HP orb drops still appear from enemy deaths;
- stagger/glory-kill visuals still trigger at low HP where configured.

### Enemy Behavior Tests

Drone:

- spawns in groups;
- dies quickly;
- creates drop/kill tempo.

Crawler:

- reaches player faster than Drone;
- telegraphs melee attack;
- has recovery.

Spitter:

- keeps distance;
- charges and fires visible projectile;
- projectile can be dodged.

Brute:

- approaches slowly;
- telegraphs slam;
- player can escape slam radius.

### Encounter Composition Tests

- arenaIndex 0 does not spawn Brute by default;
- arenaIndex 1 can introduce Spitter;
- arenaIndex 2+ can introduce Brute;
- no normal encounter spawns all enemy types at once;
- maxAlive limits are respected;
- fallback to Drone works if spawn profile is missing or invalid.

---

## 11. Documentation Updates After Implementation

After each Phase 3 PR:

- update `docs/PROGRESS.md` checklist and Change Log;
- update `docs/AI_HANDOFF.md` Current Status / Recommended Next Task;
- update `docs/PROJECT_KNOWLEDGE_BASE.md` if architecture or scene wiring changes;
- update `docs/KNOWN_ISSUES.md` if issue #5 / #6 / #7 is closed or partially mitigated;
- update this file if implementation intentionally deviates from the spec.

When Phase 3 is accepted in Unity Editor, mark this file:

```text
Status: COMPLETED (YYYY-MM-DD)
```

Do not delete it. It is useful diploma context.

---

## 12. Final Phase 3 Acceptance

Phase 3 can be considered complete when:

- the old single-behavior enemy has been replaced by a stable state-machine system;
- Drone/Crawler, Spitter, and Brute are playable in generated arena encounters;
- spawn composition creates controlled role mixes by arena index;
- attacks are readable and not unfairly simultaneous;
- current Phase 2 run/encounter flow still works from Start to Boss;
- kill-to-survive systems remain connected through `Health`, `EnemyLootTable`, `EnemyStagger`, and `GameManager.OnEnemyKilled`;
- pooling is either implemented in PR 3.F or explicitly deferred with an updated `KNOWN_ISSUES.md` note.

---

## 13. Revision Log

### 2026-04-27 — DRAFT v2 (pre-implementation review)

Clarifications added before PR 3.A. No structural changes to PR split or enemy roster.

- §5.2 — `EnemyRole.Boss` marked as reserved for Phase 4 (kept in enum to avoid future churn, not used in Phase 3).
- §5.4 — migration path simplified: `SimpleEnemyAI` itself implements `IEnemyTargetReceiver`; `GameManager` talks to the interface only, no fallback branch.
- §5.5 — added `[Header("Ranged")]` (projectilePrefab, projectileSpeed, preferredDistance, lineOfSightCheckInterval) and `[Header("Heavy / Brute")]` (slamRadius, slamHitMask) sections to the shared `EnemyData`. No `RangedEnemyData` subclass — melee enemies leave ranged/heavy fields default-zero.
- §5.5 — clarified that spawn fields on `EnemyData` are defaults; `EnemySpawnEntry` overrides per profile.
- §5.6 — added explicit damage-application contract for melee (range re-check at impact, `Health.TakeDamage`, no ghost hits).
- §5.6 — added stagger integration paragraph: brain transitions to `Staggered`, aborts in-progress attack, releases attack slot, stops agent.
- §6.2 — added Crawler tuning note: verify `moveSpeed` against actual `PlayerMovement` walk/dash before locking the literal value.
- §6.4 — specified Brute slam damage path (`Physics.OverlapSphere` at impact frame using `slamHitMask`, single hit per swing, slamOrigin sampled at impact not at telegraph start). Until PR 3.E lands the slot manager, `EnemyData.maxAlive = 1` for Brute is mandatory.
- §7.2 — `EnemySpawnEntry` override rule documented: value `> 0` overrides `EnemyData`, value `0` inherits.
- §7.4 — composer must `Debug.LogWarning` on Drone fallback (no silent fallback). `EncounterController` declared as the single authority for `arenaIndex`.
- §9 PR 3.F — pooling scope extended to include `EnemyProjectilePool` for Spitter projectiles, with reset rules for collider/trail/owner-filter.

### 2026-04-27 — PR 3.A code-landing note (player-speed reality check)

While implementing PR 3.A the actual `PlayerController.moveSpeed` was confirmed at **10 m/s** (with a 25 m/s dash burst). The spec values for Drone (`moveSpeed = 3.6`) and Crawler (`moveSpeed = 4.8`) are unreachable against a player who simply walks away. Per the §6.2 tuning guidance ("verify against actual `PlayerMovement` … drop Crawler to ~1.1× player walk speed instead of using the literal value"), the Editor `EnemyData` SOs should be authored with overridden speeds rather than the literal §6.1/§6.2 numbers. Recommended starting overrides for the first SO assets:

| Enemy   | Spec moveSpeed | Recommended SO override | Why |
|---------|----------------|-------------------------|-----|
| Drone   | 3.6            | ~6.5                    | Slower than player walk so kiteable, fast enough to maintain swarm pressure when player is reloading or attacking. |
| Crawler | 4.8            | ~9.0                    | Just under player walk (10), so dash always creates separation but standing still is dangerous. |

Brute and Spitter speeds (2.2 / 3.0) are intentionally slow and remain as spec.

The §6.1/§6.2 tables themselves are not edited — the spec stays the design intent, and this Revision Log entry records the implementation reality. Future tuning passes may revisit once `PlayerController` itself is rebalanced.

