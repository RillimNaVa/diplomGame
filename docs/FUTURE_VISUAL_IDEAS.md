# Future Visual Polish Ideas

Captured 2026-04-30 after PR 5.A (dissolve / outline / force field / charge beam / spawn telegraph) and PR 5.B (Brute slam shaders) landed. None of these are scheduled — pick from this list when polishing post-defense or during the next polish PR.

Items are grouped by ROI / cost / risk so it's easy to scan. **Stars (⭐) flag the highest-impact picks.**

---

## 🟢 High-ROI (1-3 hours each — visible on any screencast/demo)

### 1. ⭐ Glory Kill Cinematic
**What:** when GloryKillDetector fires on a staggered melee enemy, run a 0.4s slow-mo (`Time.timeScale = 0.3`) + chromatic-aberration burst via `ArenaPostProcessingController` + screen flash + small camera zoom. Existing kill + heal still happens at the end.

**Why:** DOOM Eternal "execution" feel — biggest single cinematic moment in the game.

**How:** pure C#, hook into existing `GloryKillDetector` event. Expose `ChromaticAberration` override on the post-FX controller (mirroring `PulseDamageVignette` pattern).

**Cost:** ~3 hours. No shaders.

---

### 2. ⭐ Pickup Glow Shader for HP orbs
**What:** HP orbs (the existing pickup that drops from killed enemies) get a custom HLSL shader: rotating internal beam + Fresnel rim + horizontal scanline + slow scale pulse.

**Why:** big readability win — player sees pickups at a glance, no more "wait, is that a wall fragment?"

**How:** one HLSL shader (`PickupGlow.shader`) + auto-attached component on the orb prefab that just sets a base color.

**Cost:** ~4 hours.

---

### 3. ⭐ Bullet Impact Decals + Muzzle Flash
**What:** when the player shoots a wall, spawn (a) a quick muzzle-flash particle on the weapon (~0.05s) and (b) an emissive scorch decal on the impacted surface (lifetime ~8s, FIFO pool of ~12 max).

**Why:** basic shooter feedback — the single biggest gameplay-feel hole right now. Without it, hitting a wall feels silent/empty.

**How:**
- Muzzle flash: short-lived runtime ParticleSystem on the weapon (or a ParticleSystem prefab assigned per weapon).
- Impact decal: GameObject.CreatePrimitive(Quad) with a procedural scorch shader (radial dark gradient + emissive crack lines), oriented to the surface normal via `Quaternion.LookRotation(-hit.normal)`.
- Pool max ~12 to prevent unbounded growth.

**Cost:** ~half a day. Possibly one small shader for the decal.

---

### 4. Low-HP Screen Pulse
**What:** when player HP < 25%, vignette pulses red at ~120 BPM (heartbeat rhythm) until HP rises above the threshold.

**Why:** atmosphere + survival readability.

**How:** pure C# in `ArenaPostProcessingController` — extend the existing damage-vignette pulse logic with a continuous low-HP loop driven off `playerHealth.onHealthChanged`.

**Cost:** ~1 hour.

---

### 5. Damage Direction Indicator (HUD)
**What:** when an enemy hits the player from outside the camera view, a red half-arc/arrow appears on the HUD pointing toward the attacker, fades out over ~1s.

**Why:** big gameplay-fairness win when player gets jumped by a Crawler from behind.

**How:** UI Image (semi-circle arrow), `lastAttackerPosition` stored on hit, rotation computed from `Vector3.SignedAngle(playerForward, toAttacker, Vector3.up)`. Hook into `playerHealth.onTakeDamage` + a new `lastAttacker` field passed from enemy attacks.

**Cost:** ~3 hours. No shader.

---

## 🟡 Mid-ROI (half a day to a day — solid polish but not strictly required)

### 6. Door Portal Swirl Shader
**What:** exit doors currently use a static emissive material. Replace with a polar-UV swirl shader: rotating watercolor pattern + Fresnel rim + scrolling colors that reads as "portal to next arena".

**How:** HLSL shader with `frac(angle/(2π) - _Time*speed + radius*twist)` swirl. Apply to the exit-door visual mesh through `ArenaBuildMaterials`.

**Cost:** ~half a day.

---

### 7. Reactive Lamp Flicker
**What:** when a Brute slams or a projectile detonates near a ceiling lamp, that lamp flickers for ~0.3s.

**Why:** physical "weight" — events feel like they affect the world.

**How:** small listener that, on slam/explosion, runs `Physics.OverlapSphere(impactPos, ~6m, lampLayer)` and animates `Light.intensity` with Perlin noise for a short window.

**Cost:** ~3 hours.

---

### 8. Ambient Floating Dust Motes
**What:** runtime ParticleSystem on each ArenaRoot — slow-moving small particles drifting through the air.

**Why:** instantly makes arenas feel "alive" instead of empty boxes.

**How:** world-space ParticleSystem, low emission rate, gravity ~0, biome-tinted color from `BiomeDefinition.ambientTint`. Spawned by `ArenaFlowController` after each arena build.

**Cost:** ~2 hours. No shader.

---

## 🔴 High-cost / experimental (skip unless there's slack time after defense)

### 9. Heat Haze / Distortion Post-FX
Real refraction via `_CameraOpaqueTexture` + custom URP RenderFeature. Atmospheric around lamps/projectiles, but requires enabling `Opaque Texture` in URP renderer asset (perf hit) and writing a custom render pass.

**Risk:** perf budget, complexity, may not survive a stripped build cleanly.

---

### 10. Volumetric God Rays from Ceiling Lamps
Faked via billboard cone meshes + scrolling-noise alpha shader. Great atmosphere when it works, easily looks dirty when it doesn't.

**Risk:** ~1.5-2 days, lots of art-direction tuning, hit-or-miss visually.

---

### 11. Velocity-Based Motion Blur on Player Dash/Slide
URP doesn't have built-in motion blur. Could fake via radial-blur post-FX during dash, but it's a serious investment for a single 0.2s effect.

**Risk:** custom render pass, scope creep.

---

## How to use this list

When opening the next visual polish PR, pick a tightly-scoped subset (3-5 items max). Suggested groupings:

- **PR 5.C — Combat Feedback Layer** = #1 + #3 + #5 (~2 days, big trailer-friendly cinematic + base shooter feel + gameplay fairness)
- **PR 5.D — Environment Shaders** = #2 + #6 (~1-2 days, makes pickups/doors feel premium)
- **PR 5.E — Atmosphere Pass** = #4 + #7 + #8 (~half a day total, ambient polish)

The ⭐ items (#1, #2, #3) are the highest-impact picks if only one PR remains before defense.
