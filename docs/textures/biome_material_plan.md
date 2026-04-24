# Void Survivor — Biome Material Plan For PR 2.E.1

**Status:** planning artifact  
**Date:** 2026-04-23  
**Scope:** material-slot scheme and texture usage plan before code work for `PR 2.E.1`

---

## 1. Purpose

This file fixes the practical material plan for the first visual pass of `PR 2.E`.

It exists to answer four questions before code:

1. Which exact texture sets are approved for use.
2. Which biome each set belongs to.
3. Which slot each set should fill.
4. How `Alien Nexus` should differ from `Void Station` without breaking combat readability.

This is a planning file, not an implementation file.

---

## 2. Approved Asset Inventory

Working texture sets in [docs/textures](C:/Users/assam/DiplomGame/docs/textures):

- `Sci_fi_Metal_Panel_007_SD`
- `Sci_fi_Metal_Panel_009_SD`
- `Sci-fi_Walll_001_SD`
- `Sci-fi_Metal_Walkway_001_SD`
- `Alien_Metal_002_SD`
- `Alien_Muscle_001_SD`
- `Alien_Flesh_001`

Reference preview files that should **not** be used as runtime maps:

- `Material_01.png`
- `material_1901.png`
- `Material_1019.png`
- `Material_1930a.png`
- `Alien_Flesh_001_render.jpg`

---

## 3. Important Technical Reality

The current set is usable, but it is not fully uniform in workflow.

`Void Station` sets are close to standard URP metallic workflow:

- BaseColor
- Normal
- Metallic
- Roughness
- AO
- Height

`Alien Nexus` sets are mixed:

- `Alien_Metal_002_SD` has `ROUGH`, but no metallic map.
- `Alien_Muscle_001_SD` and `Alien_Flesh_001` include `SPEC` instead of a full metallic workflow.

This means:

- `Void Station` can be wired almost directly into a metallic/smoothness setup.
- `Alien Nexus` should be authored as a **hybrid infected biome**, not as a pure organic biome.
- Organic sets should be used mostly as contamination layers, trims, overlays, and focal patches.

---

## 4. Chosen Visual Direction

### 4.1 Void Station

Visual identity:

- clean modular sci-fi
- cold graphite and steel
- readable panel rhythm
- blue or cyan emissive navigation
- controlled structure and symmetry

Target feeling:

- technological
- sterile
- dangerous but controlled

### 4.2 Alien Nexus

Visual identity:

- **infected sci-fi**, not pure flesh
- same station architecture, but corrupted
- dark purple, burgundy, and red tinting
- organic growths in seams, corners, trims, and perimeter zones
- magenta or violet emissive

Target feeling:

- unstable
- contaminated
- alien takeover of an existing station

This is the preferred direction because it is stronger visually and safer for scope than a fully organic room kit.

---

## 5. Biome Composition Rule

### 5.1 Void Station composition

- `75-85%` clean sci-fi base
- `10-15%` accent panels and trims
- `5-10%` emissive guidance and focal highlights

### 5.2 Alien Nexus composition

- `60-70%` sci-fi architectural base
- `20-25%` alien contamination
- `10-15%` emissive corruption and accent surfaces

Interpretation:

- The combat core must remain readable.
- The biome difference should come from palette, trims, overlays, and perimeter contamination.
- `Alien Nexus` must not become a fully red organic carpet.

---

## 6. Slot Model

### 6.1 Design-level slots

These are the slots the biome system should conceptually support:

- `floorPrimary`
- `floorAccent`
- `wallPrimary`
- `wallTrim`
- `ceilingPrimary`
- `coverMaterial`
- `propMaterial`
- `emissiveAccent`

### 6.2 Runtime mapping to the current builder

Current runtime material usage in the builder is simpler:

- `floor`
- `wall`
- `ceiling`
- `cover`
- `platform`
- `ramp`
- `startMarker`
- `exitMarker`
- `barrier`

So `PR 2.E.1` should be planned in two layers:

1. Add biome authoring slots.
2. Map those slots to the current runtime builder materials with safe fallbacks.

Recommended fallback mapping for the first pass:

- `floor` <- `floorPrimary`
- `wall` <- `wallPrimary`
- `ceiling` <- `ceilingPrimary`
- `cover` <- `coverMaterial`
- `platform` <- `floorAccent` or `propMaterial`
- `ramp` <- `floorPrimary`
- `startMarker / exitMarker / barrier` <- keep procedural emissive colors for now

---

## 7. Exact Slot Assignment

## 7.1 Void Station

### floorPrimary

Primary choice:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Supporting maps:

- `Sci_fi_Metal_Panel_007_normal.png`
- `Sci_fi_Metal_Panel_007_metallic.png`
- `Sci_fi_Metal_Panel_007_roughness.png`
- `Sci_fi_Metal_Panel_007_ambientOcclusion.png`
- `Sci_fi_Metal_Panel_007_height.png`

Reason:

- modular
- readable
- clean industrial rhythm

### floorAccent

Current implementation choice after the 2026-04-24 cyan follow-up:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Reason:

- keeps `Void Station` accents readable without large cyan slabs
- should appear in strips, rings, borders, or selected floor sectors only

Deferred stronger accent option:

- `docs/textures/Sci_fi_Metal_Panel_009_SD/Sci_fi_Metal_Panel_009_basecolor.png`
- use only for narrow elite/special trims after visual tuning

Use limit:

- no more than `25-30%` of visible floor area

### wallPrimary

Primary choice:

- `docs/textures/Sci-fi_Walll_001_SD/Sci-fi_Walll_001_basecolor.png`

Reason:

- works as large-surface background material
- less noisy than accent paneling

### wallTrim

Current implementation choice after the 2026-04-24 cyan follow-up:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Use on:

- ribs
- edge trims
- door frames
- borders

Deferred stronger trim option:

- `docs/textures/Sci_fi_Metal_Panel_009_SD/Sci_fi_Metal_Panel_009_basecolor.png`
- use only if the blue band is kept narrow and does not dominate the room

### ceilingPrimary

Primary choice:

- `docs/textures/Sci-fi_Walll_001_SD/Sci-fi_Walll_001_basecolor.png`

Reason:

- calm enough for large overhead surfaces

### coverMaterial

Primary choice:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Secondary option:

- `Sci_fi_Metal_Panel_009_SD` for special props or elite arenas

### propMaterial

Current implementation choice after the 2026-04-24 cyan follow-up:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Use on:

- consoles
- technical blocks
- corner props
- support pylons

Deferred stronger prop option:

- `Sci_fi_Metal_Panel_009_SD`
- reserve for special props or elite arenas after readability tuning

### emissiveAccent

Primary choice:

- `docs/textures/Sci-fi_Metal_Walkway_001_SD/Sci-fi_Metal_Walkway_001_Emissive_01_Mask.png`
- `docs/textures/Sci-fi_Metal_Walkway_001_SD/Sci-fi_Metal_Walkway_001_Emissive_01_Color.png`

Secondary option:

- `Emissive_02_Mask.png`
- `Emissive_02_Color.png`

Use on:

- exit guidance
- strips toward doors
- accent ribs
- floor lanes

Color family:

- cyan
- electric blue

---

## 7.2 Alien Nexus

### Core rule

`Alien Nexus` uses the **same structural sci-fi base** as `Void Station`, but changes:

- tint
- accent palette
- contamination placement
- emissive color

This keeps the builder coherent and the combat readable.

### floorPrimary

Primary choice:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Authoring rule:

- tint toward dark violet or burgundy in material setup

Reason:

- keeps floor readable for movement
- supports the idea of a corrupted station

### floorAccent

Primary choices:

- `docs/textures/Alien_Metal_002_SD/Alien_Metal_002_COLOR.jpg`
- `docs/textures/Sci_fi_Metal_Panel_009_SD/Sci_fi_Metal_Panel_009_basecolor.png` with alien tint

Reason:

- `Alien_Metal` works better as infected accent sectors than as full-floor replacement
- accent panels can bridge the clean sci-fi and the contaminated areas

### wallPrimary

Primary choice:

- `docs/textures/Sci-fi_Walll_001_SD/Sci-fi_Walll_001_basecolor.png`

Authoring rule:

- tint darker and more purple than `Void Station`

Reason:

- keeps the base room structure readable
- makes the alien layer feel like a takeover, not a different building kit

### wallTrim

Primary choice:

- `docs/textures/Alien_Muscle_001_SD/Alien_Muscle_001_COLOR.jpg`

Use on:

- seams
- wall trims
- pillar edges
- near-door corruption veins
- perimeter contamination

Restriction:

- do not use as a full large-surface wall replacement

### ceilingPrimary

Primary choice:

- `docs/textures/Sci-fi_Walll_001_SD/Sci-fi_Walll_001_basecolor.png`

Overlay option:

- sparse `Alien_Flesh_001` contamination patches

Reason:

- a fully organic ceiling would become visually noisy too fast

### coverMaterial

Primary choice:

- `docs/textures/Sci_fi_Metal_Panel_007_SD/Sci_fi_Metal_Panel_007_basecolor.png`

Variation:

- tint to biome palette
- allow selected contaminated variants for elite or boss encounters

### propMaterial

Primary choices:

- `docs/textures/Alien_Metal_002_SD/Alien_Metal_002_COLOR.jpg`
- tinted `Sci_fi_Metal_Panel_009_SD`

Use on:

- corrupted pylons
- infected terminals
- perimeter blocks

### emissiveAccent

Primary choice:

- reuse `Sci-fi_Metal_Walkway_001_SD` emissive masks

Authoring rule:

- recolor from cyan to violet, magenta, or pink-purple

Reason:

- the mask structure is already clean and readable
- only the color language needs to change

### Alien organic patch material

Reserved use:

- `docs/textures/Alien_Flesh_001/Alien_Flesh_001_color.jpg`

Use only as:

- wall infection islands
- ceiling growths
- local takeover zones
- boss-arena contamination emphasis

Restriction:

- never use as the universal wall or floor base

---

## 8. Placement Rules By Zone

### center

- keep mostly clean
- prefer `floorPrimary`
- avoid dense props and heavy contamination

### perimeter

- strongest candidate for trims, props, and alien growth
- use `wallTrim`, `propMaterial`, and contamination clusters

### corners

- best place for infected props, terminals, pylons, or growth pillars

### near walls

- use ribs, trims, tech panels, muscle-like veins

### near exits

- stronger emissive
- stronger focal contrast
- optional contamination framing in `Alien Nexus`

### safe combat core

- avoid noisy patterns
- avoid tall decor
- avoid strong organic silhouettes that obscure enemies

---

## 9. Arena-Type Variation Rules

These should affect material usage, not biome identity.

### Combat

- standard balance
- clear central floor
- moderate accents

### Elite

- slightly stronger trims
- stronger emissive focus
- limited contamination increase

### Parkour

- more directional floor guidance
- clearer platform and ramp separation
- stronger lane readability

### Shop / Rest

- calmer floor
- less contamination
- more symmetric composition

### Boss

- strongest focal patterning
- most aggressive contamination in `Alien Nexus`
- keep the center readable despite spectacle

---

## 10. Unity Import Notes For PR 2.E.1

### Common

- mark normal maps as Normal in Unity import settings
- roughness must be inverted into smoothness for URP Lit
- AO should be treated as Occlusion
- Height is optional for first pass and may stay unused initially

### Void Station

- can follow metallic workflow directly

### Alien Nexus

- `Alien_Metal_002_SD`: use roughness-driven smoothness and manual metallic tuning
- `Alien_Muscle_001_SD`: treat `SPEC` as reference for gloss or highlight behavior, not as a direct metallic replacement
- `Alien_Flesh_001`: same rule; use conservative smoothness and do not over-polish

Important:

- `Alien Nexus` should win through composition and palette, not through physically perfect organic shading

---

## 11. Implementation Order

Recommended order for the next real implementation step:

1. Extend `BiomeDefinition` with texture and material slot references.
2. Create two curated biome material libraries from the approved sets.
3. Wire `Void Station` first.
4. Wire `Alien Nexus` second as a hybrid infected variant.
5. Only then move to builder details and decor placement.

---

## 12. Approval Summary

This plan approves the current texture pack for `PR 2.E.1` with the following conclusion:

- `Void Station` is ready as the clean base biome.
- `Alien Nexus` should be implemented as **corrupted Void Station**, not as a full organic biome.
- The chosen texture set is enough to start the material-slot phase.
- The next step after this document is biome-slot implementation, not new texture hunting.
