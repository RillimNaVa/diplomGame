# Void Survivor — Phase 4 Roguelike Progression TZ

> **Project:** Void Survivor / DiplomGame  
> **Phase:** Phase 4 — Roguelike Progression  
> **Engine:** Unity 6 / URP  
> **Core direction:** DOOM Eternal / Ultrakill fast FPS + roguelike run progression  
> **Status:** Technical specification for implementation — revision v3  

> **Revision v3 (2026-05-02):** filled gaps from review pass — graph topology, stacking math, style/streak timing, triggered-effect semantics, multi-weapon modifier resolution, max-HP behavior, reward edge cases, Elite encounter modifier, PlayerStats wiring, Boss specifics, scenario-level acceptance. Curse references removed (deferred system).  

> **Naming note:** earlier repo history already used `PR 4.A` / `PR 4.B` for combat-feel polish. To avoid confusing future agents and changelog entries, progression implementation tasks in this document use the prefix `PR 4.P*` (`P` = Progression).

---

## 1. Phase 4 Goal

Phase 4 adds a run-based roguelike progression layer to the existing single-arena run pipeline.

The target loop:

```text
Start Room
→ Combat Arena
→ Reward Cards
→ Door Choice
→ Combat / Elite / Shop / Rest
→ More Rewards / Purchases
→ Final Prep
→ Boss
→ Victory / Game Over / Run Stats
```

The progression system must strengthen the already implemented FPS gameplay:

- fast movement;
- dash / slide / double jump;
- aggressive shooting;
- glory kill;
- HP-orb sustain;
- enemy stagger;
- arena clear flow;
- door-choice run graph.

The goal is **not** to build a large permanent meta progression yet. The first version should focus on strong run-to-run variation inside one run.

---

## 2. Core Design Principles

### 2.1 Progression must reinforce combat, not replace it

Upgrades should not turn the game into a passive numbers simulator. They should make the player interact more strongly with:

- weapon choice;
- movement;
- positioning;
- kill streaks;
- glory kill timing;
- HP-orb pickup decisions;
- elite-room risk.

Good upgrade:

```text
After a glory kill, gain temporary armor and fire-rate boost.
```

Weak upgrade:

```text
+3% damage.
```

Small numeric upgrades are allowed, but they should be simple baseline rewards, not the entire system.

### 2.2 Kill-to-survive is the core philosophy

Kill-to-survive should not be only one upgrade category. It is the central identity of the progression system.

The progression should reward:

- staying aggressive;
- executing staggered enemies;
- killing quickly;
- taking calculated risks;
- choosing Elite rooms when the build can handle them.

### 2.3 Keep MVP controlled

Do not start Phase 4 with:

- full permanent meta progression;
- complex cursed upgrades;
- complex status effects;
- Binding of Isaac-level synergy chains;
- huge upgrade pool;
- random run length from 8 to 15 arenas.

The first implementation should be data-driven, readable, and testable.

---

## 3. Scope Split

Phase 4 should be treated as a full milestone with a smaller first playable cut.

### 3.1 First playable cut

The first playable cut proves that run-based upgrades work inside the current arena pipeline.

It includes:

- `UpgradeData` ScriptableObject;
- `UpgradeSystem` runtime component;
- active upgrade stacks;
- stack limits;
- 10–12 basic upgrades;
- passive modifier API for weapons / player / sustain;
- reset on new run / death;
- 3-card reward selection after Combat / Elite clear;
- seed-based reward generation;
- rarity weights;
- reward-gated exits so the player cannot leave before choosing a card.

It does **not** include yet:

- 10-room run graph rewrite;
- Kill Points economy;
- Shop room;
- Rest room;
- weapon offers;
- run statistics screen.

### 3.2 Full Phase 4 scope

The full Phase 4 milestone adds:

- fixed Standard Run length of 10 visited rooms;
- improved door-choice structure;
- Elite room reward rules;
- Kill Points economy;
- Shop room;
- Rest room;
- final prep before Boss;
- basic run statistics.

### 3.3 Deferred beyond Phase 4

Do not include in Phase 4:

- permanent meta upgrades between runs;
- full curse system;
- large legendary dependency tree;
- complicated elemental status system;
- dynamic run length 8–15;
- triple jump upgrade;
- complex inventory;
- weapon shop offers unless the base shop is already stable.

---

## 4. Run Length and Run Structure

### 4.1 Current issue

The current run has only 5 arena nodes:

```text
Start → Mid × 3 → Boss
```

One node is a mostly empty start room. This is enough for pipeline testing, but too short for roguelike progression.

Problems with 5 nodes:

- player receives too few upgrade choices;
- build does not have time to develop;
- Shop economy barely matters;
- Rare / Epic reward curve is compressed;
- Boss checks mostly raw FPS skill, not the run build.

### 4.2 Decision: fixed 10-room Standard Run

Phase 4 should use a fixed Standard Run of **10 visited rooms**.

Important distinction:

```text
visitedRunLength = rooms the player actually enters during one run
generatedGraphNodeCount = all generated alternatives in the run graph
```

The target is:

```text
visitedRunLength = 10
```

The generated graph may contain more than 10 nodes if it stores alternative door choices. Do not use `RunGraph.nodes.Count` as the player-facing run length.

Recommended structure:

```text
0. Start Room
1. Combat
2. Combat
3. Combat / Elite
4. Shop / Rest / Combat
5. Combat
6. Combat / Elite
7. Elite / Combat / Shop
8. Final Prep: Shop / Rest / Combat
9. Boss
```

Implementation rule:

- `arenaIndex` should represent the visited-room index in the active path, not the absolute index inside the generated graph list.
- player-facing room progress should be derived from visited path progress;
- reward rarity, shop inventory, enemy scaling, and KP payout should use visited path progress;
- graph generation can still prebuild alternatives, but balance must not depend on how many unvisited alternatives exist.

### 4.3 Meaningful room count

Although the run has 10 visited rooms, the Start Room should not count as a combat arena in the player-facing UI.

Player-facing display should be closer to:

```text
Start Room
Combat Arena 1/8
Combat Arena 2/8
...
Boss Arena
```

The goal is around:

- 6–7 combat/elite rooms before boss;
- 1–2 shop/rest decisions;
- 5–7 reward-card choices per run;
- 1–2 elite-risk opportunities.

### 4.4 Variable run length

Do **not** implement fully random 8–15 arena runs in the first version.

The range is too wide and creates balance problems:

- 8-room and 15-room runs have different economy;
- scaling curves become harder to tune;
- player power varies too much;
- test time increases significantly.

Future-safe design:

```csharp
public enum RunLengthMode
{
    Short,      // 8 nodes
    Standard,   // 10 visited rooms
    Long        // 12 nodes
}
```

For the first run-graph rewrite, implement only:

```text
RunLengthMode.Standard = 10 visited rooms
```

### 4.5 Graph topology

The current `RunGraphGenerator` produces 8 nodes in 5 stages (1+2+2+2+1) with **branch factor = 2** and **shared subtree wiring** (each mid node points to both next-stage mids).

Phase 4 keeps the same approach but extends it:

```text
Stage 0: Start            (1 node)
Stage 1: Combat           (2 alternative nodes)
Stage 2: Combat           (2 alternative nodes)
Stage 3: Combat / Elite   (2 alternative nodes)
Stage 4: Shop / Rest / Combat  (3 alternative nodes — wider choice tier)
Stage 5: Combat           (2 alternative nodes)
Stage 6: Combat / Elite   (2 alternative nodes)
Stage 7: Elite / Combat / Shop (3 alternative nodes)
Stage 8: Final Prep       (2 alternative nodes — Shop / Rest / Combat options)
Stage 9: Boss             (1 node)
```

Rules:

- **Branch factor:** 2 doors per choice for stages 1-3, 5-6, 8; **3 doors** for "wide tier" stages 4 and 7. Door labels show node category.
- **Shared subtree wiring** is preserved (each parent points to all children of next stage). This keeps generation cheap and avoids combinatorial explosion. Final visited path length is still 10 regardless of branching.
- `RunStage` enum is replaced by `int stageIndex` (0..9) on `RunGraphNode`. Existing `Start/Mid1/Mid2/Mid3/Boss` cases are migrated to `stageIndex` 0/1-3/9 respectively for legacy compatibility, then deprecated.
- **Room category constraints** (avoid two Shops in a row, guarantee one Shop + one Elite before Boss) are enforced at generation time, not at door-pick time. The generator may resample a stage's category pool when a constraint would be violated.
- `arenaIndex` (used by spawn composition, KP payout, rarity weights) is `visitedPath.Count - 1` at the moment the player enters the arena, **not** `node.stageIndex` directly. They will usually be equal, but if a stage is skipped or replaced this decouples balance from graph shape.

---

## 5. Room Types

### 5.1 Start Room

Purpose:

- spawn player;
- introduce run;
- optional weapon preview / debug info;
- no reward;
- no combat.

Rules:

- does not grant Kill Points;
- does not show reward cards;
- does not count as combat arena in player-facing UI.

### 5.2 Combat Room

Standard arena encounter.

Reward:

- 3 upgrade cards after clear;
- Kill Points clear reward;
- combat style bonus.

### 5.3 Elite Room

Harder encounter with higher reward.

MVP implementation does not require new Elite enemy types. Elite pressure is delivered via a new `EliteEncounterModifier` SO (or a flag block on `ArenaTypeProfile`) applied **on top of** the existing `EnemySpawnProfile`:

```csharp
[CreateAssetMenu(menuName = "Void Survivor/Progression/Elite Modifier")]
public sealed class EliteEncounterModifier : ScriptableObject
{
    [Header("Composition")]
    public float budgetMultiplier = 1.35f;
    public EnemyData[] guaranteedEnemies;     // e.g. force one Brute + one Spitter
    public float spawnTempoMultiplier = 1.15f; // shorter delay between waves

    [Header("Stats")]
    public float enemyHpMultiplier = 1.20f;
    public float enemyDamageMultiplier = 1.0f; // keep at 1.0 for MVP
}
```

Integration:

- `EncounterController` reads the modifier from the resolved `ArenaTypeProfile` of the current node.
- `EnemySpawnComposer` multiplies budget and applies guaranteed-spawn slots before the regular weighted pick.
- `Health.maxHealth` is multiplied at spawn time via the existing `PooledEnemy.PrepareForReuse` path (does **not** mutate `EnemyData`).

Reward:

- guaranteed Rare+ reward chance boost (see §10.3 Elite reward modifier);
- higher Kill Points clear reward;
- higher style bonus cap.

### 5.4 Shop Room

Safe room with purchase options.

MVP shop content:

```text
- 1 heal offer
- 2 upgrade offers
- 1 reroll terminal
- exit door
```

Rules:

- no enemies;
- no reward cards after entering;
- no free upgrade;
- inventory generated seed-based;
- player cannot afford everything in a normal run.
- weapon offers are deferred until the base shop works.

### 5.5 Rest Room

Safe strategic room.

MVP options:

```text
REST CHAMBER
[1] Heal 35% max HP
[2] Gain +10 max HP until end of run
[3] Convert 15 Kill Points into Rare reward chance boost for next reward
```

Rules:

- no enemies;
- no shop inventory;
- no random reward card;
- one choice only;
- after the chosen effect applies, exit unlocks via the same reward-gate mechanism as combat rooms (§6.4), but with a 0-card "rest selection" instead of a 3-card draw.

### 5.6 Boss Room

Final encounter.

Rules:

- no regular card reward after clear; victory ends the run;
- victory screen after boss death;
- run stats shown after victory or death;
- KP / style accumulated during boss fight are **discarded** (run is over).

Boss enemy:

- For Phase 4 MVP, "Boss" is a beefed-up Brute variant: `Boss.asset` `EnemyData` with `EnemyRole.Boss`, `maxHealth ≈ 800`, larger slam radius, longer telegraph, plus 2-3 Spitter adds spawned mid-fight via `EncounterController`.
- A unique boss model/abilities are deferred to Phase 5 art pass.
- `ActiveAttackSlotManager` heavy-slot cap (1) already accommodates the boss without changes.

---

## 6. Door Choice System

### 6.1 Core rule

Door choice must show a preview.

Bad:

```text
Door 1 / Door 2 / Door 3
```

Good:

```text
Combat — normal reward
Elite — harder, Rare+ reward
Shop — spend Kill Points
Rest — recover
```

### 6.2 Recommended door weights

Default door generation after a normal combat room:

| Door Type | Weight |
|---|---:|
| Combat | 55% |
| Elite | 20% |
| Shop | 15% |
| Rest | 10% |

Rules:

- avoid two Shops in a row;
- avoid two Rests in a row;
- guarantee at least one Shop before Boss;
- guarantee at least one Elite opportunity before Boss;
- Boss is always final node.

### 6.3 Door labels

Door label examples:

```text
COMBAT
Reward: Upgrade Card

ELITE
Reward: Rare+ Chance

SHOP
Spend Kill Points

REST
Recover / Prepare
```

### 6.4 Reward-gated exit contract

Current arena flow opens soft-lock barriers when `EncounterController` reaches `Cleared`.

For Phase 4, Combat and Elite rooms need an additional gate:

```text
Encounter Cleared
→ combat stops
→ RewardPending = true
→ exit triggers / barriers stay locked
→ reward cards appear
→ player chooses one card
→ UpgradeSystem applies upgrade
→ RewardPending = false
→ exits unlock
```

Rules:

- Start, Shop, Rest, and Boss rooms do not show reward cards.
- Combat and Elite rooms must not let the player trigger `RunController.ChooseDoor(...)` before reward selection completes.
- If reward UI fails to generate cards, log a warning and unlock exits as a fallback so the run cannot soft-lock.
- The reward gate should live near run/progression orchestration, not inside every door trigger.
- `ExitDoorTrigger` can keep using `SoftLockBarrier.IsOpen`, but barrier opening must wait for reward completion.

Recommended owner:

```text
RunProgressionController
```

This controller subscribes to arena clear events, shows reward UI when needed, and releases exits after the selected reward is applied.

---

## 7. Upgrade Categories

Use 5 categories.

```csharp
public enum UpgradeCategory
{
    WeaponCore,
    MobilityCore,
    SustainCore,
    CombatTempo,
    RareMutator
}
```

### 7.1 WeaponCore

Affects weapon performance:

- damage;
- fire rate;
- reload speed;
- magazine size;
- piercing;
- splash on special condition.

### 7.2 MobilityCore

Affects movement:

- dash cooldown;
- dash charge;
- speed after kill;
- slide damage;
- air-control bonus if needed later.

### 7.3 SustainCore

Affects survival through aggression:

- glory kill heal;
- HP orb heal;
- shield after kill;
- armor after glory kill;
- HP orb magnet.

### 7.4 CombatTempo

Affects pace and kill chaining:

- kill streak boost;
- fast-kill speed;
- execute slow-mo;
- stagger threshold;
- temporary reload / fire-rate burst.

### 7.5 RareMutator

Rare build-shaping effects:

- chain lightning;
- last-shot explosion;
- radial stagger after execute;
- overdrive after kill streak.

---

## 8. UpgradeData ScriptableObject

### 8.1 Enums

```csharp
public enum UpgradeRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum UpgradeEffectType
{
    WeaponDamageMultiplier,
    FireRateMultiplier,
    ReloadSpeedMultiplier,
    MagazineSizeFlat,
    DashChargeFlat,
    DashCooldownMultiplier,
    SpeedAfterKill,
    GloryKillHealFlat,
    HpOrbHealFlat,
    HpOrbMagnetRadius,
    ShieldAfterKill,
    PiercingFlat,
    ChainLightningChance,
    SplashOnLastShot,
    StaggerThresholdBonus,
    SlowMoOnExecute
}
```

### 8.2 Recommended fields

```csharp
[CreateAssetMenu(menuName = "Void Survivor/Progression/Upgrade Data")]
public sealed class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Classification")]
    public UpgradeRarity rarity;
    public UpgradeCategory category;
    public UpgradeEffectType effectType;

    [Header("Stacking")]
    public int maxStacks = 1;

    [Header("Values")]
    public float valueA;
    public float valueB;
    public float duration;

    [Header("Targeting")]
    public string targetWeaponId; // Empty = all weapons

    [Header("Availability")]
    public bool canAppearInReward = true;
    public bool canAppearInShop = true;
    public int minArenaIndex = 1;

    [Header("Shop")]
    public int baseShopPrice = 25;
}
```

### 8.3 Implementation rule

Do not create one C# class per upgrade in MVP.

Use:

```text
UpgradeData + UpgradeSystem + switch/effect registry
```

This is easier to tune and better for ScriptableObject-driven Unity workflow.

---

## 9. UpgradeSystem

### 9.1 Responsibilities

`UpgradeSystem` stores and applies active run upgrades.

Responsibilities:

- store active stacks;
- add selected upgrades;
- enforce stack limits;
- calculate modifiers;
- expose modifier API to weapons / player / pickups / glory kill;
- expose event hooks for conditional upgrades;
- reset on run start / death;
- support seeded reward generation.

### 9.2 Data structure

```csharp
[Serializable]
public sealed class ActiveUpgradeStack
{
    public UpgradeData data;
    public int stacks;
}
```

### 9.3 Suggested API

```csharp
public sealed class UpgradeSystem : MonoBehaviour
{
    public IReadOnlyList<ActiveUpgradeStack> ActiveUpgrades => activeUpgrades;

    public bool CanAdd(UpgradeData data);
    public void AddUpgrade(UpgradeData data);
    public int GetStackCount(string upgradeId);

    public float GetAdditive(UpgradeEffectType type);
    public float GetMultiplier(UpgradeEffectType type);
    public float GetWeaponMultiplier(string weaponId, UpgradeEffectType type);

    public void NotifyEnemyKilled(GameObject enemy);
    public void NotifyGloryKill(GameObject enemy);
    public void NotifyWeaponFired(WeaponBase weapon);
    public void NotifyDamageDealt(Health target, float damage);
    public void NotifyPlayerDamaged(float damage);
    public void NotifyArenaCleared(ArenaCategory category, int visitedArenaIndex);

    public void ResetForNewRun();
}
```

### 9.4 Passive modifiers vs triggered effects

Do not force every upgrade into the same calculation path.

Use two lanes:

```text
Passive modifiers:
- damage multiplier
- fire-rate multiplier
- reload multiplier
- max HP bonus
- dash cooldown multiplier
- HP orb heal bonus

Triggered effects:
- shield after kill
- chain lightning
- last-shot explosion
- slow-mo on execute
- no-ammo overdrive
- slide damage
```

Passive modifiers should be read by systems when they calculate final values.

Triggered effects should run from explicit event hooks. This keeps fire modes, pickups, glory-kill code, and player movement from growing unrelated upgrade logic.

### 9.4.1 Stacking math

All passive `*Multiplier` effects of the same `UpgradeEffectType` stack **additively**, then are combined **multiplicatively** between different effect types.

```text
sumStack(effectType) = Σ (stack.valueA × stack.stacks)
multiplier(effectType) = 1 + sumStack(effectType)

finalDamage = baseDamage
            × multiplier(WeaponDamageMultiplier)
            × multiplier(EliteHunterBonus when target is Elite)
```

Flat additive effects (`*Flat`) sum directly:

```text
finalMaxHp = baseMaxHp + Σ flatStack(MaxHpFlat)
```

Caps from §15 are applied **after** stacking but **before** the per-frame final calculation:

```text
multiplier = Mathf.Min(1 + sum, 1 + cap)
```

Conditional / weapon-specific stacks (e.g. `targetWeaponId == "plasma_rifle"`) use a separate sum that only contributes when the active weapon matches; see §9.5.1.

### 9.4.2 Triggered-effect semantics matrix

For every triggered upgrade in §11, the implementation must declare three properties: **trigger event**, **refresh policy**, and **stack scaling**. Default rules below — individual upgrades may override but must do so explicitly in their `UpgradeData` description.

| Property | Default rule |
|---|---|
| Refresh policy | Re-trigger **resets** active timer to full duration. Does not extend. |
| Stack scaling | 2nd+ stack scales **magnitude**, not duration (e.g. shield amount, slow-mo strength). Duration stays fixed. |
| Concurrency | Multiple distinct triggered effects can run in parallel. Same-effect re-trigger refreshes (no double-shield). |
| Cleanup on arena enter | All active triggered effects **clear** at `EnterArena` start. Player begins each room at baseline. |
| Cleanup on death/run reset | Always cleared (§9.6). |

Per-upgrade declarations:

| Upgrade | Trigger | Duration | Stack scales |
|---|---|---:|---|
| Combat Injector | OnEnemyKilled | 3.0s | speed % |
| Vampiric Momentum | OnEnemyKilled | 2.5s | shield amount |
| Execution Armor | OnGloryKill | 3.0s | DR % (capped at 40%) |
| Blood Rush | OnGloryKill | 4.0s | fire-rate % |
| Crisis Protocol | HP threshold ≤ 35% (poll) | while threshold met | n/a (max stacks 1) |
| Last Round Detonation | OnWeaponFired (last shot in mag) | instant | n/a (max stacks 1) |
| Chain Spark | OnDamageDealt (15% per stack additive, cap 25%) | instant | trigger chance |
| Slow-Mo on Execute / Void Execution | OnGloryKill | 0.4s slow-mo | non-stacking (§15) |
| Storm Circuit | OnGloryKill (forces Chain Spark) | instant | n/a (max stacks 1) |
| Overdrive Loop | Every 5 kills (counter) | 3.0s | n/a (max stacks 1) |

The `UpgradeSystem` event hooks (§9.3 `Notify*` methods) are the only entry points for triggered effects. No upgrade reaches into weapon/player code directly.

### 9.5 Runtime modifier seams

Do not permanently mutate base data like `WeaponDefinition.damage` or `PlayerController.moveSpeed`.

Preferred approach:

```text
finalValue = baseValue × UpgradeSystem modifier
```

Required seams:

| System | Required seam |
|---|---|
| Weapon damage | modifier provider in `WeaponContext` or equivalent weapon stat resolver |
| Fire rate / cooldown | `WeaponBase` must calculate cooldown through modifier API, not only `definition.FireCooldown` |
| Reload duration | reload coroutine should use modified duration |
| Magazine size | runtime weapon ammo state must handle temporary max clip safely |
| Dash charges | `PlayerController` needs a runtime modifier method/property, not direct permanent field mutation |
| Dash cooldown | recharge tick should read modified cooldown |
| Max HP | apply as runtime run bonus and restore baseline on run reset |
| Glory heal / HP orb heal | route through `PlayerStats` or a progression stat resolver with reset contract |
| HP orb magnet | `HealthPickup` should read magnet radius from stats/progression, not hardcode per prefab |
| Stagger threshold | `EnemyStagger` should read a modifier/provider if threshold becomes upgradeable |

### 9.5.1 Multi-weapon resolution

The player carries multiple weapons and switches between them. `UpgradeData.targetWeaponId == ""` means the upgrade applies to **all** weapons; a non-empty id restricts it to one weapon.

When a weapon resolves a final stat, it queries:

```csharp
float fireRateMul =
    upgradeSystem.GetMultiplier(FireRateMultiplier)              // global
  * upgradeSystem.GetWeaponMultiplier(weaponId, FireRateMultiplier); // weapon-specific
```

Both queries follow §9.4.1 stacking rules independently. Switching weapons immediately changes the active weapon-specific component; global modifiers are unaffected.

### 9.5.2 PlayerStats integration

The existing `PlayerStats` (Kill-to-Survive PR A+B) already owns runtime player numbers (max HP, glory heal amount, HP-orb heal amount). Phase 4 keeps that ownership:

```text
PlayerStats reads from UpgradeSystem at query time:
- MaxHp        = baseMaxHp + UpgradeSystem.GetAdditive(MaxHpFlat)
- GloryHeal    = baseGloryHeal + UpgradeSystem.GetAdditive(GloryKillHealFlat)
- HpOrbHeal    = baseHpOrbHeal × UpgradeSystem.GetMultiplier(HpOrbHealMultiplier)
- OrbMagnet    = baseMagnetRadius + UpgradeSystem.GetAdditive(HpOrbMagnetRadius)
```

`PlayerStats` does **not** subscribe to upgrade events — it pulls values lazily. The `UpgradeSystem` only fires events when a stat changes, and `PlayerStats` recalculates on demand. This avoids cascade-recompute bugs.

`Health.maxHealth` is updated when `MaxHpFlat` changes: increase grows current HP by the same delta (heal up by upgrade amount); decrease (only on run reset) clamps current HP. See §9.6.

### 9.6 Reset contract

All Phase 4 progression effects are run-scoped unless explicitly stated otherwise.

Reset on:

- new run;
- player death;
- returning to main menu;
- debug restart.

Must reset:

- active upgrade stacks;
- temporary shields / armor;
- temporary speed / fire-rate buffs;
- max HP run bonus;
- modified dash charges/cooldowns;
- reward-pending state;
- Kill Points;
- current style points;
- shop reroll counters;
- rest-room one-choice state;
- run statistics.

Do not rely on scene reload as the only reset mechanism. The current run pipeline can restart through `RunController.StartRun(...)`, so progression must expose explicit reset methods.

---

## 10. Reward Cards

### 10.1 Flow

After arena clear:

```text
Arena Cleared
→ pause combat / prevent door entry
→ generate 3 reward cards
→ player chooses 1
→ apply upgrade
→ unlock doors
```

### 10.2 Card rules

- always show 3 cards;
- no duplicate upgrade IDs in the same choice;
- do not show maxed-out upgrades;
- respect `minArenaIndex`;
- respect rarity weights;
- generated from run seed + visited arena index + reward counter;
- reward UI should show rarity, title, description, current stacks, max stacks.

Reward UI must be modal enough that the player cannot accidentally leave the room while selecting a card. Time does not have to be paused globally, but new enemy spawns and exit transitions must be blocked while `RewardPending = true`.

### 10.2.1 Edge cases

- **No valid cards generated** (all common upgrades maxed in a Common-only roll, very rare): fall back to filling remaining slots from the next-rarity tier. If still empty, log warning and unlock exits with no reward applied (§6.4 fail-safe).
- **Player dies during reward selection:** standard Game Over. Run state is reset; no card is applied.
- **Pause menu opened during selection:** `Time.timeScale = 0`, reward UI stays open underneath; resuming returns to the same 3 cards (state is preserved, not regenerated).
- **Input scheme:** keyboard `1` / `2` / `3` selects the matching card; mouse click on a card also selects. `Esc` opens pause, never cancels selection.
- **Re-entry after death:** new run starts fresh; reward seed re-derives from the new `runSeed`.
- **Reroll inside reward UI:** not in MVP. Rerolls live only in Shop (§13).

### 10.3 Rarity weights

For Standard 10-room run:

| Arena Index | Common | Rare | Epic | Legendary |
|---:|---:|---:|---:|---:|
| 1 | 80% | 20% | 0% | 0% |
| 2 | 75% | 23% | 2% | 0% |
| 3 | 68% | 28% | 4% | 0% |
| 4 | 62% | 32% | 6% | 0% |
| 5 | 55% | 37% | 8% | 0% |
| 6 | 50% | 40% | 10% | 0% |
| 7 | 45% | 42% | 12% | 1% |
| 8 | 40% | 44% | 14% | 2% |

Elite reward modifier:

```text
Common -15%
Rare +10%
Epic +4%
Legendary +1%
```

Normalize weights after modifier application.

---

## 11. Initial Upgrade Pool

MVP should start with 12–16 upgrades.

### 11.1 Common upgrades

| Upgrade | Effect | Max Stacks | Category |
|---|---|---:|---|
| Overcharged Rounds | +12% weapon damage | 3 | WeaponCore |
| Rapid Core | +8% fire rate | 3 | WeaponCore |
| Reinforced Frame | +15 max HP | 3 | SustainCore |
| Blood Circuit | +8 HP from glory kill | 2 | SustainCore |
| Orb Magnet | +35% HP orb pickup radius | 2 | SustainCore |
| Dash Capacitor | -12% dash cooldown | 2 | MobilityCore |
| Combat Injector | +12% speed for 3 sec after kill | 2 | CombatTempo |
| Stagger Pressure | +5% stagger threshold | 2 | CombatTempo |

### 11.2 Rare upgrades

| Upgrade | Effect | Max Stacks | Category |
|---|---|---:|---|
| Piercing Shot | Hitscan pierces +1 enemy | 1 | WeaponCore |
| Dash Battery | +1 dash charge | 1 | MobilityCore |
| Vampiric Momentum | Kill grants temporary shield | 2 | SustainCore |
| Last Round Detonation | Last magazine shot deals splash damage | 1 | RareMutator |
| Execution Armor | -25% incoming damage for 3 sec after glory kill | 1 | SustainCore |
| Orb Overcharge | HP orbs heal +50%, but expire faster | 1 | SustainCore |

### 11.3 Epic upgrades

| Upgrade | Effect | Max Stacks | Category |
|---|---|---:|---|
| Chain Spark | 15% chance to chain lightning to nearby enemy | 2 | RareMutator |
| Blood Rush | After glory kill, +25% fire rate for 4 sec | 1 | CombatTempo |
| Kinetic Slide | Sliding through enemies deals damage / stagger buildup | 1 | MobilityCore |
| Crisis Protocol | Below 35% HP: +20% speed and reload speed | 1 | CombatTempo |
| Elite Hunter | +20% damage against Brute / Elite targets | 1 | WeaponCore |

### 11.4 Legendary upgrades

Only add after common/rare/epic work correctly.

| Upgrade | Effect | Max Stacks | Category |
|---|---|---:|---|
| Void Execution | Glory kill triggers short radial slow-mo and nearby stagger | 1 | RareMutator |
| Storm Circuit | Chain Spark always triggers after glory kill | 1 | RareMutator |
| Overdrive Loop | Every 5 kills: 3 sec no ammo consumption | 1 | CombatTempo |

---

## 12. Kill Points Economy

### 12.1 Core decision

Kill Points should be a valuable shop currency, not cheap coins dropped by every enemy.

Do **not** use direct enemy-to-currency payout as the main economy:

```text
Drone kill → +1 KP
Crawler kill → +2 KP
Spitter kill → +3 KP
```

This makes the economy scale with enemy count and becomes hard to balance.

Instead:

```text
Kills increase combat style.
Arena clear converts style into capped Kill Points bonus.
```

### 12.2 Arena payout formula

```text
Total Kill Points = Clear Reward + Style Bonus + Risk Bonus
```

Where:

```text
Style Bonus is capped by room type.
```

### 12.3 Clear reward

| Room Type | Clear Reward |
|---|---:|
| Combat | 10 + arenaIndex × 2 KP |
| Elite | 18 + arenaIndex × 3 KP |
| Rest | 0 KP |
| Shop | 0 KP |
| Boss | no regular KP required |

Examples:

| Arena Index | Combat Reward | Elite Reward |
|---:|---:|---:|
| 1 | 12 KP | 21 KP |
| 2 | 14 KP | 24 KP |
| 3 | 16 KP | 27 KP |
| 4 | 18 KP | 30 KP |
| 5 | 20 KP | 33 KP |

### 12.4 Style points

Style points are earned during combat.

| Action | Style Points |
|---|---:|
| Kill | +1 |
| Glory kill | +1 bonus (in addition to the +1 kill) |
| Brute kill | +2 (replaces the +1 kill) |
| Kill streak ≥ 5 (per kill while streak active) | +2 bonus |
| Fast clear (clear time < 0.7 × expected) | +3 (one-shot at clear) |
| No-hit clear (no damage taken in arena) | +5 (one-shot at clear) |

Timing and reset:

- **Arena scope:** style points reset to 0 on `EnterArena` and are evaluated at `EncounterController.Cleared`.
- **Kill streak:** reuses the existing `KillStreakTracker` from Kill-to-Survive PR A+B. Streak window is its current `streakDecaySeconds` (default 4s). "Kill streak ≥ 5" bonus applies per kill **while** the streak counter is at 5 or higher at the moment of that kill.
- **Fast clear baseline:** `expectedClearTime` per arena = `enemyBudget × 1.5s` (rough proxy; tuned in PR 4.PH).
- **No-hit clear:** tracked by listening to `Health.OnDamaged` on the player between `EnterArena` and `Cleared`. Heal events do not break it.

At clear:

```text
styleKP = min(stylePoints, styleCap)
```

UI rule:

- **Do not** show floating `+1 STYLE` per kill (same anti-spam reasoning as KP §12.6).
- A small style meter on the HUD ticks up silently during combat. Full breakdown appears in the post-clear payout panel (§12.6).

Style cap:

| Room Type | Style KP Cap |
|---|---:|
| Combat | 8 KP |
| Elite | 12 KP |

### 12.5 Combat Style Rank option

The UI can show style rank instead of raw style points.

Combat room:

| Rank | Bonus |
|---|---:|
| C | +2 KP |
| B | +4 KP |
| A | +6 KP |
| S | +8 KP |

Elite room:

| Rank | Bonus |
|---|---:|
| C | +4 KP |
| B | +7 KP |
| A | +10 KP |
| S | +12 KP |

### 12.6 Payout UI

After clear, show:

```text
Arena Cleared
Base Reward: +14 KP
Combat Style: +7 KP
Elite Bonus: +0 KP
Total: +21 KP
```

Do not spam `+1 KP` after every enemy kill.

---

## 13. Shop Economy

### 13.1 Expected income

Expected before first Shop:

| Path | Expected KP |
|---|---:|
| Combat + Combat | 35–45 KP |
| Combat + Elite | 50–60 KP |

This means the player can usually buy:

- one Rare upgrade;
- or Common upgrade + heal;
- or save for stronger purchase later.

### 13.2 Prices

| Item | Price |
|---|---:|
| Small Heal | 10 KP |
| Medium Heal | 18 KP |
| Full Heal | 32 KP |
| Common Upgrade | 26 KP |
| Rare Upgrade | 44 KP |
| Epic Upgrade | 70 KP |
| Weapon Offer | 40–55 KP, deferred beyond base shop |
| Reroll 1 | 8 KP |
| Reroll 2 | 14 KP |
| Reroll 3 | 22 KP |

### 13.3 Shop rule

The player should almost never be able to buy the full shop.

Correct shop feeling:

```text
I have 42 KP.
I can buy:
- a Rare upgrade,
- or a Common upgrade + small heal,
- or heal + reroll.
```

Wrong shop feeling:

```text
I cleared two rooms and bought everything.
```

---

## 14. Difficulty Scaling

### 14.1 Scaling goal

With 10 visited rooms, difficulty scaling must be softer than in a 5-room run.

Avoid simply making enemies much tankier every arena.

Preferred scaling sources:

- enemy composition;
- more mixed roles;
- Spitter / Brute frequency;
- better spawn pressure;
- Elite room modifiers;
- mild HP scaling.

### 14.2 Suggested formulas

```text
enemyBudget = baseBudget + arenaIndex × budgetStep
```

```text
enemyHpMultiplier = 1.0 + arenaIndex × 0.07
```

```text
eliteHpMultiplier = 1.25 + arenaIndex × 0.04
```

Avoid in MVP:

```text
+15% HP every arena
+15% damage every arena
large enemy count spike every arena
```

### 14.3 Damage scaling

Enemy damage scaling should be conservative.

Recommended:

```text
enemyDamageMultiplier = 1.0 + arenaIndex × 0.03
```

Or defer damage scaling entirely until after playtesting.

---

## 15. Upgrade Balance Caps

The player should become stronger, but not infinitely safe.

Recommended caps:

| Parameter | Cap |
|---|---:|
| Weapon damage bonus | +45–60% |
| Fire rate bonus | +30–35% |
| Dash charges | 3 total |
| Damage reduction | 40% |
| Glory kill heal | base + 20 |
| HP orb heal | base × 2 |
| Chain lightning chance | 25% |
| Speed after kill | +25% |
| Slow-mo effects | non-stacking |

### 15.1 Max HP behavior

Specific rules for `MaxHpFlat` (Reinforced Frame, Rest "Gain +10 max HP"):

- Hard cap on total bonus: **+60 HP** above base. Both reward upgrades and rest-room bonuses share the same pool.
- When a `MaxHpFlat` upgrade is applied, both `Health.maxHealth` and `Health.currentHealth` increase by the upgrade amount (player heals up by the same delta — small immediate reward).
- On run reset, `Health.maxHealth` snaps back to base, `currentHealth` is clamped to new max.

### 15.2 Triple jump decision

Do not add triple jump in Phase 4 MVP.

Reason:

- can break arena verticality;
- can trivialize enemy attacks;
- can invalidate platform layout;
- extra dash is safer and easier to balance.

---

## 16. Seeded Determinism

Phase 4 should preserve deterministic behavior.

Seeded systems:

- reward card generation;
- shop inventory;
- shop reroll;
- door type selection;
- elite reward boost;
- rest options if randomized later.

Recommended seed derivation:

```text
rewardSeed = runSeed ^ visitedArenaIndex ^ rewardCounter ^ 0x44AA7711
shopSeed   = runSeed ^ visitedArenaIndex ^ 0x90BB1234
shopRerollSeed = shopSeed ^ rerollCount ^ 0x77EEAA33
restSeed   = runSeed ^ visitedArenaIndex ^ 0x5510CCDD
graphSeed  = runSeed ^ 0x12340A0A
```

Use `System.Random`, not `UnityEngine.Random`, for procedural selection.

### 16.1 Save policy

Phase 4 progression is run-scoped and memory-only.

Do not implement permanent save data in Phase 4 MVP.

Allowed runtime state:

- current run seed;
- visited room index;
- current room category;
- active upgrade stacks;
- Kill Points;
- style points for current room;
- shop reroll counter;
- run statistics.

This state may be reset by `RunController.StartRun(...)`, player death, victory restart, or debug restart.

If permanent meta progression is added later, it must be a separate system with its own save file / PlayerPrefs policy. Do not mix future permanent unlocks into `UpgradeSystem`.

---

## 17. UI Requirements

### 17.1 Reward card UI

Each card should show:

- upgrade name;
- rarity;
- category;
- description;
- stack count, e.g. `2/3`;
- effect value;
- input hint.

### 17.2 Kill Points UI

HUD should show current Kill Points.

Example:

```text
KP 42
```

After arena clear, show payout breakdown.

### 17.3 Shop UI

Each offer should show:

- item name;
- price;
- affordability state;
- description;
- purchase input.

### 17.4 Door preview UI

Door labels should show room type and reward implication.

---

## 18. Recommended Implementation Order

### PR 4.PA — UpgradeData + UpgradeSystem Core

Deliver:

- `UpgradeData.cs`;
- `UpgradeSystem.cs`;
- active stack logic;
- modifier API;
- event hook API;
- reset on new run;
- 10–12 basic upgrades;
- no UI yet if needed, debug add is acceptable.

Acceptance:

- upgrades can be applied;
- stack limits work;
- modifiers affect gameplay;
- run reset clears upgrades;
- no permanent mutation of base ScriptableObjects;
- `dotnet build Assembly-CSharp.csproj` passes.

### PR 4.PB — Runtime Modifier Hooks

Deliver:

- weapon damage/fire-rate/reload modifier integration;
- player dash charge/cooldown modifier integration;
- HP / heal / orb modifier integration;
- glory-kill heal modifier integration;
- reset contract for all modified runtime values.

Acceptance:

- upgrades affect real gameplay through modifier seams;
- removing/resetting upgrades restores baseline behavior;
- no direct permanent mutation of `WeaponDefinition` assets;
- dash UI still reads correct charge count;
- existing movement, weapons, HP orbs, glory kill, and enemy stagger still work.

### PR 4.PC — Reward Cards + Reward-Gated Exits

Deliver:

- 3-card reward UI;
- seeded reward generation;
- rarity weights;
- apply selected card;
- skip maxed upgrades;
- reward-pending state;
- exits unlock only after reward selection.

Acceptance:

- after combat clear, 3 cards appear;
- choosing one applies effect;
- same seed gives same cards;
- cards do not duplicate in one selection;
- maxed upgrades are filtered;
- player cannot leave Combat / Elite before selecting a reward;
- Start / Shop / Rest / Boss do not show card rewards.

### PR 4.PD — 10-Room Run Graph + Door Preview + Elite Modifier

Deliver:

- Standard 10 visited-room run with the topology from §4.5;
- `RunGraphNode.stageIndex` replaces `RunStage` enum (legacy mapping kept for one PR cycle);
- Start not counted as combat UI arena;
- Combat / Elite / Shop / Rest / Boss structure;
- door preview labels;
- guaranteed Shop and Elite opportunity (enforced at generation, not at pick time);
- `EliteEncounterModifier` SO + integration in `EncounterController` and `EnemySpawnComposer` (§5.3).

Acceptance:

- player path has 10 visited rooms;
- generated graph may contain extra alternative nodes;
- Boss always final;
- at least one Shop appears before Boss;
- at least one Elite option appears before Boss;
- door labels are readable;
- rarity / enemy scaling / KP calculations use `visitedArenaIndex`, not `stageIndex` directly;
- Elite arenas apply budget×1.35, hp×1.20, and at least one guaranteed enemy from the modifier.

### PR 4.PE — Kill Points Economy

Deliver:

- Kill Points runtime state;
- clear reward;
- style point tracking;
- style cap;
- clear payout UI;
- HUD KP counter.

Acceptance:

- KP is awarded on arena clear, not directly per enemy kill;
- style bonus is capped;
- Elite gives more KP than Combat;
- UI shows payout breakdown.

### PR 4.PF — Shop Room

Deliver:

- safe shop room;
- seed-based inventory;
- heal / upgrade / reroll offers;
- price rules;
- purchase flow.

Acceptance:

- player can buy affordable offers;
- player cannot buy unaffordable offers;
- reroll price increases;
- shop inventory is deterministic by seed;
- player cannot usually buy full shop after normal income;
- weapon offers are not part of the base shop unless explicitly re-scoped later.

### PR 4.PG — Rest Room + Final Prep

Deliver:

- rest room choice UI;
- heal / max HP / reward boost options;
- final prep node before boss.

Acceptance:

- only one rest option can be selected;
- rest effects apply correctly;
- final prep room can appear before Boss.

### PR 4.PH — Balance Pass + Run Stats

Deliver:

- rarity tuning;
- shop price tuning;
- enemy scaling tuning;
- run stats screen;
- debug logging for reward/economy generation.

Acceptance:

- player gets around 5–7 upgrades before boss;
- shop forces meaningful choices;
- Elite is risky but rewarding;
- boss is beatable with a reasonable build;
- no runaway economy.

### PR 4.PI — Scenario Playtest Pass

Final Phase 4 verification through scripted scenarios. Each must pass before Phase 4 is closed:

| # | Scenario | Expected outcome |
|---|---|---|
| S1 | Full 10-room run with fixed seed `1234`, no death | 5+ upgrades applied, 1+ shop visit, boss defeated, victory screen with stats |
| S2 | Same seed twice (S1 → reset → S1) | Identical card draws, identical shop inventory, identical door categories |
| S3 | Death at arena 5, then `RunController.StartRun(newSeed)` | All upgrade stacks cleared, KP=0, max HP back to base, no leaked triggered effects |
| S4 | Death during reward selection | Game Over fires, no card applied, run resets cleanly |
| S5 | Pause during reward selection | Same 3 cards on resume, no regeneration |
| S6 | Maxed-out Common pool reward roll | UI fills from next rarity tier; if empty, exits unlock with warning log |
| S7 | Elite arena entry | Budget/HP modifiers visible, guaranteed enemy spawns, KP payout > equivalent Combat |
| S8 | Two consecutive Shops | Generation must not produce this; door category constraint upheld |
| S9 | Weapon swap mid-arena with weapon-specific upgrade equipped | Modifier applies only when matching weapon active |
| S10 | Triggered effect (Combat Injector) re-trigger before duration ends | Timer resets, no double-stack |

---

## 19. Debug Tools

Add temporary debug information for tuning:

```text
Run Seed
Arena Index
Room Type
Current KP
Expected Reward Tier
Active Upgrades
Style Points
Style KP Cap
Enemy Budget
Enemy HP Multiplier
```

Debug logs should be gated behind a bool field.

---

## 20. Do Not Break

Do not break existing systems:

- movement feel;
- dash / slide / double jump;
- weapon framework;
- glory kill;
- HP-orb drops;
- enemy stagger;
- enemy pooling;
- run graph transitions;
- arena clear conditions;
- deterministic procedural generation;
- single-arena pipeline.

Do not reintroduce Arena Complex / connected multi-room maps in Phase 4.

---

## 21. Final Phase 4 Target

Phase 4 is complete when:

- Standard run has 10 visited rooms;
- player receives reward cards after combat/elite rooms;
- exits stay locked until reward selection is complete;
- upgrades affect weapons, movement, sustain, and combat tempo;
- Kill Points are awarded through clear + capped style bonus;
- Shop room creates real purchase decisions;
- Rest room provides recovery/prep choice;
- Elite room provides clear risk/reward;
- Boss is reached with a recognizable build;
- same seed produces same run reward/shop structure;
- the system is explainable in the diploma as a data-driven roguelike progression layer.

First playable cut is complete earlier, when:

- `UpgradeData` / `UpgradeSystem` exist;
- 10–12 upgrades can be applied and reset;
- 3-card rewards appear after Combat / Elite clear;
- reward-gated exits work;
- at least weapon damage, fire rate, glory heal, HP orb heal, and dash cooldown are affected by real modifiers.

---

## 22. Summary

The recommended Phase 4 design is:

```text
Fixed 10-room Standard Run
+ 3-card rewards
+ reward-gated exits
+ data-driven UpgradeSystem
+ capped Kill Points economy
+ meaningful door preview
+ Shop / Rest / Elite risk decisions
+ no permanent meta progression yet
```

This gives enough replayability for the game to feel like a roguelike without overexpanding the diploma scope.
