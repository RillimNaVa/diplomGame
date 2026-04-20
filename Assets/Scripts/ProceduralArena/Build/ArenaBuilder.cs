using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Build
{
    public static class ArenaBuilder
    {
        public const string RootName = "ArenaRoot";

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
