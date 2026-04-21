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
            BuildSingleCover(room, mats.cover, root.transform);
            BuildSingleExits(room, cfg, wh, mats.exitMarker, root.transform);
            BuildSingleStartMarker(room, cfg, mats.startMarker, root.transform);
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

        static void BuildSingleExits(
            ArenaRoomData room, ArenaRunConfig cfg, float wh, Material exitMat, Transform parent)
        {
            if (room.exitDoorAnchors.Count == 0) return;
            var exitsRoot = new GameObject("Exits");
            exitsRoot.transform.SetParent(parent, false);
            float m = cfg.macroCellMeters;
            float doorHeight = Mathf.Min(wh * 0.7f, 5f);
            float doorWidth = m * 0.9f;
            float doorThickness = 0.05f;
            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var a = room.exitDoorAnchors[i];
                Vector3 size;
                if (a.outwardDir.x != 0)
                    size = new Vector3(doorThickness, doorHeight, doorWidth);
                else
                    size = new Vector3(doorWidth, doorHeight, doorThickness);
                Vector3 center = new Vector3(a.worldCenter.x, doorHeight * 0.5f, a.worldCenter.z);
                var go = BuildUtils.SpawnBox(exitsRoot.transform, $"Exit_{i}", center, size, exitMat, false);
                var anchor = new GameObject($"ExitAnchor_{i}");
                anchor.transform.SetParent(exitsRoot.transform, false);
                anchor.transform.position = new Vector3(a.worldCenter.x, 0f, a.worldCenter.z);
                anchor.transform.rotation = Quaternion.Euler(0f, a.yawDeg, 0f);
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
