using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Build
{
    public static class ArenaBuilder
    {
        public const string RootName = "ArenaRoot";
        static Material s_shopParticleMaterial;

        // =====================================================================
        // r4 single-arena entry point. Uses the room's shape mask + per-arena
        // ceiling and pre-resolved cover / exit anchors from SingleArenaGenerator.
        // No Room_N hierarchy — only ArenaRoot/Shell/Cover/Anchors/Exits.
        // =====================================================================
        public static GameObject BuildSingle(
            ArenaRuntimeContext ctx, ArenaRunConfig cfg, Transform parent, ArenaBuildMaterials mats = null)
        {
            if (ctx == null || ctx.layout == null || cfg == null || parent == null) return null;
            if (ctx.layout.rooms.Count == 0) return null;
            var room = ctx.layout.rooms[0];
            if (room.shapeMask == null) return null;
            if (mats == null) mats = ArenaBuildMaterials.CreateDefaults();

            Clear(parent);

            float wh = room.wallHeightMeters > 0f ? room.wallHeightMeters : cfg.wallHeightMeters;

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var shell = new GameObject("Shell");
            shell.transform.SetParent(root.transform, false);

            BuildSingleShell(room, cfg, wh, mats, shell.transform);
            BuildSingleVerticality(room, mats, root.transform);
            BuildSingleStructures(room, mats, root.transform);
            BuildSingleCover(room, mats.cover, root.transform);
            BuildSingleExits(room, cfg, wh, mats, root.transform);
            BuildSingleStartMarker(room, cfg, mats.startMarker, root.transform);
            BuildSingleArchitecture(room, cfg, wh, mats, root.transform);
            BuildSingleFloorPatterns(room, cfg, mats, root.transform);
            BuildSingleShopTerminal(room, cfg, mats, root.transform);
            BuildSingleDecor(room, cfg, wh, mats, root.transform);
            BuildSingleAtmosphere(room, cfg, wh, mats, root.transform);
            BuildSingleEdgeStrips(room, cfg, mats, root.transform);
            BuildSingleFillLights(room, cfg, wh, mats, root.transform);
            if (cfg.generateAnchors) BuildSingleAnchors(room, cfg, wh, root.transform);

            return root;
        }

        static void BuildSingleShell(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform shell)
        {
            float m = cfg.macroCellMeters;
            float ft = cfg.floorThicknessMeters;
            float ct = cfg.ceilingThicknessMeters;
            float th = cfg.wallThicknessMeters;
            var b = room.boundsCells;
            var mask = room.shapeMask;
            int w = mask.GetLength(0);
            int h = mask.GetLength(1);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[x, y]) continue;
                int wx = x + b.xMin, wy = y + b.yMin;
                float cx = wx * m + m * 0.5f;
                float cz = wy * m + m * 0.5f;

                BuildUtils.SpawnBox(shell, $"Floor_{wx}_{wy}",
                    new Vector3(cx, -ft * 0.5f, cz),
                    new Vector3(m, ft, m), mats.floor, true);

                BuildUtils.SpawnBox(shell, $"Ceiling_{wx}_{wy}",
                    new Vector3(cx, wh + ct * 0.5f, cz),
                    new Vector3(m, ct, m), mats.ceiling, true);

                TryEmitMaskWall(mask, room, x, y,  0, -1, wx, wy, m, wh, th, mats.wall, shell);
                TryEmitMaskWall(mask, room, x, y,  0,  1, wx, wy, m, wh, th, mats.wall, shell);
                TryEmitMaskWall(mask, room, x, y, -1,  0, wx, wy, m, wh, th, mats.wall, shell);
                TryEmitMaskWall(mask, room, x, y,  1,  0, wx, wy, m, wh, th, mats.wall, shell);
            }
        }

        static bool IsDoorOpening(ArenaRoomData room, int wx, int wy, int dx, int dy)
        {
            if (room.exitDoorAnchors == null) return false;
            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var a = room.exitDoorAnchors[i];
                if (a.cell.x == wx && a.cell.y == wy &&
                    a.outwardDir.x == dx && a.outwardDir.y == dy)
                    return true;
            }
            return false;
        }

        static void TryEmitMaskWall(
            bool[,] mask, ArenaRoomData room, int x, int y, int dx, int dy, int wx, int wy,
            float m, float wh, float th, Material wallMat, Transform shell)
        {
            int nx = x + dx, ny = y + dy;
            int w = mask.GetLength(0), h = mask.GetLength(1);
            bool neighborInterior = nx >= 0 && ny >= 0 && nx < w && ny < h && mask[nx, ny];
            if (neighborInterior) return;

            bool isDoor = IsDoorOpening(room, wx, wy, dx, dy);
            // Door openings: emit only the lintel (wall segment above the door),
            // leaving a walk-through gap below. Non-doors: emit full wall.
            float wallHeight = wh;
            float wallCenterY = wh * 0.5f;
            if (isDoor)
            {
                float doorHeight = Mathf.Min(wh * 0.7f, 5f);
                if (doorHeight >= wh) return; // no lintel fits
                wallHeight = wh - doorHeight;
                wallCenterY = doorHeight + wallHeight * 0.5f;
            }

            Vector3 c = new Vector3(wx * m + m * 0.5f, wallCenterY, wy * m + m * 0.5f);
            Vector3 size;
            string tag;
            if (dy == -1)      { c += new Vector3(0f, 0f, -m * 0.5f + th * 0.5f); size = new Vector3(m, wallHeight, th); tag = "S"; }
            else if (dy == 1)  { c += new Vector3(0f, 0f,  m * 0.5f - th * 0.5f); size = new Vector3(m, wallHeight, th); tag = "N"; }
            else if (dx == -1) { c += new Vector3(-m * 0.5f + th * 0.5f, 0f, 0f); size = new Vector3(th, wallHeight, m); tag = "W"; }
            else               { c += new Vector3( m * 0.5f - th * 0.5f, 0f, 0f); size = new Vector3(th, wallHeight, m); tag = "E"; }

            string namePrefix = isDoor ? "Lintel" : "Wall";
            BuildUtils.SpawnBox(shell, $"{namePrefix}_{wx}_{wy}_{tag}", c, size, wallMat, true);
        }

        static void BuildSingleCover(ArenaRoomData room, Material coverMat, Transform parent)
        {
            if (room.coverPlacements.Count == 0) return;
            var coverRoot = new GameObject("Cover");
            coverRoot.transform.SetParent(parent, false);
            for (int i = 0; i < room.coverPlacements.Count; i++)
            {
                var p = room.coverPlacements[i];
                var go = BuildUtils.SpawnBox(coverRoot.transform, $"Cover_{i}", p.position, p.size, coverMat, true);
                if (Mathf.Abs(p.yawDeg) > 0.01f)
                    go.transform.rotation = Quaternion.Euler(0f, p.yawDeg, 0f);
            }
        }

        // PR 2.H1 — hand-authored structures (bunker / sandbag line / pillar
        // cluster / sniper nest). Looks up the StructureDefinition by id from
        // BuiltInStructures (future: per-profile pools), spawns each part via
        // BuildUtils.SpawnBox so the existing WorldUVScaler + per-biome
        // material pipeline applies for free. Yaw is quantized to 90° so all
        // parts stay axis-aligned to the cell grid.
        static void BuildSingleStructures(ArenaRoomData room, ArenaBuildMaterials mats, Transform parent)
        {
            if (room.structurePlacements == null || room.structurePlacements.Count == 0) return;

            var defsById = new Dictionary<string, StructureDefinition>();
            var defaults = BuiltInStructures.All();
            for (int i = 0; i < defaults.Length; i++)
            {
                if (defaults[i] != null && !string.IsNullOrEmpty(defaults[i].structureId))
                    defsById[defaults[i].structureId] = defaults[i];
            }

            var structuresRoot = new GameObject("Structures");
            structuresRoot.transform.SetParent(parent, false);

            for (int i = 0; i < room.structurePlacements.Count; i++)
            {
                var p = room.structurePlacements[i];
                if (!defsById.TryGetValue(p.structureId, out StructureDefinition def) || def == null) continue;

                var pivot = new GameObject($"Structure_{p.structureId}_{i}");
                pivot.transform.SetParent(structuresRoot.transform, false);
                pivot.transform.position = p.position;
                pivot.transform.rotation = Quaternion.Euler(0f, p.yawDeg, 0f);

                for (int k = 0; k < def.parts.Length; k++)
                {
                    StructureBoxPart bp = def.parts[k];
                    Material partMat = ResolveStructureMaterial(bp.slot, mats);
                    bool collide = bp.slot != StructureSlot.EmissiveAccent && bp.slot != StructureSlot.Decor;
                    // Spawn at world-space transformed offset; SpawnBox places the
                    // box centered, so add half-height on Y to put the base at y=0.
                    Vector3 worldOffset = pivot.transform.TransformVector(bp.localOffset)
                                          + Vector3.up * (bp.size.y * 0.5f);
                    Vector3 spawnPos = p.position + worldOffset;
                    var go = BuildUtils.SpawnBox(pivot.transform, $"Part_{k}_{bp.slot}", spawnPos, bp.size, partMat, collide);
                    // Re-apply yaw on the part so its dominant face still maps correctly
                    // for WorldUVScaler. SpawnBox sets identity rotation otherwise.
                    if (Mathf.Abs(p.yawDeg) > 0.01f)
                        go.transform.rotation = Quaternion.Euler(0f, p.yawDeg, 0f);
                }
            }
        }

        static Material ResolveStructureMaterial(StructureSlot slot, ArenaBuildMaterials mats)
        {
            if (mats == null) return null;
            switch (slot)
            {
                case StructureSlot.Wall:           return mats.wall;
                case StructureSlot.Cover:          return mats.cover;
                case StructureSlot.Trim:           return mats.wallTrim != null ? mats.wallTrim : mats.wall;
                case StructureSlot.Floor:          return mats.platform != null ? mats.platform : mats.cover;
                case StructureSlot.Decor:          return mats.prop != null ? mats.prop : mats.cover;
                case StructureSlot.EmissiveAccent: return mats.emissiveAccent;
                default: return mats.cover;
            }
        }

        static void BuildSingleVerticality(ArenaRoomData room, ArenaBuildMaterials mats, Transform parent)
        {
            if (!AllowsVerticality(room.category)) return;

            bool hasPlatforms = room.platformPlacements.Count > 0;
            bool hasRamps = room.rampPlacements.Count > 0;
            if (!hasPlatforms && !hasRamps) return;

            var root = new GameObject("Verticality");
            root.transform.SetParent(parent, false);

            if (hasPlatforms)
            {
                var platformsRoot = new GameObject("Platforms");
                platformsRoot.transform.SetParent(root.transform, false);
                for (int i = 0; i < room.platformPlacements.Count; i++)
                {
                    var p = room.platformPlacements[i];
                    var go = BuildUtils.SpawnBox(platformsRoot.transform, $"Platform_{i}", p.center, p.size, mats.platform, true);
                    if (Mathf.Abs(p.yawDeg) > 0.01f)
                        go.transform.rotation = Quaternion.Euler(0f, p.yawDeg, 0f);
                }
            }

            if (hasRamps)
            {
                var rampsRoot = new GameObject("Ramps");
                rampsRoot.transform.SetParent(root.transform, false);
                for (int i = 0; i < room.rampPlacements.Count; i++)
                {
                    var ramp = room.rampPlacements[i];
                    var go = BuildUtils.SpawnBox(rampsRoot.transform, $"Ramp_{i}", ramp.center, ramp.size, mats.ramp, true);
                    go.transform.rotation = Quaternion.Euler(ramp.pitchDeg, ramp.yawDeg, 0f);
                }
            }
        }

        static void BuildSingleExits(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform parent)
        {
            if (room.exitDoorAnchors.Count == 0) return;
            var exitsRoot = new GameObject("Exits");
            exitsRoot.transform.SetParent(parent, false);
            float m = cfg.macroCellMeters;
            float doorHeight = Mathf.Min(wh * 0.7f, 5f);
            float doorWidth = m * 0.9f;
            float doorThickness = 0.05f;
            var biome = mats != null ? mats.sourceBiome : null;
            Color exitLightColor = biome != null && biome.exitLightColor.a > 0.01f
                ? biome.exitLightColor
                : (biome != null ? biome.exitMarkerColor : new Color(1f, 0.3f, 0.3f));
            float exitLightIntensity = biome != null ? biome.accentLightIntensity : 2.2f;
            float exitLightRange = biome != null ? Mathf.Max(1f, biome.accentLightRange) : 8f;
            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var a = room.exitDoorAnchors[i];
                Vector3 size;
                if (a.outwardDir.x != 0)
                    size = new Vector3(doorThickness, doorHeight, doorWidth);
                else
                    size = new Vector3(doorWidth, doorHeight, doorThickness);
                Vector3 center = new Vector3(a.worldCenter.x, doorHeight * 0.5f, a.worldCenter.z);
                var go = BuildUtils.SpawnBox(exitsRoot.transform, $"Exit_{i}", center, size, mats != null ? mats.exitMarker : null, false);
                var anchor = new GameObject($"ExitAnchor_{i}");
                anchor.transform.SetParent(exitsRoot.transform, false);
                anchor.transform.position = new Vector3(a.worldCenter.x, 0f, a.worldCenter.z);
                anchor.transform.rotation = Quaternion.Euler(0f, a.yawDeg, 0f);

                if (exitLightIntensity > 0.01f)
                {
                    // Nudge light slightly inward so it lights the room, not the void beyond the wall.
                    Vector3 lightPos = center
                        - new Vector3(a.outwardDir.x * m * 0.4f, 0f, a.outwardDir.y * m * 0.4f);
                    AttachPointLight(exitsRoot.transform, $"ExitLight_{i}", lightPos,
                        exitLightColor, exitLightIntensity, exitLightRange);
                }
            }
        }

        static void BuildSingleStartMarker(
            ArenaRoomData room, ArenaRunConfig cfg, Material startMat, Transform parent)
        {
            if (startMat == null) return;
            float m = cfg.macroCellMeters;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "StartMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(room.startSpawnPoint.x, 0.05f, room.startSpawnPoint.z);
            marker.transform.localScale = new Vector3(m * 0.8f, 0.1f, m * 0.8f);
            var rend = marker.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = startMat;
            var col = marker.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        static void BuildSingleArchitecture(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null) return;

            var root = new GameObject("Architecture");
            root.transform.SetParent(parent, false);

            BuildCornerPillars(room, cfg, wh, mats.wall, root.transform);
            BuildDoorFrames(room, cfg, wh, mats.wallTrim ?? mats.wall, root.transform);
            BuildWallRibs(room, cfg, wh, mats.wallTrim ?? mats.wall, root.transform);
            BuildCeilingBeams(room, cfg, wh, mats.ceiling ?? mats.wall, root.transform);
        }

        static void BuildSingleFloorPatterns(
            ArenaRoomData room, ArenaRunConfig cfg, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null || mats.floorAccent == null) return;

            var root = new GameObject("FloorDetails");
            root.transform.SetParent(parent, false);

            float m = cfg.macroCellMeters;
            float overlayY = 0.03f;
            Vector3 center = BoundsCenter(room.boundsCells, m, overlayY);
            float roomSpan = Mathf.Min(room.boundsCells.width, room.boundsCells.height) * m;
            float centerSize = room.category == ArenaCategory.Boss ? roomSpan * 0.26f :
                               room.category == ArenaCategory.Elite ? roomSpan * 0.22f :
                               room.category == ArenaCategory.Parkour ? roomSpan * 0.2f :
                               roomSpan * 0.18f;
            centerSize = Mathf.Max(m * 1.4f, centerSize);

            BuildUtils.SpawnBox(root.transform, "CenterPlate_Base",
                center,
                new Vector3(centerSize, 0.05f, centerSize),
                mats.floor,
                false);

            float ringThickness = Mathf.Max(0.24f, m * 0.12f);
            Material accentMat = mats.floorAccent ?? mats.emissiveAccent ?? mats.floor;
            BuildUtils.SpawnBox(root.transform, "CenterRing_N",
                center + new Vector3(0f, 0.02f, centerSize * 0.5f - ringThickness * 0.5f),
                new Vector3(centerSize, 0.03f, ringThickness),
                accentMat,
                false);
            BuildUtils.SpawnBox(root.transform, "CenterRing_S",
                center + new Vector3(0f, 0.02f, -centerSize * 0.5f + ringThickness * 0.5f),
                new Vector3(centerSize, 0.03f, ringThickness),
                accentMat,
                false);
            BuildUtils.SpawnBox(root.transform, "CenterRing_E",
                center + new Vector3(centerSize * 0.5f - ringThickness * 0.5f, 0.02f, 0f),
                new Vector3(ringThickness, 0.03f, centerSize),
                accentMat,
                false);
            BuildUtils.SpawnBox(root.transform, "CenterRing_W",
                center + new Vector3(-centerSize * 0.5f + ringThickness * 0.5f, 0.02f, 0f),
                new Vector3(ringThickness, 0.03f, centerSize),
                accentMat,
                false);

            float padWidth = m * 1.05f;
            Material exitPadMaterial = mats.floorAccent ?? mats.floor;
            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var anchor = room.exitDoorAnchors[i];
                Vector3 padCenter = new Vector3(anchor.worldCenter.x, overlayY, anchor.worldCenter.z)
                    - new Vector3(anchor.outwardDir.x * m * 0.75f, 0f, anchor.outwardDir.y * m * 0.75f);
                BuildUtils.SpawnBox(root.transform, $"ExitPad_{i}",
                    padCenter,
                    new Vector3(
                        anchor.outwardDir.x != 0 ? ringThickness : padWidth,
                        0.04f,
                        anchor.outwardDir.x != 0 ? padWidth : ringThickness),
                    exitPadMaterial,
                    false);
            }

            BuildUtils.SpawnBox(root.transform, "StartPad",
                new Vector3(room.startSpawnPoint.x, overlayY, room.startSpawnPoint.z),
                new Vector3(m * 1.1f, 0.04f, m * 1.1f),
                mats.floorAccent ?? mats.floor,
                false);
        }

        static void BuildSingleDecor(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null || mats.prop == null) return;

            int decorCount = ResolveDecorCount(room.category);
            if (decorCount <= 0) return;

            var root = new GameObject("Decor");
            root.transform.SetParent(parent, false);

            float m = cfg.macroCellMeters;
            bool[,] mask = room.shapeMask;
            int[,] targets = new int[,]
            {
                { 1, 1 },
                { mask.GetLength(0) - 2, 1 },
                { 1, mask.GetLength(1) - 2 },
                { mask.GetLength(0) - 2, mask.GetLength(1) - 2 },
                { mask.GetLength(0) / 2, 1 },
                { mask.GetLength(0) / 2, mask.GetLength(1) - 2 },
                { 1, mask.GetLength(1) / 2 },
                { mask.GetLength(0) - 2, mask.GetLength(1) / 2 },
            };

            int placed = 0;
            for (int i = 0; i < targets.GetLength(0) && placed < decorCount; i++)
            {
                if (!TryFindInteriorCell(mask, targets[i, 0], targets[i, 1], out int lx, out int ly))
                    continue;

                Vector3 pos = LocalCellCenter(room.boundsCells, lx, ly, m, 0f);
                if (IsNearPoint(pos, room.startSpawnPoint, m * 1.5f)) continue;
                if (IsNearAnyExit(pos, room.exitDoorAnchors, m * 1.25f)) continue;

                float height = room.category == ArenaCategory.Boss ? wh * 0.24f : wh * 0.18f;
                Vector3 size = new Vector3(m * 0.65f, height, m * 0.65f);
                var block = BuildUtils.SpawnBox(root.transform, $"Prop_{placed}", pos + new Vector3(0f, height * 0.5f, 0f), size, mats.prop, true);
                block.transform.rotation = Quaternion.Euler(0f, placed * 37f, 0f);

                if (ShouldUseDecorAccent(room.category) && mats.emissiveAccent != null)
                {
                    BuildUtils.SpawnBox(block.transform, "AccentBand",
                        new Vector3(0f, height * 0.18f, 0f),
                        new Vector3(size.x * 0.9f, Mathf.Max(0.08f, wh * 0.015f), size.z * 0.9f),
                        mats.emissiveAccent,
                        false);
                }

                placed++;
            }
        }

        static void BuildSingleShopTerminal(ArenaRoomData room, ArenaRunConfig cfg, ArenaBuildMaterials mats, Transform parent)
        {
            if (room.category != ArenaCategory.Shop || cfg == null || mats == null) return;

            var root = new GameObject("ShopTerminal");
            root.transform.SetParent(parent, false);

            float m = cfg.macroCellMeters;
            Vector3 center = BoundsCenter(room.boundsCells, m, 0.08f);
            Material plateMat = mats.floorAccent != null ? mats.floorAccent : mats.floor;
            Material propMat = mats.prop != null ? mats.prop : mats.cover;
            Color shopGlow = ResolveShopGlowColor(mats);
            Material glowMat = CreateShopGlowMaterial(shopGlow);

            BuildUtils.SpawnBox(root.transform, "ShopPlatform_Base",
                center,
                new Vector3(m * 1.55f, 0.10f, m * 1.55f),
                plateMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_SoftGlow",
                center + new Vector3(0f, 0.115f, 0f),
                new Vector3(m * 1.24f, 0.025f, m * 1.24f),
                glowMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_GlowLine_X",
                center + new Vector3(0f, 0.145f, 0f),
                new Vector3(m * 1.02f, 0.025f, m * 0.08f),
                glowMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_GlowLine_Z",
                center + new Vector3(0f, 0.15f, 0f),
                new Vector3(m * 0.08f, 0.025f, m * 1.02f),
                glowMat,
                false);

            float ring = Mathf.Max(0.18f, m * 0.10f);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_Ring_N",
                center + new Vector3(0f, 0.04f, m * 0.72f),
                new Vector3(m * 1.55f, 0.06f, ring),
                glowMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_Ring_S",
                center + new Vector3(0f, 0.04f, -m * 0.72f),
                new Vector3(m * 1.55f, 0.06f, ring),
                glowMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_Ring_E",
                center + new Vector3(m * 0.72f, 0.04f, 0f),
                new Vector3(ring, 0.06f, m * 1.55f),
                glowMat,
                false);
            BuildUtils.SpawnBox(root.transform, "ShopPlatform_Ring_W",
                center + new Vector3(-m * 0.72f, 0.04f, 0f),
                new Vector3(ring, 0.06f, m * 1.55f),
                glowMat,
                false);

            Vector3 kioskBase = center + new Vector3(0f, 0f, -m * 0.95f);
            BuildUtils.SpawnBox(root.transform, "ShopKiosk_Base",
                kioskBase + new Vector3(0f, 0.45f, 0f),
                new Vector3(m * 0.42f, 0.9f, m * 0.28f),
                propMat,
                true);
            BuildUtils.SpawnBox(root.transform, "ShopKiosk_Screen",
                kioskBase + new Vector3(0f, 1.05f, m * 0.16f),
                new Vector3(m * 0.62f, 0.36f, 0.05f),
                glowMat,
                false);

            AttachPointLight(root.transform, "ShopPlatform_Light",
                center + new Vector3(0f, 0.35f, 0f),
                shopGlow,
                2.2f,
                Mathf.Max(5f, m * 2.2f));
            SpawnShopPlatformParticles(root.transform, center, m, shopGlow);

            var triggerGo = new GameObject("ShopPlatformTrigger");
            triggerGo.transform.SetParent(root.transform, false);
            triggerGo.transform.position = center + new Vector3(0f, 1f, 0f);
            var trigger = triggerGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(m * 1.55f, 2f, m * 1.55f);
            triggerGo.AddComponent<ShopTerminalTrigger>();
        }

        static Color ResolveShopGlowColor(ArenaBuildMaterials mats)
        {
            Color fallback = new Color(0.25f, 0.85f, 1f);
            var biome = mats != null ? mats.sourceBiome : null;
            if (biome == null) return fallback;

            Color color = biome.emissiveAccent != null && biome.emissiveAccent.emissionIntensity > 0.01f
                ? biome.emissiveAccent.emissionColor
                : biome.barrierColor;
            return color.maxColorComponent > 0.05f ? color : fallback;
        }

        static Material CreateShopGlowMaterial(Color tint)
        {
            Color glow = tint.maxColorComponent > 0.05f ? tint : new Color(0.25f, 0.85f, 1f);
            Color visible = Color.Lerp(Color.white, glow, 0.72f);
            visible.a = 1f;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = "ShopPlatformGlow(Runtime)" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", visible * 1.7f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", visible * 1.7f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", glow * 2.8f);
            }
            mat.enableInstancing = true;
            WorldUVDensityRegistry.Register(mat, 0.5f);
            return mat;
        }

        static void SpawnShopPlatformParticles(Transform parent, Vector3 center, float m, Color tint)
        {
            var go = new GameObject("ShopPlatformParticles");
            go.transform.SetParent(parent, false);
            go.transform.position = center + new Vector3(0f, 0.16f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = ResolveShopParticleMaterial();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingFudge = 0.1f;
            }

            Color particleColor = Color.Lerp(Color.white, tint, 0.65f);
            particleColor.a = 0.55f;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 4f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.105f);
            main.startColor = particleColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 22f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(m * 1.1f, 0.04f, m * 1.1f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.65f, 1.15f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(particleColor, 0f),
                    new GradientColorKey(particleColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.6f, 0.2f),
                    new GradientAlphaKey(0.45f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.7f),
                    new Keyframe(0.45f, 1f),
                    new Keyframe(1f, 0.2f)));

            ps.Play();
        }

        static void BuildSingleAtmosphere(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null) return;

            var root = new GameObject("Atmosphere");
            root.transform.SetParent(parent, false);
            float m = cfg.macroCellMeters;

            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var anchor = room.exitDoorAnchors[i];
                Vector3 sideOffset = anchor.outwardDir.x != 0
                    ? new Vector3(0f, 0f, m * 0.42f)
                    : new Vector3(m * 0.42f, 0f, 0f);
                Vector3 basePos = new Vector3(anchor.worldCenter.x, 0f, anchor.worldCenter.z)
                    - new Vector3(anchor.outwardDir.x * m * 0.85f, 0f, anchor.outwardDir.y * m * 0.85f);

                SpawnAtmospherePylon(root.transform, $"ExitGlow_{i}_A", basePos + sideOffset, wh, mats);
                SpawnAtmospherePylon(root.transform, $"ExitGlow_{i}_B", basePos - sideOffset, wh, mats);
            }

            if (mats.sourceBiome != null && mats.sourceBiome.useContaminationLayer && mats.contamination != null)
            {
                BuildContaminationPatches(room, cfg, mats, root.transform);
            }
        }

        static bool AllowsVerticality(ArenaCategory category)
        {
            switch (category)
            {
                case ArenaCategory.Combat:
                case ArenaCategory.Elite:
                case ArenaCategory.Parkour:
                case ArenaCategory.Boss:
                    return true;
                default:
                    return false;
            }
        }

        static int ResolveDecorCount(ArenaCategory category)
        {
            switch (category)
            {
                case ArenaCategory.Start:
                    return 0;
                case ArenaCategory.Shop:
                case ArenaCategory.Rest:
                    return 2;
                case ArenaCategory.Boss:
                    return 5;
                default:
                    return 4;
            }
        }

        static bool ShouldUseDecorAccent(ArenaCategory category)
        {
            switch (category)
            {
                case ArenaCategory.Combat:
                case ArenaCategory.Elite:
                case ArenaCategory.Parkour:
                case ArenaCategory.Boss:
                    return true;
                default:
                    return false;
            }
        }

        static void BuildCornerPillars(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, Material mat, Transform parent)
        {
            if (mat == null) return;

            bool[,] mask = room.shapeMask;
            float m = cfg.macroCellMeters;
            int[,] targets = new int[,]
            {
                { 0, 0 },
                { mask.GetLength(0) - 1, 0 },
                { 0, mask.GetLength(1) - 1 },
                { mask.GetLength(0) - 1, mask.GetLength(1) - 1 }
            };

            for (int i = 0; i < targets.GetLength(0); i++)
            {
                if (!TryFindInteriorCell(mask, targets[i, 0], targets[i, 1], out int lx, out int ly))
                    continue;
                Vector3 pos = LocalCellCenter(room.boundsCells, lx, ly, m, 0f);
                BuildUtils.SpawnBox(parent, $"CornerPillar_{i}",
                    pos + new Vector3(0f, wh * 0.5f, 0f),
                    new Vector3(m * 0.28f, wh, m * 0.28f),
                    mat,
                    true);
            }
        }

        static void BuildDoorFrames(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, Material mat, Transform parent)
        {
            if (mat == null || room.exitDoorAnchors == null) return;

            float m = cfg.macroCellMeters;
            float doorHeight = Mathf.Min(wh * 0.7f, 5f);
            float postWidth = Mathf.Max(0.18f, cfg.wallThicknessMeters * 0.8f);
            float topHeight = Mathf.Max(0.3f, wh * 0.05f);

            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var anchor = room.exitDoorAnchors[i];
                Vector3 center = new Vector3(anchor.worldCenter.x, doorHeight * 0.5f, anchor.worldCenter.z);
                bool alongX = anchor.outwardDir.x == 0;
                Vector3 lateral = alongX ? new Vector3(m * 0.45f, 0f, 0f) : new Vector3(0f, 0f, m * 0.45f);

                Vector3 postSize = alongX
                    ? new Vector3(postWidth, doorHeight, cfg.wallThicknessMeters * 1.2f)
                    : new Vector3(cfg.wallThicknessMeters * 1.2f, doorHeight, postWidth);
                BuildUtils.SpawnBox(parent, $"DoorFrame_Left_{i}", center - lateral, postSize, mat, true);
                BuildUtils.SpawnBox(parent, $"DoorFrame_Right_{i}", center + lateral, postSize, mat, true);

                Vector3 topCenter = new Vector3(anchor.worldCenter.x, doorHeight + topHeight * 0.5f, anchor.worldCenter.z);
                Vector3 topSize = alongX
                    ? new Vector3(m * 1.1f, topHeight, cfg.wallThicknessMeters * 1.2f)
                    : new Vector3(cfg.wallThicknessMeters * 1.2f, topHeight, m * 1.1f);
                BuildUtils.SpawnBox(parent, $"DoorFrame_Top_{i}", topCenter, topSize, mat, true);
            }
        }

        static void BuildWallRibs(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, Material mat, Transform parent)
        {
            if (mat == null || room.shapeMask == null) return;

            float m = cfg.macroCellMeters;
            float ribDepth = Mathf.Max(0.15f, cfg.wallThicknessMeters * 0.6f);
            float ribWidth = Mathf.Max(0.2f, m * 0.18f);
            bool[,] mask = room.shapeMask;

            for (int y = 0; y < mask.GetLength(1); y++)
            for (int x = 0; x < mask.GetLength(0); x++)
            {
                if (!mask[x, y]) continue;
                int wx = x + room.boundsCells.xMin;
                int wy = y + room.boundsCells.yMin;
                if ((wx + wy) % 3 != 0) continue;

                TrySpawnRib(mask, room, x, y, 0, -1, m, wh, ribWidth, ribDepth, mat, parent);
                TrySpawnRib(mask, room, x, y, 0, 1, m, wh, ribWidth, ribDepth, mat, parent);
                TrySpawnRib(mask, room, x, y, -1, 0, m, wh, ribDepth, ribWidth, mat, parent);
                TrySpawnRib(mask, room, x, y, 1, 0, m, wh, ribDepth, ribWidth, mat, parent);
            }
        }

        static void TrySpawnRib(
            bool[,] mask, ArenaRoomData room, int x, int y, int dx, int dy,
            float m, float wh, float sx, float sz, Material mat, Transform parent)
        {
            int nx = x + dx;
            int ny = y + dy;
            bool neighborInterior = nx >= 0 && ny >= 0 && nx < mask.GetLength(0) && ny < mask.GetLength(1) && mask[nx, ny];
            if (neighborInterior) return;

            int wx = x + room.boundsCells.xMin;
            int wy = y + room.boundsCells.yMin;
            if (IsDoorOpening(room, wx, wy, dx, dy)) return;

            Vector3 center = LocalCellCenter(room.boundsCells, x, y, m, wh * 0.5f);
            center += new Vector3(dx * (m * 0.5f - sx * 0.5f), 0f, dy * (m * 0.5f - sz * 0.5f));

            BuildUtils.SpawnBox(parent, $"WallRib_{wx}_{wy}_{dx}_{dy}",
                center,
                new Vector3(sx, wh * 0.92f, sz),
                mat,
                true);
        }

        static void BuildCeilingBeams(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, Material mat, Transform parent)
        {
            if (mat == null) return;

            float m = cfg.macroCellMeters;
            float width = room.boundsCells.width * m;
            float height = room.boundsCells.height * m;
            Vector3 center = BoundsCenter(room.boundsCells, m, wh - Mathf.Max(0.25f, cfg.ceilingThicknessMeters));
            float beamThickness = Mathf.Max(0.3f, wh * 0.04f);

            int longAxisCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(width, height) / (m * 5f)), 1, 4);
            for (int i = 0; i < longAxisCount; i++)
            {
                float t = longAxisCount == 1 ? 0.5f : (float)i / (longAxisCount - 1);
                if (width >= height)
                {
                    float z = Mathf.Lerp(room.boundsCells.yMin * m + m, room.boundsCells.yMax * m - m, t);
                    BuildUtils.SpawnBox(parent, $"CeilingBeam_X_{i}",
                        new Vector3(center.x, center.y, z),
                        new Vector3(width - m, beamThickness, beamThickness),
                        mat,
                        true);
                }
                else
                {
                    float x = Mathf.Lerp(room.boundsCells.xMin * m + m, room.boundsCells.xMax * m - m, t);
                    BuildUtils.SpawnBox(parent, $"CeilingBeam_Z_{i}",
                        new Vector3(x, center.y, center.z),
                        new Vector3(beamThickness, beamThickness, height - m),
                        mat,
                        true);
                }
            }
        }

        static void SpawnAtmospherePylon(Transform parent, string name, Vector3 position, float wh, ArenaBuildMaterials mats)
        {
            float height = Mathf.Clamp(wh * 0.38f, 2.2f, 4.5f);
            float width = 0.28f;
            BuildUtils.SpawnBox(parent, $"{name}_Core",
                position + new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, width),
                mats.prop ?? mats.wallTrim ?? mats.wall,
                false);

            if (mats.emissiveAccent != null)
            {
                BuildUtils.SpawnBox(parent, $"{name}_Glow",
                    position + new Vector3(0f, height * 0.55f, 0f),
                    new Vector3(width * 1.6f, height * 0.22f, width * 1.6f),
                    mats.emissiveAccent,
                    false);
            }

            var biome = mats.sourceBiome;
            float intensity = biome != null ? biome.accentLightIntensity * 0.6f : 1.3f;
            float range = biome != null ? Mathf.Max(1f, biome.accentLightRange * 0.85f) : 6f;
            if (intensity > 0.01f)
            {
                Color lightColor = biome != null
                    ? (biome.emissiveAccent != null && biome.emissiveAccent.emissionIntensity > 0.01f
                        ? biome.emissiveAccent.emissionColor
                        : biome.barrierColor)
                    : new Color(0.3f, 0.8f, 1f);
                if (lightColor.maxColorComponent < 0.05f) lightColor = new Color(0.3f, 0.8f, 1f);
                AttachPointLight(parent, $"{name}_Light",
                    position + new Vector3(0f, height * 0.6f, 0f),
                    lightColor, intensity, range);
            }
        }

        static void AttachPointLight(Transform parent, string name, Vector3 position,
            Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None; // keep cheap; additional-light shadows fill up fast
            light.renderMode = LightRenderMode.Auto;
        }

        static Material ResolveShopParticleMaterial()
        {
            if (s_shopParticleMaterial != null) return s_shopParticleMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            s_shopParticleMaterial = new Material(shader) { name = "ShopPlatformParticles(Runtime)" };
            if (s_shopParticleMaterial.HasProperty("_Surface")) s_shopParticleMaterial.SetFloat("_Surface", 1f);
            if (s_shopParticleMaterial.HasProperty("_Blend")) s_shopParticleMaterial.SetFloat("_Blend", 0f);
            if (s_shopParticleMaterial.HasProperty("_BaseColor"))
            {
                Color white = Color.white;
                white.a = 0.85f;
                s_shopParticleMaterial.SetColor("_BaseColor", white);
            }
            return s_shopParticleMaterial;
        }

        static void BuildContaminationPatches(
            ArenaRoomData room, ArenaRunConfig cfg, ArenaBuildMaterials mats, Transform parent)
        {
            var biome = mats.sourceBiome;
            if (biome == null || !biome.useContaminationLayer || mats.contamination == null) return;

            float m = cfg.macroCellMeters;
            bool[,] mask = room.shapeMask;
            int maxPatches = biome.contaminationStrength >= 0.65f ? 4 : biome.contaminationStrength >= 0.35f ? 3 : 2;
            int placed = 0;

            int[,] targets = new int[,]
            {
                { 1, 1 },
                { mask.GetLength(0) - 2, 1 },
                { 1, mask.GetLength(1) - 2 },
                { mask.GetLength(0) - 2, mask.GetLength(1) - 2 },
                { mask.GetLength(0) / 2, 1 },
                { mask.GetLength(0) / 2, mask.GetLength(1) - 2 }
            };

            for (int i = 0; i < targets.GetLength(0) && placed < maxPatches; i++)
            {
                if (!TryFindInteriorCell(mask, targets[i, 0], targets[i, 1], out int lx, out int ly))
                    continue;

                Vector3 floorPos = LocalCellCenter(room.boundsCells, lx, ly, m, 0.04f);
                if (IsNearPoint(floorPos, room.startSpawnPoint, m * biome.centerCleanBias * 2f))
                    continue;

                float size = Mathf.Lerp(m * 0.45f, m * 0.9f, biome.contaminationStrength);
                BuildUtils.SpawnBox(parent, $"ContaminationFloor_{placed}",
                    floorPos,
                    new Vector3(size, 0.03f, size),
                    mats.contamination,
                    false);

                BuildUtils.SpawnBox(parent, $"ContaminationCeiling_{placed}",
                    floorPos + new Vector3(0f, room.wallHeightMeters - Mathf.Max(0.08f, cfg.ceilingThicknessMeters), 0f),
                    new Vector3(size * 0.6f, 0.03f, size * 0.6f),
                    mats.contamination,
                    false);

                placed++;
            }
        }

        static bool IsNearAnyExit(Vector3 pos, List<ExitDoorAnchor> exits, float threshold)
        {
            if (exits == null) return false;
            float sqr = threshold * threshold;
            for (int i = 0; i < exits.Count; i++)
            {
                var exitPos = new Vector3(exits[i].worldCenter.x, pos.y, exits[i].worldCenter.z);
                if ((pos - exitPos).sqrMagnitude <= sqr) return true;
            }
            return false;
        }

        static bool IsNearPoint(Vector3 pos, Vector3 other, float threshold)
        {
            float sqr = threshold * threshold;
            return (new Vector3(pos.x, 0f, pos.z) - new Vector3(other.x, 0f, other.z)).sqrMagnitude <= sqr;
        }

        static bool TryFindInteriorCell(bool[,] mask, int targetX, int targetY, out int foundX, out int foundY)
        {
            if (mask == null)
            {
                foundX = foundY = 0;
                return false;
            }

            int maxRadius = Mathf.Max(mask.GetLength(0), mask.GetLength(1));
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int y = Mathf.Max(0, targetY - radius); y <= Mathf.Min(mask.GetLength(1) - 1, targetY + radius); y++)
                {
                    for (int x = Mathf.Max(0, targetX - radius); x <= Mathf.Min(mask.GetLength(0) - 1, targetX + radius); x++)
                    {
                        if (!mask[x, y]) continue;
                        foundX = x;
                        foundY = y;
                        return true;
                    }
                }
            }

            foundX = foundY = 0;
            return false;
        }

        static Vector3 LocalCellCenter(RectInt bounds, int localX, int localY, float cellSize, float y)
        {
            int wx = localX + bounds.xMin;
            int wy = localY + bounds.yMin;
            return new Vector3(wx * cellSize + cellSize * 0.5f, y, wy * cellSize + cellSize * 0.5f);
        }

        static Vector3 BoundsCenter(RectInt bounds, float cellSize, float y)
        {
            return new Vector3(
                (bounds.xMin + bounds.width * 0.5f) * cellSize,
                y,
                (bounds.yMin + bounds.height * 0.5f) * cellSize);
        }

        // Thin emissive strips along the floor where it meets exterior walls.
        // Visually breaks up the flat floor-to-wall seam and gives the arena a
        // "Doom Eternal panel" feel without needing a real trim sheet.
        static void BuildSingleEdgeStrips(
            ArenaRoomData room, ArenaRunConfig cfg, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null) return;
            Material strip = mats.emissiveAccent;
            if (strip == null) return;

            float m = cfg.macroCellMeters;
            float th = cfg.wallThicknessMeters;
            float stripHeight = 0.06f;
            float stripDepth = Mathf.Max(0.08f, th * 0.6f);
            float stripY = 0.05f; // sit just above the floor surface

            var root = new GameObject("EdgeStrips");
            root.transform.SetParent(parent, false);

            bool[,] mask = room.shapeMask;
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[x, y]) continue;
                int wx = x + room.boundsCells.xMin;
                int wy = y + room.boundsCells.yMin;
                TryEmitEdgeStrip(mask, room, x, y, 0, -1, wx, wy, m, th, stripDepth, stripHeight, stripY, strip, root.transform);
                TryEmitEdgeStrip(mask, room, x, y, 0,  1, wx, wy, m, th, stripDepth, stripHeight, stripY, strip, root.transform);
                TryEmitEdgeStrip(mask, room, x, y, -1, 0, wx, wy, m, th, stripDepth, stripHeight, stripY, strip, root.transform);
                TryEmitEdgeStrip(mask, room, x, y,  1, 0, wx, wy, m, th, stripDepth, stripHeight, stripY, strip, root.transform);
            }
        }

        static void TryEmitEdgeStrip(
            bool[,] mask, ArenaRoomData room, int x, int y, int dx, int dy, int wx, int wy,
            float m, float th, float depth, float height, float yPos, Material mat, Transform parent)
        {
            int nx = x + dx, ny = y + dy;
            int w = mask.GetLength(0), h = mask.GetLength(1);
            bool neighborInterior = nx >= 0 && ny >= 0 && nx < w && ny < h && mask[nx, ny];
            if (neighborInterior) return;
            if (IsDoorOpening(room, wx, wy, dx, dy)) return; // skip door gaps

            Vector3 c = new Vector3(wx * m + m * 0.5f, yPos, wy * m + m * 0.5f);
            Vector3 size;
            string tag;
            float inset = th + depth * 0.5f;
            if (dy == -1)      { c += new Vector3(0f, 0f, -m * 0.5f + inset); size = new Vector3(m * 0.95f, height, depth); tag = "S"; }
            else if (dy == 1)  { c += new Vector3(0f, 0f,  m * 0.5f - inset); size = new Vector3(m * 0.95f, height, depth); tag = "N"; }
            else if (dx == -1) { c += new Vector3(-m * 0.5f + inset, 0f, 0f); size = new Vector3(depth, height, m * 0.95f); tag = "W"; }
            else               { c += new Vector3( m * 0.5f - inset, 0f, 0f); size = new Vector3(depth, height, m * 0.95f); tag = "E"; }

            BuildUtils.SpawnBox(parent, $"EdgeStrip_{wx}_{wy}_{tag}", c, size, mat, false);
        }

        // Ceiling-mounted spotlights pointing straight down. We use Spot rather
        // than Point because point lights at 10–15 m above the floor lose 95 %
        // of their intensity to inverse-square falloff before reaching player
        // height. A 110° spot focuses the cone toward the playable area instead.
        // Intensity is intentionally moderate (1.6 × biome.accentLightIntensity)
        // so it illuminates without blowing out — the actual visual punch
        // comes from contrast vs. the ambient/post-fx tint.
        static void BuildSingleFillLights(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, ArenaBuildMaterials mats, Transform parent)
        {
            if (mats == null) return;
            var biome = mats.sourceBiome;
            float baseIntensity = biome != null ? biome.accentLightIntensity : 2.2f;
            float fillIntensity = Mathf.Max(1.65f, baseIntensity * 1.05f);
            float fillRange = wh + 6f;       // reach the floor with margin
            float spotAngle = 110f;          // wide enough to overlap neighbours

            float m = cfg.macroCellMeters;
            float spanX = room.boundsCells.width * m;
            float spanZ = room.boundsCells.height * m;

            // Color: warm-neutral leaning toward biome ambient tint, kept low-saturation
            // so it doesn't fight the biome ColorAdjustments tint pushed via post-fx.
            Color tint = biome != null ? biome.ambientTint : new Color(0.85f, 0.88f, 0.95f);
            Color fillColor = Color.Lerp(new Color(0.95f, 0.95f, 0.95f), tint, 0.25f);
            if (fillColor.maxColorComponent < 0.05f) fillColor = new Color(0.9f, 0.92f, 0.95f);

            var root = new GameObject("FillLights");
            root.transform.SetParent(parent, false);

            // Mount spots just below the lamp panel so the cone starts from the fixture.
            float spotY = wh - 0.45f;
            Vector3 center = BoundsCenter(room.boundsCells, m, spotY);

            AttachCeilingSpot(root.transform, "FillLight_Center", center, fillColor, fillIntensity, fillRange, spotAngle);
            SpawnCeilingLamp(root.transform, "CeilingLamp_Center", center, wh, fillColor, mats);

            float quadInset = 0.28f;
            float qx = spanX * (0.5f - quadInset);
            float qz = spanZ * (0.5f - quadInset);
            if (spanX >= m * 6f && spanZ >= m * 6f)
            {
                float quadIntensity = fillIntensity * 0.72f;
                Vector3[] offsets = {
                    new Vector3( qx, 0f,  qz),
                    new Vector3(-qx, 0f,  qz),
                    new Vector3( qx, 0f, -qz),
                    new Vector3(-qx, 0f, -qz),
                };
                string[] tags = { "NE", "NW", "SE", "SW" };
                for (int i = 0; i < 4; i++)
                {
                    Vector3 pos = center + offsets[i];
                    AttachCeilingSpot(root.transform, $"FillLight_{tags[i]}", pos, fillColor, quadIntensity, fillRange, spotAngle);
                    SpawnCeilingLamp(root.transform, $"CeilingLamp_{tags[i]}", pos, wh, fillColor, mats);
                }
            }
        }

        static void AttachCeilingSpot(Transform parent, string name, Vector3 position,
            Color color, float intensity, float range, float spotAngle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // point straight down
            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.spotAngle = spotAngle;
            light.innerSpotAngle = spotAngle * 0.55f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            // PR 5.C — auto-attach LampFlicker so nearby impacts can dim it.
            go.AddComponent<LampFlicker>();
        }

        // Visible ceiling fixture: a small emissive panel mounted to the ceiling
        // above the corresponding fill light, plus a tiny mounting bracket cube.
        // No extra Light component — the underlying fill point light already
        // provides the illumination, this is purely visual so the player can
        // see *where* the light is coming from.
        static void SpawnCeilingLamp(
            Transform parent, string name, Vector3 fillLightPos, float wh, Color tint, ArenaBuildMaterials mats)
        {
            if (mats == null) return;
            Material panelMat = mats.lampPanel ?? mats.emissiveAccent;
            if (panelMat == null) return;

            // Mount the lamp 4cm below the actual ceiling tile to avoid z-fight
            // with `Ceiling_*` floors-in-reverse (those have their bottom face
            // exactly at wh).
            float lampTopY = wh - 0.04f;
            float panelSize = 1.55f;           // readable from the floor without blowing out bloom
            float panelThickness = 0.10f;
            float bracketThickness = 0.08f;
            float bracketSize = panelSize + 0.25f;

            // Bracket: dark frame flush to the ceiling. Top at lampTopY.
            Material bracketMat = mats.ceiling ?? mats.wall;
            if (bracketMat != null)
            {
                float bracketCenterY = lampTopY - bracketThickness * 0.5f;
                BuildUtils.SpawnBox(parent, $"{name}_Bracket",
                    new Vector3(fillLightPos.x, bracketCenterY, fillLightPos.z),
                    new Vector3(bracketSize, bracketThickness, bracketSize),
                    bracketMat,
                    false);
            }

            // Emissive panel hangs just under the bracket.
            float panelTopY = lampTopY - bracketThickness;
            float panelCenterY = panelTopY - panelThickness * 0.5f;
            BuildUtils.SpawnBox(parent, $"{name}_Panel",
                new Vector3(fillLightPos.x, panelCenterY, fillLightPos.z),
                new Vector3(panelSize, panelThickness, panelSize),
                panelMat,
                false);
        }

        static void BuildSingleAnchors(ArenaRoomData room, ArenaRunConfig cfg, float wh, Transform parent)
        {
            var anchorsRoot = new GameObject("Anchors");
            anchorsRoot.transform.SetParent(parent, false);
            float m = cfg.macroCellMeters;
            var b = room.boundsCells;
            var mask = room.shapeMask;
            int w = mask.GetLength(0), h = mask.GetLength(1);

            var walls = new GameObject("WallAnchors").transform;
            walls.SetParent(anchorsRoot.transform, false);
            var floor = new GameObject("FloorAnchors").transform;
            floor.SetParent(anchorsRoot.transform, false);
            var ceiling = new GameObject("CeilingAnchors").transform;
            ceiling.SetParent(anchorsRoot.transform, false);

            float spacing = Mathf.Max(cfg.ceilingAnchorSpacingMeters, 1f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[x, y]) continue;
                int wx = x + b.xMin, wy = y + b.yMin;
                float cx = wx * m + m * 0.5f;
                float cz = wy * m + m * 0.5f;
                SpawnAnchor(floor, $"F_{wx}_{wy}", new Vector3(cx, 0f, cz));
                // ceiling anchor on spacing grid
                if ((wx * m) % spacing < m && (wy * m) % spacing < m)
                    SpawnAnchor(ceiling, $"C_{wx}_{wy}", new Vector3(cx, wh, cz));
                // wall anchors on mask boundary
                TryWallAnchor(walls, mask, x, y, 0, -1, wx, wy, m, "S");
                TryWallAnchor(walls, mask, x, y, 0,  1, wx, wy, m, "N");
                TryWallAnchor(walls, mask, x, y, -1, 0, wx, wy, m, "W");
                TryWallAnchor(walls, mask, x, y,  1, 0, wx, wy, m, "E");
            }
        }

        static void TryWallAnchor(
            Transform parent, bool[,] mask, int x, int y, int dx, int dy,
            int wx, int wy, float m, string tag)
        {
            int nx = x + dx, ny = y + dy;
            int w = mask.GetLength(0), h = mask.GetLength(1);
            bool neighborInterior = nx >= 0 && ny >= 0 && nx < w && ny < h && mask[nx, ny];
            if (neighborInterior) return;
            Vector3 c = new Vector3(wx * m + m * 0.5f, 0f, wy * m + m * 0.5f);
            if (dy == -1) c += new Vector3(0f, 0f, -m * 0.5f);
            else if (dy == 1) c += new Vector3(0f, 0f, m * 0.5f);
            else if (dx == -1) c += new Vector3(-m * 0.5f, 0f, 0f);
            else c += new Vector3(m * 0.5f, 0f, 0f);
            SpawnAnchor(parent, $"{tag}_{wx}_{wy}", c);
        }

        static void SpawnAnchor(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
        }

        public static GameObject Build(
            ArenaRuntimeContext ctx, ArenaRunConfig cfg, Transform parent, ArenaBuildMaterials mats = null)
        {
            if (ctx == null || ctx.layout == null || cfg == null) return null;
            if (parent == null) return null;
            if (mats == null) mats = ArenaBuildMaterials.CreateDefaults();

            Clear(parent);

            var layout = ctx.layout;
            var outer = layout.outerBoundsCells;
            var grid = ArenaOccupancy.Build(layout, cfg.corridorWidthCells);

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;

            var roomRoots = new Dictionary<int, Transform>();
            var roomShells = new Dictionary<int, Transform>();
            foreach (var r in layout.rooms)
            {
                var rgo = RoomBlockoutBuilder.BuildRoomContainer(r, root.transform);
                roomRoots[r.id] = rgo.transform;
                var shell = new GameObject("Shell");
                shell.transform.SetParent(rgo.transform, false);
                roomShells[r.id] = shell.transform;
            }

            var corridorsRoot = new GameObject("Corridors");
            corridorsRoot.transform.SetParent(root.transform, false);
            var corridorShell = new GameObject("Shell");
            corridorShell.transform.SetParent(corridorsRoot.transform, false);

            BuildShell(grid, outer, cfg, mats, roomShells, corridorShell.transform);

            foreach (var r in layout.rooms)
            {
                Material marker = r.id == layout.startRoomId ? mats.startMarker
                               : r.id == layout.exitRoomId  ? mats.exitMarker
                               : null;
                if (marker != null)
                    RoomBlockoutBuilder.BuildMarker(r, cfg, roomRoots[r.id], marker);
                RoomBlockoutBuilder.BuildCover(r, cfg, roomRoots[r.id], mats.cover, ctx.spawnRng, grid, outer);
                RoomBlockoutBuilder.BuildAnchors(r, cfg, roomRoots[r.id]);
            }

            for (int i = 0; i < layout.corridors.Count; i++)
            {
                var cgo = CorridorBlockoutBuilder.BuildCorridorContainer(i, corridorsRoot.transform);
                CorridorBlockoutBuilder.BuildAnchors(layout.corridors[i], cfg, cgo.transform, outer, grid);
            }

            return root;
        }

        public static void Clear(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child.name == RootName)
                {
                    if (Application.isPlaying) Object.Destroy(child.gameObject);
                    else Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        static void BuildShell(
            ArenaOccupancy grid, RectInt outer, ArenaRunConfig cfg, ArenaBuildMaterials mats,
            Dictionary<int, Transform> roomShells, Transform corridorShell)
        {
            float m = cfg.macroCellMeters;
            float wh = cfg.wallHeightMeters;
            float ft = cfg.floorThicknessMeters;
            float ct = cfg.ceilingThicknessMeters;
            float th = cfg.wallThicknessMeters;

            for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                if (!grid.IsInterior(x, y)) continue;
                Transform shell = ShellForCell(grid, x, y, roomShells, corridorShell);
                int wx = x + outer.xMin, wy = y + outer.yMin;
                float cx = wx * m + m * 0.5f;
                float cz = wy * m + m * 0.5f;

                BuildUtils.SpawnBox(shell, $"Floor_{wx}_{wy}",
                    new Vector3(cx, -ft * 0.5f, cz),
                    new Vector3(m, ft, m), mats.floor, true);

                BuildUtils.SpawnBox(shell, $"Ceiling_{wx}_{wy}",
                    new Vector3(cx, wh + ct * 0.5f, cz),
                    new Vector3(m, ct, m), mats.ceiling, true);

                TryEmitWall(grid, x, y,  0, -1, m, wh, th, mats.wall, shell, outer);
                TryEmitWall(grid, x, y,  0,  1, m, wh, th, mats.wall, shell, outer);
                TryEmitWall(grid, x, y, -1,  0, m, wh, th, mats.wall, shell, outer);
                TryEmitWall(grid, x, y,  1,  0, m, wh, th, mats.wall, shell, outer);
            }
        }

        static Transform ShellForCell(
            ArenaOccupancy grid, int x, int y, Dictionary<int, Transform> roomShells, Transform corridorShell)
        {
            int idx = y * grid.width + x;
            var k = grid.kind[idx];
            if (k == CellKind.Room)
            {
                int rid = grid.roomId[idx];
                if (roomShells.TryGetValue(rid, out var t)) return t;
            }
            return corridorShell;
        }

        static void TryEmitWall(
            ArenaOccupancy grid, int x, int y, int dx, int dy,
            float m, float wh, float th, Material wallMat, Transform shell, RectInt outer)
        {
            int nx = x + dx, ny = y + dy;
            var myKind = grid.Get(x, y);
            var nKind = grid.Get(nx, ny);
            bool emit = false;

            if (nKind == CellKind.Empty) emit = true;
            else if (myKind == CellKind.Room && nKind == CellKind.Room)
            {
                int myId = grid.GetRoomId(x, y);
                int nId  = grid.GetRoomId(nx, ny);
                if (myId != nId && myId < nId) emit = true;
            }

            if (!emit) return;

            int wx = x + outer.xMin, wy = y + outer.yMin;
            Vector3 c = new Vector3(wx * m + m * 0.5f, wh * 0.5f, wy * m + m * 0.5f);
            Vector3 size;
            string tag;

            if (dy == -1)      { c += new Vector3(0f, 0f, -m * 0.5f + th * 0.5f); size = new Vector3(m, wh, th); tag = "S"; }
            else if (dy == 1)  { c += new Vector3(0f, 0f,  m * 0.5f - th * 0.5f); size = new Vector3(m, wh, th); tag = "N"; }
            else if (dx == -1) { c += new Vector3(-m * 0.5f + th * 0.5f, 0f, 0f); size = new Vector3(th, wh, m); tag = "W"; }
            else               { c += new Vector3( m * 0.5f - th * 0.5f, 0f, 0f); size = new Vector3(th, wh, m); tag = "E"; }

            BuildUtils.SpawnBox(shell, $"Wall_{wx}_{wy}_{tag}", c, size, wallMat, true);
        }
    }
}
