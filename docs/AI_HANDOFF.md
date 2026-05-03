# AI Handoff

Short-lived current-task handoff for the next AI session.
For stable architecture / roadmap / known issues, see:

- [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/docs/KNOWN_ISSUES.md)
- [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md)

---

## Current Status (2026-05-03)

**Phase 4 Roguelike Progression — PR 4.PA + PR 4.PB + PR 4.PC + PR 4.PD + PR 4.PE + PR 4.PF code landed.** PR 4.PE adds the Kill Points economy layer; PR 4.PF adds the first Shop Room implementation with deterministic heal/upgrade offers and reroll. Latest PR 4.PF polish adds a generated soft-glow platform insert, glow cross-lines with dedicated `ShopPlatformGlow(Runtime)` material, no-shadow point light, and upward particles from the Shop platform. `dotnet build Assembly-CSharp.csproj --no-restore` passes with 0 errors. Detailed scope in [PROGRESS.md](./PROGRESS.md) Phase 4 section + changelog block 2026-05-03.

**Master spec:** [PHASE_4_ROGUELIKE_PROGRESSION_TZ.md](./PHASE_4_ROGUELIKE_PROGRESSION_TZ.md) revision v3.

**Done so far in Phase 4:**
- **PR 4.PA — UpgradeSystem core.** `UpgradeData` SO, `UpgradeSystem` auto-singleton with stacking math (additive within effect type, multiplicative between, capped per §15), modifier API (`GetAdditive` / `GetMultiplier` / `GetWeaponMultiplier`), event hooks (`Notify*`), `ResetForNewRun`. Enums: `UpgradeRarity` / `UpgradeCategory` / `UpgradeEffectType` (with `BonusCap` lookup encoding §15).
- **PR 4.PB — Runtime modifier hooks.** `WeaponBase.EffectiveDamage / EffectiveFireCooldown / EffectiveReloadDuration`. 4 fire modes use `weapon.EffectiveDamage`. `PlayerController.MaxDashCharges / EffectiveDashCooldown` + top-up listener. `PlayerStats` baseline+resolver (`GetOrbHealAmount` / `GetGloryHealAmount` / `GetOrbMagnetRadius` + `MaxHpFlat` listener). `HealthPickup`, `GloryKillDetector`, `GloryKillExecutor`, `GameManager` notify hooks. 8 baseline `UpgradeData` YAML in `Assets/Resources/Progression/Upgrades/`. `UpgradeDebugProbe` auto-attached via `GameManager.ResolveReferences` (F9 add / F10 log / F11 reset).
- **PR 4.PC — Reward cards + reward-gated exits.** `EncounterController.HoldBarriers` + idempotent `OpenBarriers()`. `RewardCardGenerator` (seeded weighted-without-replacement). `RewardPreview.Build` for 8 effect types. `RewardCardCanvas` (procedural sortingOrder 6000, rarity-coloured borders, Skip button, 1/2/3 + Esc input via new Input System). `RunProgressionController` orchestrator with player freeze + cursor unlock + seeded reward counter. `RunController.OnArenaBuilt` calls `WatchEncounter`; `StartRun` calls `UpgradeSystem.ResetForNewRun()`.
- **PR 4.PD — 10-room run graph + door preview + Elite modifier.** `RunGraphNode.stageIndex` (0..9) + `Category` shortcut. `RunGraphGenerator` rewritten on hardcoded 10-stage `StageTemplates[]` with shared-subtree wiring. `RunConfig.combatPool / elitePool / shopPool / restPool`. `DefaultRunConfig.asset` updated YAML wiring all 4 pools. `EliteEncounterModifier` SO (`Assets/Scripts/ProceduralArena/Arena/EliteEncounterModifier.cs`) — budget × HP × tempo multipliers + guaranteed `EnemySpawnEntry[]`. `ArenaTypeProfile.eliteModifier` slot + `ArenaFlowController` propagation. `EnemySpawnComposer.Compose` accepts `budgetMultiplier`. `EncounterController.SpawnEnemiesViaGameManager` applies modifier and prepends guaranteed enemies. `DoorChoiceLabel` 2-line category-coloured preview per TZ §6.3. `RunController` Boss check uses `Category == Boss`.
- **PR 4.PE — Kill Points economy.** `KillPointsWallet` run-scoped auto-singleton (`Add` / `TrySpend` / `ResetForNewRun` + `OnTotalChanged`). `StylePointsTracker` tracks kills, Brute kills, glory kills, streak>=5 bonus kills, no-hit, clear time, and emits a finalized `StyleBreakdown`. `ArenaPayoutCalculator` implements TZ §12.3/§12.4 clear reward + style cap formulas. `PayoutPanel` shows Base / Combat Style / Elite Bonus / Total before reward cards. `CombatHUDController` now builds `KillPointsBlock` and `StyleMeterBlock`. `Health.AnyDeath` global event added for style tracking. `RunProgressionController.WatchEncounter` now receives `ArenaCategory`, awards KP on Combat/Elite clear, shows payout first, then chains to reward cards. `RunController.StartRun` resets `KillPointsWallet`. Follow-up fail-safe fixed: if reward generation returns 0 cards after payout, the player is unfrozen, cursor restored, and exits opened.
- **PR 4.PF — Shop Room.** `ShopOffer` + `ShopInventoryGenerator` produce one heal offer and two upgrade offers from the upgrade pool using deterministic `System.Random`, `canAppearInShop`, `minArenaIndex`, and max-stack filtering. `ShopController` prepares inventory for `ArenaCategory.Shop`, derives shop/reroll seeds from run seed + arena index, spends `KillPointsWallet`, applies `UpgradeSystem.AddUpgrade` or `Health.Heal`, and freezes/unfreezes the player around UI. `ArenaBuilder` auto-spawns a visible `ShopTerminal` platform/kiosk in Shop rooms; the platform includes `ShopPlatform_SoftGlow`, glow cross-lines, dedicated `ShopPlatformGlow(Runtime)` material, a cheap point light, and upward `ShopPlatformParticles`. `ShopTerminalTrigger` opens the prepared shop when the player steps onto the platform. `ShopCanvas` builds runtime KP/offer/reroll/close UI with 1/2/3/R/Esc input. Closing with Esc/close restores input; to reopen, step off and back onto the platform. Shop keeps `ClearCondition.None`: no enemies, no payout, no reward cards, exits stay usable after closing UI.

**Bugfix pass (same day):**
1. Reward UI was dropping player through the floor (skybox visible) — `playerController.enabled = false` stopped `controller.Move()` so CharacterController fell, AND SendMessage input callbacks still fired through disabled scripts. Fix: `PlayerController.IsFrozen` flag + `SetFrozen(bool)`. `Update()` early-returns with `controller.Move(Vector3.zero)`. Every `OnXxx` input callback checks `IsFrozen` and no-ops. `OnFire` additionally force-clears `weaponManager.SetFireHeld(false)` + an `isTriggerHeldExternal` shadow.
2. Weapon stutter / invisible tracers after taking any reward card — same root cause (LMB on cards reached `OnFire`), same fix. `RunProgressionController.FreezePlayer/UnfreezePlayer` switched to `SetFrozen` and unfreeze additionally calls `SetFireHeld(false)` for safety.
3. `ArenaRuntimeDebugOverlay` top-left counter `Arena: N/5` → `Arena: N/10` to match new Standard Run length; dropped legacy `current.stage` enum print, kept category.

**Editor playtest status:** user reported they already ran the PR 4.PE playtest and then asked to continue. No specific PR 4.PE regressions were reported in this thread. User later reported the basic PR 4.PF Shop platform interaction looked good; the new glow/particle polish still needs a focused Unity Editor visual check.

**Suggested next direction:** run one focused Unity Editor playtest for PR 4.PF Shop. After that, continue with PR 4.PG — Rest Room + Final Prep.

**Known follow-ups deferred from Phase 4 so far:**
- Author at least one `EliteEncounterModifier.asset` and drop into `Arena_Elite_L.eliteModifier` (user did this in their playtest session).
- Triggered-effect upgrades (Combat Injector, Vampiric Momentum, etc.) — TZ §9.4.2 matrix specified but not yet implemented; the `Notify*` event hooks exist but no subscribers wire actual gameplay effects.
- Curse system / `RareMutator` Legendaries — explicitly deferred per TZ §3.3.

---

## Current Status (2026-05-01)

- **Glory Kill — Space Slice rework — VERIFIED by user 2026-05-01.** Final F-press glory kill design after iterating through a camera-animation version that had multiple physics/pitch bugs. New flow: hit-stop (0.06s freeze + camera shake) → spawn `SpaceSliceFX` (7m white LineRenderer slash through enemy, randomized roll) + `WhiteFlashFX` (full-screen white flash, own Canvas at sortingOrder 5000, peak 0.55 alpha) → drop to `Time.timeScale = 0.45` slow-mo → lethal damage (existing `EnemyDissolve` + `EnemyDeathBurst` handle the death visuals) → heal player → linger 0.30s → restore. **Player is no longer locked / camera no longer rotated** — the original camera-pitch lerp + body-yaw rotation + CC/Rigidbody freeze approach caused (a) camera tilting under the map, (b) player falling through the floor, (c) snap on restore due to `PlayerController.xRotation` desync. Dropped that approach entirely. New files: `Assets/Scripts/Combat/Player/{SpaceSliceFX, WhiteFlashFX}.cs`. Wiring fixes: `GameManager.ResolveReferences` adds `GloryKillExecutor` before `CombatHUDController` so the prompt block resolves the executor; `GloryKillPromptBlock.Tick` lazy-resolves executor as a safety net; `GloryKillExecutor.FindExecuteTarget` lazy-resolves `cameraTransform` from `Camera.main` or `playerController.GetComponentInChildren<Camera>` since `Camera.main` was null at executor `Awake`. New public `PlayerController.CameraPitch` seam is unused by the final design but kept (cheap, future-proof). Debug logs in `GloryKillExecutor` (gated by `debugLog` field, default true) — leave on for now or flip to false after Phase 2 work begins.
- **Sword Feel + UI HUD Polish (Pass A + Pass B from earlier 2026-05-01) — verified by user.** Auto-attached `MeleeSwingFeedback` + `BladeTrail` + `MeleeImpactSparks` on melee weapons via `GenericWeapon.Initialize`. Combat HUD (`CombatHUDController` + procedural blocks under `Assets/Scripts/Combat/Player/HUD/`) replaces legacy slider/text UIManager widgets. Legacy `GloryKillDetector.legacyAutoKillEnabled = false` — auto-on-swing path is dormant.
- **Suggested next directions (user not yet decided):** (1) Pass C visual polish — hit-stop on regular hits, weapon bob, chromatic aberration on low HP / glory kill, vignette flash boost. (2) Phase 2 reopened — actually it's Phase 3 work that's in progress per `project_state.md`; user's next major direction per the diploma plan is to push enemy AI / encounter polish further or formalize Phase 3 completion. (3) Polish — glory kill SFX, stagger threshold tweak, killcount combo HUD, slice color variation by kill type. **User asked for a recommendation** and was advised to do Pass C first then move to next major phase work.

- **PR 5.C - Combat / Environment Feedback Polish - code landed 2026-05-01, Editor playtest pending.**
  - New `ImpactFXSystem` adds runtime muzzle flash and pooled bullet-impact decals.
  - New `PickupGlow.shader` + `HealthPickupGlow` make HP orbs pulse/glow without prefab rewiring.
  - New `ExitPortal.shader` replaces flat exit emissive panels with a subtle portal swirl.
  - New `AmbientDustMotes` adds biome-tinted floating dust per generated arena.
  - New `LampFlicker` makes ceiling lamps dim/flicker on Brute slam or nearby plasma impact; distance is checked horizontally so tall ceilings still respond.
  - New `DamageDirectionHUD` uses `Health.TakeDamage(damage, sourcePosition)` to point toward incoming damage.
  - New `SpeedBurstFeedback` adds a light dash/slide FOV kick, edge tint, and subtle speed-line particles. Keep it subtle: center-screen visibility has priority.
  - `EnemyDeathShards` adds short-lived debris and a flash-light on enemy death.
  - Ranged enemies gained simple strafing, and `EnemyBrainBase` gained lightweight separation so enemies stack less.
  - Ceiling lamp bloom was reduced after playtest feedback: smaller panels, lower fill-light intensity, lower lamp emissive intensity.
  - `dotnet build Assembly-CSharp.csproj --no-restore` clean (0 errors, 0 warnings).
  - **Pending in Unity Editor:** verify dash/slide speed feedback does not block aim, lamps are no longer overexposed, HP orb glow reads, exit portal shader compiles, bullet decals/muzzle flash work, and no ParticleSystem duration warnings return.
- **Next UI direction captured 2026-05-01:** `docs/UI_HUD_POLISH_PLAN.md` records the accepted HUD pass: HP block, ammo/current weapon, dash charges, crosshair, compact enemy counter, restyled damage direction indicator, heal feedback, no timer, arena debug overlay stays for now.

- **Post-review runtime bugfix pass - code landed 2026-04-30, Editor playtest pending.** Fixed four findings from the Claude/Codex review: `Health.CancelAutoDisable` now suppresses the later `Invoke(Disable)` scheduling during `onDeath`, `StaggerOutline` restores original `sharedMaterials` on disable/reset, `CameraShake` removes the previous frame's offset before applying the next one, and pooled `NavMeshAgent.Warp` runs only after `SetActive(true)`. `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings).
- **Arena Complex / PR 3.5 cancelled 2026-04-30 by user decision.**
  - Removed the prototype `Assets/Scripts/ProceduralArena/Complex/` module.
  - Removed `docs/ARENA_COMPLEX_TZ.md`.
  - Restored `SoftLockBarrier` to its regular exit-barrier behavior; no `NavMeshObstacle` gate support remains.
  - The active arena architecture is the existing single-arena `RunController` / `ArenaFlowController` pipeline.
  - Do not resume Arena Complex / Connected Arena Rooms unless the user explicitly reopens the idea.


- **PR 5.B — Brute Slam Shaders — code landed 2026-04-30, Editor playtest pending.** Replaces the runtime emissive-cylinder visuals on `BruteSlamDecal` (wind-up) and `SlamImpactRing` (impact) with two dedicated HLSL shaders.
  - New `Assets/Shaders/SlamWarning.shader` — polar-coords ground rune driven by `_Progress`: outer ring + pulsing inner X-cross + clockwise sweep arc (countdown filler) + 12 rotating tick marks.
  - New `Assets/Shaders/SlamShockwave.shader` — polar-coords impact effect driven by `_Progress` 0 → 1.05: hot core + primary expanding ring at radius=`_Progress` + trailing secondary ring + N radial lightning cracks with per-finger jitter via `hash11(slot)`.
  - `BruteSlamDecal.cs` and `SlamImpactRing.cs` refactored to detect the new shaders via `Shader.Find` and animate `_Progress`. Both fall back to the original PR 4.A alpha+emission cylinder ramp if the shader is missing — graceful degradation, no crashes on a stripped build.
  - `dotnet build Assembly-CSharp.csproj` clean (0/0).
  - **Pending in Unity Editor:** start a fresh run, force a Brute encounter (Combat arenaIndex 2+), and validate (a) wind-up rune now reads as a procedural countdown — clockwise sweep + pulsing X-cross + rotating ticks instead of a static orange disc; (b) impact shockwave shows a bright hot core + expanding shock ring + radiating lightning cracks instead of just an expanding emissive disc.
- **PR 5.A — Visual Polish Pass (HLSL shaders + spawn telegraph + charge beam) — code landed 2026-04-29, Editor playtest still pending.**
  - Three new HLSL shaders under `Assets/Shaders/`:
    - `EnemyDissolve.shader` — hash-based 3D world-space noise + clip threshold + emissive burning edge. Lit by `GetMainLight()` Lambert + ambient.
    - `StaggerOutline.shader` — inverted-hull `Cull Front` outline pass with HDR pulsing color.
    - `ForceField.shader` — animated hex-grid + vertical scroll waves + Fresnel rim + slow pulse, transparent two-sided.
  - New runtime components in `Assets/Scripts/Combat/Enemies/`:
    - `EnemyDissolve.cs` — auto-attached by `EnemyBrainBase.Awake`. On `Health.onDeath` builds per-renderer instance materials using the dissolve shader (copies `_BaseColor` / `_BaseMap` / `_EmissionColor` from source), ramps `_DissolveAmount` 0 → 1.05 over 1.0s. `ResetForPool` restores original sharedMaterials so pool reuse is clean.
    - `StaggerOutline.cs` — auto-attached. On `EnemyStagger.OnStaggerChanged(true)` appends an outline material slot to every renderer's `sharedMaterials`. Removes on end / pool return / disable.
    - `Spawn/SpawnTelegraph.cs` — runtime-built warning marker at every spawn point. Floor rotating emissive circle (0.4× → 1.05× scale) + vertical beam cylinder. Static `SpawnTelegraph.SpawnAt(pos, duration)` factory.
  - New runtime component in `Assets/Scripts/Combat/Enemies/AI/`:
    - `SpitterChargeBeam.cs` — auto-attached by `RangedEnemyBrain.Awake`. LineRenderer from muzzle to player during Telegraph state, thickness + brightness ramp accelerates toward firing frame. Ends on Attack / Stagger / Death / pool disable.
  - `EnemyProjectile.cs` — `autoBuildTrail = true` builds a default cyan plasma `TrailRenderer` (0.25s, 0.18→0.02 width, additive URP particle material) if none exists on the prefab. Existing `ResetForPool` already calls `TrailRenderer.Clear()` on rent.
  - `ArenaBuildMaterials.MakeForceField` — replaces the plain `MakeEmissive` for the soft-lock barrier slot, falls back to emissive cube if the ForceField shader is missing. Soft-lock barriers now read as animated energy fields, not orange boxes.
  - `GameManager` — new `spawnTelegraphDuration` (default 0.7s) + `SpawnEnemyWithTelegraph` / `SpawnEncounterEnemyWithTelegraph` coroutine wrappers. Both legacy wave loop and encounter mode pre-pick the spawn point, spawn the telegraph, wait, then rent at the exact point so the spawn is visually telegraphed.
  - `PooledEnemy.PrepareForReuse` calls `EnemyDissolve.ResetForPool` + `StaggerOutline.ResetForPool` so recycled instances start clean.
  - `RangedEnemyBrain` overrides `HandleStaggerChanged` + `HandleDeath` to call `chargeBeam.EndCharge()`.
  - `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) — verified via temp-injected csproj.
  - **Pending in Unity Editor (next step):** start a fresh run (Ctrl+P) and validate:
    1. Killed enemies dissolve over 1.0s with a glowing orange burn edge instead of just disappearing.
    2. Staggered low-HP enemies show an HDR-orange outline halo around their silhouette in addition to the existing red emissive pulse.
    3. Soft-lock barriers covering exits read as animated energy fields (hex grid + scrolling vertical waves + rim glow) instead of plain orange cubes.
    4. Spitter shows a thin cyan laser beam from muzzle to player during its 0.7s wind-up — beam ramps from thin to thick toward the firing frame.
    5. Spitter plasma projectiles leave a cyan plasma trail behind them.
    6. Every enemy spawn is preceded by a 0.7s spawn telegraph at the exact spawn point — floor circle (rotating, pulsing) plus a vertical beam from floor to ~5.5m. After the telegraph the enemy materializes via `SpawnWarpIn` (existing 0.4s scale-up).
    7. All previous PR 4.A / 4.B / 2.H1 / 3.F effects still work (hit flash, screen shake, spawn warp-in, Crawler leap, hand-authored structures, pool returns).
  - **If the dissolve / outline / force field shaders fail to compile in Unity:** check Unity Console for HLSL errors. Fallbacks: `EnemyDissolve` silently does nothing if the shader isn't found (`ResolveShader` returns null), `StaggerOutline` similarly silent, `ForceField` falls back to `MakeEmissive` (orange cube barrier). So broken shaders downgrade gracefully without crashing the game.

## Previous Status (earlier 2026-04-29)

- **Polish triple landed 2026-04-29 (PR 4.A + PR 4.B + PR 2.H1) — Editor playtest still pending** (carried over from this morning's session).
  - **Spitter buff** in `Assets/EnemyData/Spitter.asset`: `attackRange 18→28`, `preferredDistance 12→20`, `projectileSpeed 10→14`. Sniper-feel.
  - **PR 4.A — Combat Feel Pass.** New auto-attached components on every enemy via `EnemyBrainBase.Awake`: `HitFlash` (white emissive flash on damage), `EnemyDeathBurst` (24-particle burst on death, color from `EnemyData.telegraphColor`), `SpawnWarpIn` (0.55→1.0× scale + emissive pulse on every OnEnable, fires for fresh and pooled rents). New `SlamImpactRing` runtime expanding emissive ring at Brute slam frame, plus distance-falloff `CameraShake.AddTrauma` call. New `PlayerHitFeedback` auto-added by `GameManager.ResolveReferences` — drives red `PulseDamageVignette` on `ArenaPostProcessingController` + scaled `CameraShake` trauma. `CameraShake` is a singleton that auto-attaches to `Camera.main` on first `AddTrauma`.
  - **PR 4.B — Crawler Leap Attack.** New `LeapMeleeEnemyBrain` — Crawler now does a 0.45s telegraph + 0.5s kinematic-arc leap at 14 m/s when the player is at 4-8m, falls through to regular melee inside `attackRange`. Snapshot landing point lets player dodge perpendicular during travel.
  - **PR 2.H1 — Hand-Authored Cover Structures.** New `StructureDefinition` POCO + `BuiltInStructures` factory ships 4 silhouettes (bunker / sandbag line / pillar cluster / sniper nest). New `ArenaStructurePlanner.Plan` deterministic via dedicated `structureRng = seed ^ 0x8855`. New `ArenaTypeProfile.structureBudget` Range(0..5) default 2. New `ArenaRoomData.structurePlacements`. `SingleArenaGenerator` plans structures **between verticality reservation and cover** so cover/spawns avoid overlaps. `ArenaBuilder.BuildSingleStructures` uses `BuildUtils.SpawnBox` so `WorldUVScaler` + per-biome materials + instancing all apply for free.
  - `dotnet build Assembly-CSharp.csproj` clean (0/0) — verified via temp-injected csproj.
  - **Pending in Unity Editor:**
    1. *PR 4.B Editor wiring* — open `Assets/Prefabs/Enemy_Crawler.prefab`, remove `MeleeEnemyBrain`, add `LeapMeleeEnemyBrain`, drag `Crawler.asset` into the `data` field. Default leap tuning works with existing Crawler stats.
    2. *Unified playtest* — start a fresh run (Ctrl+P). Validate (a) every shot at every enemy shows a white hit flash; (b) taking a hit shows red vignette + camera shake; (c) killing an enemy spawns a particle burst tinted by its role color; (d) Brute slam shows expanding ring + camera kick; (e) every spawn has a 0.4s warp-in scale animation; (f) Crawler at 4-8m telegraphs + leaps onto player; (g) Combat/Elite arenas now contain 1-3 visible bunkers/pillars/sandbag-lines/sniper-nests without breaking NavMesh / cover / spawn; (h) Spitter sits at ~18-21m holding its hold-distance.

- **Phase 3 PR 3.F — Enemy + Projectile Pooling — code landed 2026-04-29, Editor playtest still pending** (carried over).
- **Phase 3 PR 3.E — Combat Readability + Active Attack Slots — VERIFIED 2026-04-29 by user.**
  - New `Assets/Scripts/Combat/Enemies/Spawn/EnemyPool.cs` (auto-creating scene-singleton; per-prefab `Stack<GameObject>` rent/return; tidy re-parent under the pool root on Return; null-safe Rent for scene-reload).
  - New `Assets/Scripts/Combat/Enemies/Spawn/PooledEnemy.cs` (`[DefaultExecutionOrder(200)]` so Awake captures `health.maxHealth` *after* `EnemyBrainBase.Awake` writes `data.maxHealth`). Listens to its own `Health.onDeath`, cancels Health's auto-disable, schedules pool return after a 1.5s grace window so loot drops + glory-kill detector still complete. `PrepareForReuse()` is called by the pool before `SetActive(true)` and restores Health (`maxHealth = baselineMaxHealth`, `currentHealth = maxHealth`), `EnemyStagger`, `EnemyLootTable`, dissolve, and outline state; `FinishReuseAfterEnable()` runs post-enable and resets/warps the `NavMeshAgent` to the new spawn point.
  - New `Assets/Scripts/Combat/Enemies/Projectiles/EnemyProjectilePool.cs` — same pattern for Spitter plasma shots. `EnemyProjectile` got `BindPool` + `ResetForPool` (clears owner/damage/direction, caches `TrailRenderer[]` and `Clear()`s each on rent), and `Destroy(gameObject)` → `pool.Return(...)` with a fallback to Destroy when no pool is bound.
  - `RangedEnemyBrain.SpawnProjectile` rents through `EnemyProjectilePool.Instance.Rent` instead of `Instantiate`.
  - `Health.ResetForPool()` (restores `currentHealth`, re-fires `onHealthChanged`) + `Health.CancelAutoDisable()` (cancels the pending 1s `SetActive(false)` when a pool owns the lifecycle).
  - `EnemyStagger.ResetForPool()` — drops `IsStaggered`, blacks out the cached material emission, fires `OnStaggerChanged(false)` so the brain leaves `Staggered`.
  - `EnemyLootTable.ResetForPool()` — clears the one-shot `rolled` guard so each death rolls loot.
  - `GameManager.SpawnEnemy` + `SpawnEncounterEnemy` switched from `Instantiate` to `EnemyPool.Instance.Rent`. `Health.onDeath` listener uses **Remove+Add** so a recycled instance does not accumulate duplicate `OnEnemyDied` / `OnEncounterEnemyDied` subscriptions across rents (TZ §10 acceptance: kills count exactly once).
  - `EnemyBrainBase` — fair-spawn init moved out of `Start` (which only runs once per instance lifetime) into a per-`OnEnable` first-frame check in `Update`. `OnEnable` resets `spawnInitialized = false` and re-enters `Spawn`; `Update` waits one tick for `Target` to be resolved before invoking `InitializeSpawn()`. Closes the "pooled rent skips fair-spawn" gap.
  - `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) — verified 2026-04-29 via temp-injected csproj using worktree absolute paths.
  - **Pending in Unity Editor (next step):** long run (5-arena loop ×2) — verify (a) no disabled enemy/projectile GameObjects accumulate in the Hierarchy outside the `EnemyPool` / `EnemyProjectilePool` containers, (b) kill counts match actual deaths (no double-counting from listener accumulation), (c) HP orbs drop on every kill, (d) recycled enemies start at full HP with no stuck red stagger pulse, (e) fair-spawn delay still triggers on close spawns of recycled enemies, (f) projectile trails start cleanly from the muzzle and do not jump from the previous shot's last position.

## Previous Status (2026-04-28)

- **Phase 3 PR 3.D — Spawn Composition — VERIFIED 2026-04-28 by user.** Role mix by `arenaIndex` playtested OK; no fallback warnings during normal play.
- **Phase 3 PR 3.E — Combat Readability + Active Attack Slots — code landed 2026-04-28, verified 2026-04-29.**
  - New `Assets/Scripts/Combat/Enemies/AI/AttackSlotKind.cs` (Melee/Ranged/Heavy/Special enum).
  - New `Assets/Scripts/Combat/Enemies/AI/ActiveAttackSlotManager.cs` — auto-creating scene-singleton, caps 3/3/1/1 (TZ §8.3). Idempotent `TryAcquire`, multi-set `Release` (caller doesn't need to remember which kind).
  - New `Assets/Scripts/Combat/Enemies/AI/TelegraphFlash.cs` — emissive pulse via MaterialPropertyBlock on every child Renderer; auto-attached by `EnemyBrainBase.Awake` so existing prefabs (Drone/Crawler/Spitter/Brute) get telegraph visuals without any Editor work. Reads each renderer's base `_EmissionColor` at Awake and adds the pulse color on top, then restores to base on EndPulse.
  - New `Assets/Scripts/Combat/Enemies/AI/BruteSlamDecal.cs` — runtime-built flat Cylinder primitive sized to `slamRadius × 2 × 0.025`, transparent URP Lit emissive orange, alpha + emission ramp toward impact frame. Brute `Show(duration)` on Telegraph, `Hide()` on Attack/Recover/Stagger/Death.
  - `EnemyData` gained `telegraphColor` (HDR), `fairSpawnDistance`, `fairSpawnDelay`. Defaults 5m / 0.6s; set 0 to disable. SOs ship with the default warm-orange telegraph color until per-enemy tuning lands.
  - `EnemyBrainBase` integration — cached `TelegraphFlash` + `SlotKind`; new helpers `TryAcquireAttackSlot` / `ReleaseAttackSlot` / `BeginTelegraphFlash` / `EndTelegraphFlash`; fair-spawn delay in `Start` (hold in `Spawn` until `Time.time >= spawnHoldUntil`, telegraph pulses during the hold); `OnDisable` / `HandleStaggerChanged` / `HandleDeath` release the slot + end the pulse so nothing leaks.
  - `MeleeEnemyBrain` / `RangedEnemyBrain` / `BruteEnemyBrain` gate Move → Telegraph behind `TryAcquireAttackSlot()`; if denied, brain keeps moving/repositioning until a slot frees up (no animation interrupted). `BeginTelegraphFlash()` on Telegraph entry, `EndTelegraphFlash()` on Attack frame, `ReleaseAttackSlot()` on Recover → Move. Brute additionally drives `BruteSlamDecal.Show/Hide` at the matching transitions and overrides stagger/death to hide the decal.
  - `dotnet build Assembly-CSharp.csproj` clean (0 errors, 0 warnings) — verified via temp-injected csproj using worktree absolute paths.
  - **Pending in Unity Editor (next step):** spawn a heavy encounter (Combat arena 2+, ≥ 8 Drones + 1 Brute via the `SpawnProfile_Combat` budget), confirm only 3 melee + 1 heavy resolve attacks at once, confirm Brute slam decal is visible during the 0.9s wind-up, confirm a player-adjacent spawn holds ~0.6s with a flash before chasing. HP-orb / stagger / glory-kill / barrier-open contracts must remain intact.

## Previous Status (2026-04-27)

- **Phase 3 PR 3.A — Enemy state-machine base — VERIFIED 2026-04-27 by user.** Drone + Crawler SOs + prefab variants, playtest passed.
- **Phase 3 PR 3.B — Plasma Spitter / Sentinel — VERIFIED 2026-04-27 by user.** Spitter SO + projectile prefab + Enemy_Spitter variant, LoS/dodge/wall-block confirmed.
- **Phase 3 PR 3.C — Station Brute — VERIFIED 2026-04-27 by user.** Brute SO + Enemy_Brute variant, slam telegraph + escape window playtested. Slam telegraph **visual** delivered in PR 3.E (`BruteSlamDecal`).
- **Phase 3 PR 3.D — Spawn Composition — code + Unity asset wiring landed 2026-04-27, verified 2026-04-28.**
  - New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnEntry.cs` — Serializable row with `Resolved*` accessors implementing the override rule (TZ §7.2 Revision: entry value > 0 overrides `EnemyData`, 0 inherits).
  - New `Assets/Scripts/Combat/Enemies/Data/EnemySpawnProfile.cs` — SO holding entry roster, budget curve (`baseBudget` + `budgetPerArenaIndex`), variety cap, and `maxTanks` / `maxRanged` guards (TZ §7.6).
  - New `Assets/Scripts/Combat/Enemies/Spawn/EnemySpawnComposer.cs` — static pure-logic utility. Filters eligible entries by arenaIndex; picks `targetVariety` distinct roles (1/2/3 by arena index, capped by profile); spends budget weighted-random with maxAlive / role caps. Optional `System.Random` for seeded runs. **Every fallback path emits `Debug.LogWarning` with a specific reason** — no silent fallbacks (TZ §7.4 Revision).
  - New `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, …)` overload; `SpawnEncounterEnemy` pulls per-enemy prefab from `encounterRoster[enemiesSpawned]`. Legacy `BeginEncounter(int count, …)` preserved unchanged for the wave loop and for null-profile fallback.
  - `EncounterController` got `arenaIndex` and `spawnProfile` fields. `arenaIndex` is the single source of truth (TZ §7.4 Revision). `SpawnEnemiesViaGameManager` runs the composer when profile != null and passes the resolved roster; otherwise falls through to legacy count-based path.
  - `ArenaTypeProfile.spawnProfile` field added — Combat / Elite / Boss profiles can each carry their own composition.
  - `ArenaFlowController.SetupEncounter` propagates `node.arenaIndex` and `profile.spawnProfile` into the runtime `EncounterController`.
  - `Assets/EnemyData/SpawnProfile_Combat.asset` authored with Drone / Crawler / Spitter / Brute entries, and wired into `Assets/ArenaProfiles/Arena_Combat_M.asset` + `Assets/ArenaProfiles/Arena_Elite_L.asset`.
  - `GameManager.BeginEncounter(IList<EnemySpawnEntry> roster, …)` now ends any already-active encounter before preserving the new roster, so an overlap restart cannot clear the composition list.
  - `dotnet build Assembly-CSharp.csproj` clean.
  - **Pending in Unity Editor (next step):** playtest a generated run with the profile active. Confirm mixed roles appear by arenaIndex, no `[EnemySpawnComposer] Fallback` warnings appear in normal play, Brute stays capped at one, and HP-orb / stagger / glory-kill / barrier-open contracts still work.
- **Phase 3 PR 3.C — Station Brute — code landed 2026-04-27, Editor wiring pending.**
  - New `Assets/Scripts/Combat/Enemies/AI/BruteEnemyBrain.cs` extends `EnemyBrainBase`. State flow: Move → Telegraph → Attack (area slam) → Recover. Telegraph faces target via slow `Quaternion.Slerp` (rate 6/sec — half of Spitter's, so a strafing player can break the lock during a 0.9s wind-up).
  - Slam damage applied ONCE at end of `telegraphTime` via `Physics.OverlapSphereNonAlloc` (static 32-collider buffer, no per-frame GC) at `transform.TransformPoint(slamOriginOffset)` sampled at the impact frame, **not** at telegraph start. Player escapes by leaving `slamRadius` during the wind-up.
  - Multi-collider dedup: `HashSet<Health>` so the player's CharacterController + child weapon volumes don't double-damage on one slam.
  - `slamRadius` and `slamHitMask` come from `EnemyData` fields added in PR 3.A — no schema changes.
  - `OnDrawGizmosSelected` draws a wire-sphere slam preview in Scene view (orange during Telegraph, red baseline). Useful when tuning the SO.
  - Until PR 3.E lands the active-attack slot manager, `EnemyData.maxAlive = 1` for Brute is mandatory (TZ Revision Log v2) — enforced via the SO field in Editor, not via code.
  - `dotnet build Assembly-CSharp.csproj` clean.
  - **Pending in Unity Editor (next step):** create `Brute.asset` SO + `Enemy_Brute.prefab` variant, then playtest.
- **Phase 3 PR 3.B — Plasma Spitter / Sentinel — code landed 2026-04-27, Editor wiring pending.**
  - New `Assets/Scripts/Combat/Enemies/AI/RangedEnemyBrain.cs` extends `EnemyBrainBase`. State flow: Move (close in if dist > pref+band) → Reposition (back off if dist < pref-band) → Telegraph (face target, wait `telegraphTime`) → Attack (re-check LoS, spawn projectile) → Recover → Move. Hysteresis `distanceBand` (default 1.5m) prevents jitter at the band edge.
  - LoS check: `Physics.Raycast` from `muzzleOffset` (default `Vector3.up * 1.0`) toward `target.position + Vector3.up`. Throttled by `EnemyData.lineOfSightCheckInterval`, result cached between checks. Refuses Telegraph entry without clear LoS, AND re-checks at Attack frame so dashing-behind-cover during wind-up cancels the shot (TZ §6.3) while still consuming the cooldown.
  - New `Assets/Scripts/Combat/Enemies/Projectiles/EnemyProjectile.cs`. Owner-filtered: trigger `SphereCollider`, manual `transform.position` translation (avoids non-kinematic Rigidbody surprises). Passes through self and any other enemy (`EnemyBrainBase` or legacy `SimpleEnemyAI` in parent chain). Damages the first non-owner `Health` via `Health.TakeDamage`. Self-destructs on damage hit OR world hit OR `lifetime` timeout (default 4s). No pooling — that's PR 3.F.
  - `dotnet build Assembly-CSharp.csproj` clean (zero CS errors/warnings) after temp-injecting new files into csproj for verification.
  - **Pending in Unity Editor (next step, see `Recommended Next Task`):** create `EnemyProjectile.prefab`, `Spitter.asset` SO, `Enemy_Spitter.prefab` variant, then playtest LoS / dodgeability.
- **Phase 3 PR 3.A — Enemy state-machine base — code landed 2026-04-27, Editor wiring pending.**
  - New module `Assets/Scripts/Combat/Enemies/AI/`: `EnemyRole` (Fodder/Chaser/Ranged/Tank/Zoner/Boss-reserved), `EnemyAIState` (Spawn/Move/Telegraph/Attack/Recover/Reposition/Staggered/Dead), `IEnemyTargetReceiver` (`SetTarget(Transform)`), `EnemyBrainBase` abstract MonoBehaviour, `MeleeEnemyBrain` concrete brain.
  - New `Assets/Scripts/Combat/Enemies/Data/EnemyData.cs` ScriptableObject — single SO type for all enemies; ranged/heavy fields stay default-zero on melee enemies. `[CreateAssetMenu]` path: *Create > Void Survivor > Enemies > Enemy Data*.
  - `EnemyBrainBase`: `[RequireComponent(NavMeshAgent, Health)]`, throttled `RequestPathTo` (0.2s default `pathUpdateInterval`, `agent.isOnNavMesh` guard inherited from PR 2.C), `Health.onDeath` → `EnemyAIState.Dead` listener, `EnemyStagger.OnStaggerChanged` → `EnemyAIState.Staggered` listener (aborts in-progress attack, calls `agent.ResetPath`).
  - `MeleeEnemyBrain`: Move → Telegraph → Attack → Recover loop. Damage applies once at end of `telegraphTime` with a fresh range check, so a target moving out during wind-up causes the swing to whiff (TZ §5.6 contract). Drone and Crawler will share this class and differ only by their `EnemyData` SO.
  - `Assets/test/SimpleEnemyAI.cs` is now a Phase 3 compatibility wrapper: implements `IEnemyTargetReceiver`, `SetDestination` throttled to `pathUpdateInterval` (default 0.2s — fixes "no SetDestination every frame" acceptance), per-attack `Debug.Log` removed. Behavior on `Enemy.prefab` unchanged otherwise; the prefab keeps working until it is migrated to `MeleeEnemyBrain` in Editor.
  - `Assets/test/GameManager.cs` `SpawnEnemy` and `SpawnEncounterEnemy` resolve `IEnemyTargetReceiver` via `GetComponent` — no `SimpleEnemyAI` fallback branch (TZ §5.4 simplified migration).
  - `dotnet build Assembly-CSharp.csproj` ran clean (zero CS errors/warnings) after temporarily injecting the new files into the csproj for verification; Unity will regenerate the csproj on next Editor refresh and pick the files up automatically.
  - **Pending in Unity Editor (next step):** (1) create `Drone.asset` and `Crawler.asset` `EnemyData` SOs via the *Create > Void Survivor > Enemies > Enemy Data* menu — fill in role/stats/attack/spawn fields per ENEMY_AI_TZ §6.1/§6.2 (but verify Crawler/Drone speeds against `PlayerController.moveSpeed = 10` before locking values; spec defaults of 3.6/4.8 are unkiteable-by-walking but also un-catch-uppable since player walk = 10); (2) either build `Enemy_Drone.prefab` / `Enemy_Crawler.prefab` variants of `Enemy.prefab` with `MeleeEnemyBrain` + the SOs, or swap the component on `Enemy.prefab` itself; (3) playtest legacy wave + encounter modes; HP orb / stagger / glory-kill / `OnEnemyKilled` should all still fire because `Health` and `GameManager` event contracts were not touched.

## Previous Status (2026-04-26)

- **Phase 1 is COMPLETE.** Movement upgrades, Weapon System (PR A + PR B), Kill-to-Survive (PR A + PR B) shipped and playtested.
- **Phase 2 procedural arena pipeline r4 is complete (PR 2.A–2.E).** PR 2.E was closed 2026-04-24 after user's in-Editor playtest.
- **Phase 2 PR 2.F Visual Fidelity Pass — verified 2026-04-25 by user; "looks better than before, but platforms still flat slabs and some textures stretch".**
- **Phase 2 PR 2.G Anti-stretch + lighting fill — code landed 2026-04-25, Unity verify pending.**
  - `WorldUVScaler` MonoBehaviour + `WorldUVDensityRegistry` (`Assets/Scripts/ProceduralArena/Build/WorldUVScaler.cs`): per-instance MaterialPropertyBlock derives `_BaseMap_ST` (and `_BumpMap_ST` / occlusion / metallic / emission ST) from each box' `lossyScale`. Dominant face = smallest axis = surface normal; UV tile width/height = world width/height of the dominant face × tilesPerMeter from the registry. Kills "Roblox" stretch on big floors and walls without changing meshes.
  - `BuildUtils.SpawnBox` now auto-attaches `WorldUVScaler` to every spawned cube.
  - `ArenaBuildMaterials.CreateSurface` registers per-material density (`slot.textureScale × 0.25` → tiles per meter), enables `enableInstancing = true`, and stops baking `textureScale` into the material's `_BaseMap_ST` (per-instance MPB now drives tiling).
  - New `ArenaBuilder.BuildSingleEdgeStrips` — thin emissive strips along floor-to-wall seams (skips door cells) for "panel-light" feel.
  - New `ArenaBuilder.BuildSingleFillLights` — center + four quadrant **Spot** lights pointing straight down (110° outer / ~60° inner) at `wh-0.45` height; intensity = `max(2.5, biome.accentLightIntensity × 1.6)`, range = `wh + 6m`. Quadrant lights only on arenas ≥ 6×6 cells; small Start/Shop/Rest arenas get just the center fill. Spots replace earlier Point fill lights — points lose ~95% to inverse-square falloff before reaching player height, spots focus the cone where it matters.
  - New `ArenaBuilder.SpawnCeilingLamp` — co-spawned at every fill-light position. Visible fixture: dark mounting bracket (uses `mats.ceiling`/`mats.wall`) flush to the ceiling + bright emissive panel (`mats.lampPanel`, 2.2 m square) hanging 8 cm below. Mounted 4 cm below the ceiling tile to avoid z-fight. New `mats.lampPanel` is intentionally biome-agnostic warm-white at intensity 4.5 so panels read as actually lit on every biome.
  - `PC_RPAsset.asset`: `m_AdditionalLightsPerObjectLimit` 4→8 (so fill + exit + pylon point lights coexist on one renderer), `m_ShadowDistance` 50→60.
- **Phase 2 PR 2.F Visual Fidelity Pass — closed 2026-04-25 after user playtest.**
  - Camera `m_RenderPostProcessing` enabled in `Assets/test.unity` + SMAA on (Antialiasing=2); post-processing is now actually rendered.
  - `PC_Renderer.asset` SSAO intensity raised 0.4 → 0.85, DirectLightingStrength 0.25 → 0.35, Radius 0.3 → 0.35.
  - `BiomeSurfaceDefinition` got `bumpScale`, `parallaxStrength`, `detailAlbedoResourcePath`, `detailTextureScale`, `detailStrength` per-slot fields. `ArenaBuildMaterials.ApplyTextureSet` wires them into URP Lit (`_BumpScale`, `_Parallax`, `_DetailAlbedoMap`, `_DETAIL_MULX2` keyword).
  - `BiomeDefinition` got `BiomePostProcessing` block (bloom / colorFilter / exposure / contrast / saturation / vignette) + `accentLightIntensity` / `accentLightRange` / `exitLightColor` for runtime point lights.
  - New `ArenaPostProcessingController` (auto-added to `ArenaFlowController`) spawns a runtime Global Volume with Bloom + ColorAdjustments + Vignette + ACES Tonemapping, priority 100. Biome tint now rides through `ColorAdjustments.colorFilter` instead of abusing `RenderSettings.ambient*`.
  - `ArenaFlowController.SpawnReflectionProbe` adds a realtime box-projected reflection probe at arena center after each build (one-shot `RenderProbe`), so metal reacts.
  - `ArenaBuilder.BuildSingleExits` + `SpawnAtmospherePylon` now attach actual URP Point Lights on exit markers and atmosphere pylons, color/intensity/range driven by biome.
  - Fog bug fixed: `ApplyBiomeAtmosphere` now uses `fogStrength` as the Lerp t (previously clamped to ≥0.92, so any biome with fogStrength=0 still got 92% of biome.fogColor).
  - Ambient tint nudge softened (0.35/0.25/0.30) because the heavy color work now lives in post-processing.
- **Phase 2 PR 2.E closed 2026-04-24** after user playtest.
  - `BiomeDefinition` uses material-slot-driven biome data.
  - Approved PR 2.E textures plus companion maps were copied under `Assets/Resources/ProceduralArena/Biomes/`.
  - `ArenaBuildMaterials` now resolves full companion texture sets with Resources fallback.
  - `Assets/Editor/ProceduralArena/ProceduralArenaTextureImportUtility.cs` now auto-reimports PR 2.E textures with readable/normal/linear import rules.
  - `ArenaBuilder.BuildSingle` now splits collidable architecture from non-solid overlays/decor and tones down center accents / contamination.
  - `ArenaFlowController` now applies a stronger biome fog and ambient tint pass during arena transitions.
  - `ArenaDebugGizmos -> r4 / Generate + Build Single Arena` now uses biome-aware materials too.
  - Follow-up tuning on 2026-04-24 reduced over-bright cyan in `Biome_VoidStation`, temporarily disabled `Parkour` selection in run generation, and added hard safeguards so `Start` / `Shop` / `Rest` arenas stay flat with reduced edge decor.
  - `dotnet build Assembly-CSharp.csproj` passed on 2026-04-24 after the follow-up fixes.
  - User Unity Editor test reported the PR 2.E result is "more or less normal", so the milestone is accepted for now.
- Stable high-level knowledge base: [PROJECT_KNOWLEDGE_BASE.md](C:/Users/assam/DiplomGame/docs/PROJECT_KNOWLEDGE_BASE.md)
- Roadmap: [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md)
- TZ: [ARENA_GENERATION_TZ.md](C:/Users/assam/DiplomGame/docs/ARENA_GENERATION_TZ.md) - APPROVED r4.

### Cancelled Design Note: Arena Complex / Connected Arena Rooms

Captured 2026-04-26 from the user's sketch, then cancelled 2026-04-30. Do not implement connected multi-room arenas in the current roadmap. Keep the existing single-arena run pipeline as the active architecture.

---

## Current Goal

**Phase 4 PR 4.PF — Unity Editor playtest.** Shop Room code compiles, but needs runtime validation in `test.unity` before PR 4.PG Rest starts.

1. `GameManager.useEncounterMode = true`. Start a fresh run (Ctrl+P) and reach a Shop door (stage 4 / 7 / 8).
2. Enter Shop. Expected: safe room, no enemies, no payout panel, no reward cards, visible Shop platform/kiosk appears near room center with soft glow and upward particles.
3. Step onto the Shop platform. Expected: Shop UI appears with current KP and 3 offers: one heal and two upgrades. Buttons for unaffordable offers are disabled / marked `NOT ENOUGH KP`.
4. Buy an affordable heal: KP decreases, player HP heals by the listed fraction, offer becomes `SOLD`.
5. Buy an affordable upgrade: KP decreases, `UpgradeSystem` stack applies, offer becomes `SOLD`.
6. Press `R` / reroll: reroll spends 8 KP first, then 14, then 22; inventory changes and remains deterministic for the same seed + reroll count.
7. Press `Esc` / Close: UI closes, player movement and cursor lock restore, held fire is cleared.
8. Stay on the platform: UI should not instantly reopen. Step off and back onto the platform: UI opens again.
9. Walk to an exit. Expected: exits are already open because Shop keeps `ClearCondition.None`; leaving the room works normally.

If anything regresses, likely suspects:
- Shop platform does not appear -> check `ArenaBuilder.BuildSingleShopTerminal`;
- Shop UI does not open -> check `RunController.OnArenaBuilt` category routing to `ShopController.PrepareForArena` and `ShopTerminalTrigger.OnTriggerEnter`;
- purchases do nothing -> check `KillPointsWallet.TrySpend`, `UpgradeSystem.AddUpgrade`, and `GameManager.playerHealth`;
- reroll not deterministic -> check seed derivation in `ShopController.RegenerateOffers`;
- player remains frozen -> check `ShopController.CloseShop` / `ShopCanvas.RequestClose`.
- glow/particles are too strong or missing -> tune `ShopPlatform_SoftGlow`, `ShopPlatform_GlowLine_X/Z`, `ShopPlatform_Light`, `CreateShopGlowMaterial`, and `SpawnShopPlatformParticles` in `ArenaBuilder.BuildSingleShopTerminal`.

### Earlier Phase 3 plan (kept for reference)

**Phase 3 PR 3.E — Editor playtest.** Verify combat readability and slot-cap behavior in `test.unity`:

1. `GameManager.useEncounterMode = true`. Start a fresh run (Ctrl+P).
2. Walk into Arena 2+ (Combat or Elite). Budget should resolve to a mix with 1 Brute + several Drones/Crawlers/Spitter.
3. **Slot caps:** observe many enemies alive but only ≤3 in Telegraph at the same time across melee, ≤3 across ranged, ≤1 Brute slamming. Other enemies should keep moving/repositioning while waiting for a free slot — no frozen poses.
4. **Telegraph flash:** Drone/Crawler/Spitter should pulse emissive orange (or whatever color their `EnemyData.telegraphColor` is) during the wind-up, reset to baseline on impact.
5. **Brute slam decal:** flat orange disc on the floor, sized to `slamRadius`, alpha + emission ramping during the 0.9s wind-up, hidden on Attack/Recover and on stagger/death.
6. **Fair-spawn delay:** if any enemy spawns within 5m of the player, it should hold in place with a brief flash for ~0.6s before chasing. Tune `EnemyData.fairSpawnDistance` / `fairSpawnDelay` per enemy if it feels too short or too long.
7. **Contracts intact:** HP orbs drop on kill, stagger pulses at low HP, glory-kill works, soft-lock barriers open after clear. No `SetDestination on inactive agent` errors. No `[EnemySpawnComposer] Fallback` warnings during normal play.

If anything regresses, the most likely suspects are:
- `ActiveAttackSlotManager` not being created → check Hierarchy for the auto-spawned `ActiveAttackSlotManager` GameObject when the first enemy attempts to acquire a slot.
- Telegraph flash too dim → raise `EnemyData.telegraphColor` HDR intensity (its alpha-multiplied HDR value) per enemy.
- Brute slam decal invisible on a specific biome floor → check that the runtime URP Lit transparent shader exists on this platform; fall back to bumping `_EmissionColor` HDR scale in `BruteSlamDecal.Update`.

After playtest passes, **PR 3.F (Enemy + Projectile pooling)** is the last Phase 3 PR before Phase 4 boss prep.

### Earlier Phase 3 plan (kept for reference)

**Phase 3 — Enemy AI.** Decided 2026-04-26: PR 2.H (beveled prefabs) and the hand-authored structures idea are deferred until at least the first Phase 3 enemy-AI pass lands. Visual coupling between Phase 2.H and Phase 3 is minimal (NavMesh re-bakes per arena, `combatSpawnPoints` are independent, glory-kill / stagger / pooling are orthogonal), so deferring is safe. Only real follow-up cost: a balance pass on ranged enemies once new cover structures change line-of-sight density — that's normal Phase 6 work.

First Phase 3 task per [PROGRESS.md](C:/Users/assam/DiplomGame/docs/PROGRESS.md) §"Phase 3: Enemy AI": **refactor `Assets/test/SimpleEnemyAI.cs` into a state-machine base** (issue #5), then build out the four-role core (Drone, Crawler, Plasma Spitter/Sentinel, Station Brute) on top of it. AI improvements + spawning composition come after. Gravity Node is optional after the core is stable; Void Warden is not part of the near-term Phase 3 scope.

Phase 3 master spec now lives in [ENEMY_AI_TZ.md](C:/Users/assam/DiplomGame/docs/ENEMY_AI_TZ.md) (created 2026-04-27). It supersedes broad brainstorming for the near-term implementation scope: four main enemy roles, simple state-machine AI, spawn composition by budget/weights/arenaIndex, readable telegraphs/attack slots, and pooling only after enemy contracts stabilize. Full AI Director, Arena Complex spawn logic, Shield Drone, complex Brute charge, and final art/VFX are deferred.

### Deferred Phase 2 work (resume after first Phase 3 enemy-AI pass)

- **PR 2.H — Beveled prefabs.** Replace `GameObject.CreatePrimitive(Cube)` slabs with proper meshes that have chamfered edges. Platforms (`mats.platform` in `BuildSingleVerticality`) are priority #1 — they're the worst-looking element per user screenshot 2026-04-25. Open question when resuming: Asset Store modular sci-fi pack (Kenney/Synty, CC0/cheap, fast, may fight the runtime PBR pipeline from PR 2.E) vs. Blender custom meshes (full control, diploma-friendly, learning curve since user is first-time in Blender).
- **PR 2.H1 — Hand-authored structures (idea captured 2026-04-26).** Pre-made structure variants (bunkers, sandbag lines, pillar clusters, broken arches, sniper nests; atmospheric: crashed pods, generator stacks, terminals, dead drone heaps) spawned into existing cover/decor slots via `ArenaCoverPlanner` budget. Implementation sketch: `StructureDefinition` SO with `List<BoxPart>` (offset, size, materialSlot) — stays in code, reuses `BuildUtils.SpawnBox` so `WorldUVScaler` + per-biome materials work for free, no Blender / no Asset Store, fully deterministic via a new `structureRng` sub-stream in `SingleArenaGenerator`. Bias: 1–2 structures on M arenas, 2–3 on L; biome / arena-category gates which set is eligible. This may close ~60% of PR 2.H value (silhouette readability) without bevels — re-evaluate scope of PR 2.H once H1 lands.
- **Arena Complex / Connected Arena Rooms** was cancelled 2026-04-30; do not revisit unless the user explicitly reopens it.

### What's still open from Phase 2

- PR 2.G visual verify is treated as informally accepted (user moved on to Phase 3 planning); if regressions surface during Phase 3 playtests, re-open against `WorldUVScaler` / fill-light tuning.
- `Parkour` profile is still excluded from `DefaultRunConfig` — keep excluded until PR 2.D ramp-axis review (see "What Is Not Done Yet" below).

- PR 2.A - SingleArenaGenerator + shape / cover / exit planners + size presets + per-arena 10-25m ceiling - **DONE (verified 2026-04-21)**.
- PR 2.B - Run Graph + transitions + fade + door-choice + Victory/GameOver - **DONE (verified 2026-04-21)**.
- PR 2.C - Async `NavMeshSurface.UpdateNavMesh`, encounter integration, soft-lock barriers, clear conditions, `SimpleEnemyAI.isOnNavMesh` guard - **DONE (verified 2026-04-22)**.
- PR 2.D - Verticality + biome selection + new arena type profiles + `arenaIndex` scaling + runtime debug overlay - **DONE (verified 2026-04-22)**.
- PR 2.E - second pass adds companion PBR maps, texture import automation, collidable architecture/props, quieter Alien Nexus composition, stronger fog/emissive readability, Parkour disable guard, and flat-arena safeguards - **DONE (verified 2026-04-24 by user Unity Editor test)**.

Scene wiring reference (`test.unity`):
- `Run` GameObject with `RunController` (config -> `DefaultRunConfig`, flow -> `ArenaHost`).
- `Run/ArenaHost` GameObject with `ArenaFlowController` (buildConfig -> `TestArenaConfig`).
- `GameManager` - set `useEncounterMode = true` for PR 2.C/2.E flow (legacy wave loop still works when `false`).
- Legacy `ArenaDebug` GameObject - keep disabled while `Run` is active, otherwise two arenas overlap.
- Player must have tag `Player`. CharacterController teleports via `cc.enabled = false/true`.

---

## What Is Already Done

**Phase 1:**
- Movement is tuned and playtested.
- Weapon system is modular and shipped under `Assets/Scripts/Combat/Weapons/`.
- Kill-to-Survive systems are in place under `Assets/Scripts/Combat/{Pickups,Enemies,Player}/`.

**Phase 2 (r4):**
- `Assets/Scripts/ProceduralArena/Arena/` - single-arena generation, shapes, cover, exits, verticality, biome metadata.
- `Assets/Scripts/ProceduralArena/Run/` - run graph, transitions, door choice, runtime debug overlay.
- `Assets/Scripts/ProceduralArena/Navigation/` - async NavMesh bake.
- `Assets/Scripts/ProceduralArena/Encounter/` - encounter orchestration and soft-lock barriers.
- `Assets/Scripts/ProceduralArena/Build/` - single-arena build path plus PR 2.E visual layers, companion PBR map resolver, and architecture collision split.
- `Assets/Editor/ProceduralArena/` - PR 2.E texture import helper for readable normal/mask maps.
- `Assets/Resources/ProceduralArena/Biomes/` - approved PR 2.E texture copies plus companion maps for runtime biome fallback.
- `Assets/ArenaProfiles/` - Start/Combat/Boss/Elite/Parkour/Shop/Rest profiles plus `Biome_VoidStation` and `Biome_AlienNexus`.
- `Assets/ArenaProfiles/DefaultRunConfig.asset` - `Parkour` removed from active run pools for now.

---

## What Is Not Done Yet

- `Parkour` is intentionally disabled in generated runs for now; if it appears again, check `DefaultRunConfig.asset` and `RunGraphGenerator.IsSelectableProfile(...)`.
- PR 2.D/2.E review notes to keep in mind before re-enabling Parkour or extending verticality:
  - east/west ramp geometry should be rechecked because ramp pitch currently assumes the north/south local axis.
  - biome atmosphere mutates global `RenderSettings`; restore `fogMode` too if scenes start sharing multiple visual controllers.
- If bright cyan `VoidStation` blocks/trim reappear, verify that Unity reimported the updated `Biome_VoidStation.asset` and the copied textures; the intended follow-up uses neutral `Panel_007` for `floorAccent`, `wallTrim`, and `propMaterial`, with much weaker emissive accents.
- `test.unity` is still not in Build Settings (issue #3).
- Enemy pooling (issue #6), projectile pooling (issue #7), and `SimpleEnemyAI` refactor (issue #5) remain deferred beyond this milestone.
- Arena Complex / Connected Arena Rooms is cancelled as of 2026-04-30. Do not implement unless the user explicitly reopens the idea.

---

## Recommended Next Task

Next is a focused **Phase 4 PR 4.PF Shop Room Editor playtest**. Use the *Current Goal* checklist above and verify shop platform triggering, purchases, reroll pricing, cursor/movement restore, reopen behavior, and exit behavior.

After the PR 4.PF playtest is accepted, continue with **PR 4.PG — Rest Room + Final Prep**: one-choice rest UI, heal/max-HP/reward-boost options, and final-prep node behavior before Boss.

### Earlier-PR notes (kept for context — PR 3.D playtest steps)

**A. Playtest PR 3.D composition.**
1. `GameManager.useEncounterMode = true`. Start a fresh run (`Ctrl+P`).
2. Walk into the first Combat arena (arenaIndex 1 typically — Start is 0).
   - Expected: roster mix of Drone + Crawler + (≥arenaIndex 1) Spitter. Budget = 8 + 1×3 = 11.
   - With Drone cost 1, Crawler 2, Spitter 3, the composer should produce ~5-7 enemies of mixed types.
3. Arena 2+: Brute can appear (cost 6, max 1). With budget = 8 + 2×3 = 14 you should occasionally see 1 Brute + 4 fodder.
4. Verify in Console:
   - **No `[EnemySpawnComposer] Fallback`** warnings during normal play (a warning means a profile is misconfigured — null roster, all entries gated out, etc.).
   - **No** errors. HP-orb / stagger / glory-kill still fire.
5. Try a misconfiguration on purpose: temporarily clear all entries on `SpawnProfile_Combat`. Save. Reload run. Console should print:
   `[EnemySpawnComposer] Fallback to caller's default — spawn profile is null or has no entries.` — then the legacy `GameManager.enemyPrefab` spawns. Confirms the warning path works. Restore the entries.

**B. Optional tuning after the playtest.**
- If Combat and Elite feel too similar, clone `SpawnProfile_Combat.asset` into a separate Elite profile and tune weights / `maxRanged` there.
- Keep `Arena_Boss_L.asset` on the legacy path for now; boss composition belongs with the later boss enemy task.

**C. Then PR 3.E (Combat Readability + Active Attack Slots).**
- `ActiveAttackSlotManager` so heavy/melee/ranged slots aren't all resolving at once.
- **Brute slam telegraph visual** (deferred from PR 3.C — ground decal/circle while telegraphing).
- General telegraph layer for all enemies (placeholder emissive flash on Crawler / Spitter wind-ups).
- Fair-spawn delay for close spawns.

---

### Earlier-PR notes (kept for context)

PR 3.C Editor steps (now done — Brute):

**A. Author the Brute `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Brute`. Fields per TZ §6.4:

**A. Author the Brute `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Brute`. Fields per TZ §6.4:
- Identity: enemyId `brute`, displayName `Station Brute`, role `Tank`.
- Stats: maxHealth 260, moveSpeed 2.2, damage 28.
- Attack: attackRange 3.0, attackCooldown 3.0, telegraphTime 0.9, recoveryTime 1.1.
- Ranged: leave zero (Brute has no projectile).
- Heavy / Brute: **slamRadius 4.0**, **slamHitMask = Default + Player layers** (open the LayerMask dropdown, tick the layers that should be hit by the slam — at minimum the player's layer; add any destructible layers later if introduced).
- Spawn: spawnCost 6, minArenaIndex 2, **maxAlive 1** ← do NOT raise this until PR 3.E ships the slot manager. spawnWeight 1.

**B. Build the Brute prefab variant.** Right-click `Assets/Prefabs/Enemy.prefab` → **Create → Prefab Variant** → rename `Enemy_Brute`. Open it. Remove `SimpleEnemyAI`. Add `BruteEnemyBrain`. Drag `Brute.asset` into `data`. Leave `Slam Origin Offset` at (0, 0, 0) — that's the feet for the standard 1×2×1 capsule. Leave `Draw Slam Gizmo` ✓ (Editor-only, no runtime cost). Save.

Optional polish: scale the visual mesh up (Transform Scale 1.4 / 2.0 / 1.4) so the Brute reads as visually heavier than a Drone. Health bar prefab will scale with parent.

**C. Playtest.** Temporarily set `GameManager.enemyPrefab` to `Enemy_Brute.prefab` and run.
- Brute should approach slowly (~2.2 m/s, easy to outwalk).
- At ≤3m it stops, faces you over ~0.9s, then slams.
- If you stay within ~4m of the slam origin at impact, you take 28 damage. Step out → no damage.
- Open the Scene view tab while paused — you should see an orange wire-sphere around the Brute during Telegraph showing the slam radius.
- Health/stagger/glory-kill/HP-orb still work.

**D. Test combination encounters (optional, before PR 3.D).** Set `GameManager.enemyPrefab` back to Drone and manually drop a single Brute into the scene at runtime via Hierarchy duplication, OR temporarily edit `EncounterController` to spawn a mix. Verify the Brute creates target-priority pressure when combined with Drones.

After playtest passes, PR 3.A/B/C are all clear of code work. Next is **PR 3.D — Spawn Composition**: `EnemySpawnEntry`, `EnemySpawnProfile`, `EnemySpawnComposer`, and the `EncounterController` → `GameManager.BeginEncounter` path that drives role mix by `arenaIndex`. After 3.D the per-arena spawn finally produces real role mixes instead of one prefab repeated.

**E. Then PR 3.E (combat readability + active attack slots)** ships the `ActiveAttackSlotManager` so heavy/melee/ranged attacks don't all resolve at once even with many enemies alive.

---

### Earlier-PR notes (kept for context)

PR 3.B Editor steps (now done — Spitter):

**A. Build the projectile prefab.**
1. Project window → right-click in `Assets/Prefabs/` → **Create → 3D Object → Sphere**. Actually simpler: GameObject → 3D Object → Sphere in the Hierarchy (any scene), scale to ~0.4, then drag into `Assets/Prefabs/` to make a prefab; delete the scene instance.
2. Open the new prefab. On the root: set the `SphereCollider` to `Is Trigger = true`, radius ~0.25. Remove the default Mesh Collider if Unity added one.
3. Add Component → `Enemy Projectile` script. `Lifetime` = 4 seconds (default).
4. Optional polish: assign a bright emissive material to the MeshRenderer so the projectile reads as plasma. A `TrailRenderer` child (short, color-matched) helps dodgeability.
5. Rename the prefab `EnemyProjectile.prefab`. Save.

**B. Author the Spitter `EnemyData` SO.** Right-click in `Assets/EnemyData/` → **Create → Void Survivor → Enemies → Enemy Data**. Rename `Spitter`. Fields per TZ §6.3:
- Identity: enemyId `spitter`, displayName `Plasma Spitter`, role `Ranged`.
- Stats: maxHealth 90, moveSpeed 3.0, damage 12.
- Attack: attackRange 18, attackCooldown 2.0, telegraphTime 0.7, recoveryTime 0.5.
- Ranged: drag `EnemyProjectile.prefab` into `projectilePrefab`. projectileSpeed 10. preferredDistance 12. lineOfSightCheckInterval 0.2.
- Heavy / Brute: leave zero.
- Spawn: spawnCost 3, minArenaIndex 1 (TZ §6.3 acceptance), maxAlive 4, spawnWeight 1.

**C. Build the Spitter prefab variant.** Right-click `Assets/Prefabs/Enemy.prefab` → **Create → Prefab Variant** → rename `Enemy_Spitter`. Open it. Remove `SimpleEnemyAI`. Add `RangedEnemyBrain`. Drag `Spitter.asset` into `data`. Leave `Muzzle Offset` at (0, 1, 0). `Distance Band` = 1.5. `LoS Block Mask` — set to **Default** layer (so walls/cover block, but enemy layer 6 is excluded). Save.

**D. Playtest.** Easiest: temporarily set `GameManager.enemyPrefab` to `Enemy_Spitter.prefab` and run.
- Spitter should hold ~12m distance, back off if you rush in, fire visible plasma every ~2s.
- Stand behind a cover pillar — Spitter must NOT shoot through it.
- Strafe perpendicular to the projectile during flight — it should be dodgeable.
- Health, stagger, glory-kill, HP-orb drop should still work (contracts unchanged).

After playtest passes, restore `GameManager.enemyPrefab` to whatever the spawn composer (PR 3.D, future) will use, OR leave it on Drone for now and let PR 3.D drive variety.

**E. Then PR 3.C (Station Brute).** New `BruteEnemyBrain` with `Physics.OverlapSphere` slam at impact frame. `EnemyData` already has `slamRadius`/`slamHitMask` fields ready.

---

PR 3.A Editor steps (now done — Drone/Crawler):

1. **Create EnemyData SOs in Unity Editor.** *Right-click in Project view → Create → Void Survivor → Enemies → Enemy Data* twice. Name them `Drone.asset` and `Crawler.asset` (suggested location: `Assets/EnemyData/`). Tune via ENEMY_AI_TZ §6.1 / §6.2 — but **adjust Drone `moveSpeed`/Crawler `moveSpeed` upward** before locking values, because `PlayerController.moveSpeed = 10`. Suggested override: Drone ≈ 6.5, Crawler ≈ 9.0 (faster than nothing but still slower than dash burst of 25 m/s). Document the deviation in PROGRESS change log when committed.
2. **Build prefab variants or swap component on `Enemy.prefab`.** Easiest: open `Assets/Prefabs/Enemy.prefab`, replace `SimpleEnemyAI` MonoBehaviour with `MeleeEnemyBrain`, drag in the Drone EnemyData asset, save as `Enemy_Drone.prefab`. Repeat with Crawler SO for `Enemy_Crawler.prefab`. Wire `GameManager.enemyPrefab` to `Enemy_Drone.prefab` for now.
3. **Playtest both modes.** Set `useEncounterMode = true`, run through the 5-arena run. Verify (a) enemies spawn on NavMesh, (b) `Health.onDeath` still triggers HP-orb drops + barrier-open, (c) stagger pulses at low HP, (d) glory-kill detector still works, (e) no Console spam, (f) no `SetDestination on inactive agent`. Then flip `useEncounterMode = false` and confirm legacy wave loop also works.
4. **Then PR 3.B (Plasma Spitter).** New `RangedEnemyBrain` + `EnemyProjectile` + LoS + hold-distance. The `EnemyData` SO already has the ranged stub fields (`projectilePrefab`, `projectileSpeed`, `preferredDistance`, `lineOfSightCheckInterval`); no further `EnemyData` schema changes expected.
5. Keep `GameManager` encounter API compatible with the Phase 2 `ArenaFlowController` / `EncounterController` path — already done in PR 3.A via `IEnemyTargetReceiver`, just don't accidentally re-introduce `SimpleEnemyAI`-typed `GetComponent` calls.
6. Re-enable or revisit Parkour only after the ramp-axis review and gameplay readability pass (unrelated to Phase 3, parked).
7. Arena Complex / Connected Arena Rooms is cancelled as of 2026-04-30; do not route spawn-composition work around future multi-room staged clears unless the user explicitly reopens it.

---

## Files Most Relevant For The Next Task

- [Assets/test/GameManager.cs](C:/Users/assam/DiplomGame/Assets/test/GameManager.cs)
- [Assets/test/Health.cs](C:/Users/assam/DiplomGame/Assets/test/Health.cs)
- [Assets/Scripts/Progression/RunProgressionController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/RunProgressionController.cs)
- [Assets/Scripts/Progression/KillPointsWallet.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/KillPointsWallet.cs)
- [Assets/Scripts/Progression/StylePointsTracker.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/StylePointsTracker.cs)
- [Assets/Scripts/Progression/ArenaPayoutCalculator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ArenaPayoutCalculator.cs)
- [Assets/Scripts/Progression/PayoutPanel.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/PayoutPanel.cs)
- [Assets/Scripts/Progression/ShopController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ShopController.cs)
- [Assets/Scripts/Progression/ShopInventoryGenerator.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ShopInventoryGenerator.cs)
- [Assets/Scripts/Progression/ShopOffer.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ShopOffer.cs)
- [Assets/Scripts/Progression/ShopTerminalTrigger.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ShopTerminalTrigger.cs)
- [Assets/Scripts/Progression/ShopCanvas.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Progression/ShopCanvas.cs)
- [Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Build/ArenaBuilder.cs)
- [Assets/Scripts/Combat/Player/HUD/CombatHUDController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Combat/Player/HUD/CombatHUDController.cs)
- [Assets/Scripts/Combat/Player/HUD/KillPointsBlock.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Combat/Player/HUD/KillPointsBlock.cs)
- [Assets/Scripts/Combat/Player/HUD/StyleMeterBlock.cs](C:/Users/assam/DiplomGame/Assets/Scripts/Combat/Player/HUD/StyleMeterBlock.cs)
- [Assets/Scripts/ProceduralArena/Run/RunController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/RunController.cs)
- [Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Encounter/EncounterController.cs)
- [Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs](C:/Users/assam/DiplomGame/Assets/Scripts/ProceduralArena/Run/ArenaFlowController.cs)
- [docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md](C:/Users/assam/DiplomGame/docs/PHASE_4_ROGUELIKE_PROGRESSION_TZ.md)
- [KNOWN_ISSUES.md](C:/Users/assam/DiplomGame/docs/KNOWN_ISSUES.md)

---

## Do Not Break

- Movement feel (walk/jump/double-jump/dash/slide/air-control)
- Weapon framework under `Assets/Scripts/Combat/Weapons/`
- Kill-to-Survive seams (`PlayerStats`, `EnemyLootTable`, `IGloryKillPolicy`)
- `Health` event contracts (`onHealthChanged`, `onDeath`, `onTakeDamage`)
- `GameManager.OnEnemyKilled` event
- `GameManager` legacy wave loop when `useEncounterMode = false`
- Zero `UnityEngine.Random` inside `Assets/Scripts/ProceduralArena/**`
- `[DEPRECATED]` BSP headers and files kept for diploma reference

---

## Immediate Manual Setup Reminder

- On `GameManager` in `test.unity`, ensure `useEncounterMode = true`.
- Keep `Run` active and legacy `ArenaDebug` disabled during runtime tests.
- For walk-through debug without killing enemies, set `skipClearCondition = true` in `DefaultRunConfig.asset`; keep it `false` for normal encounter testing.
- Start a fresh run after code changes; `RunGraph` and already-built arena roots should not be reused when checking enemy AI behavior.
- `test.unity` still should be added to Build Settings later (outside PR 2.E scope).

---

## When To Update This File

Update this file when:

- the active task changes
- a major task is partially completed and should be resumed later
- a future AI agent needs to know what was already decided
- there is a temporary constraint or warning that matters only right now

Do not turn this file into a permanent architecture document.
