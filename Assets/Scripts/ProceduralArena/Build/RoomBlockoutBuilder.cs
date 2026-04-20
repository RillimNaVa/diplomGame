using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Build
{
    public static class RoomBlockoutBuilder
    {
        public static GameObject BuildRoomContainer(ArenaRoomData room, Transform parent)
        {
            var go = new GameObject($"Room_{room.id}_{room.type}");
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void BuildMarker(
            ArenaRoomData room, ArenaRunConfig cfg, Transform roomRoot, Material mat)
        {
            if (mat == null) return;
            float m = cfg.macroCellMeters;
            Vector3 center = BuildUtils.CellCornerToWorld(room.CenterCell, m) + new Vector3(m * 0.5f, 0.01f, m * 0.5f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(roomRoot, false);
            marker.transform.position = center + new Vector3(0f, 0.05f, 0f);
            marker.transform.localScale = new Vector3(m * 0.8f, 0.1f, m * 0.8f);
            var rend = marker.GetComponent<MeshRenderer>();
            if (rend != null) rend.sharedMaterial = mat;
            var col = marker.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        public static void BuildCover(
            ArenaRoomData room, ArenaRunConfig cfg, Transform roomRoot,
            Material coverMat, System.Random spawnRng, ArenaOccupancy grid, RectInt outer)
        {
            if (!IsCombatRoom(room.type)) return;
            if (cfg.coverDensity <= 0f) return;

            float areaMacro = room.boundsCells.width * room.boundsCells.height;
            int count = Mathf.RoundToInt((areaMacro / 100f) * cfg.coverDensity * 10f);
            if (count <= 0) return;

            var coverRoot = new GameObject("Cover");
            coverRoot.transform.SetParent(roomRoot, false);

            float m = cfg.macroCellMeters;
            var placed = new List<Vector3>();
            int attempts = 0;
            int maxAttempts = count * 12;

            while (placed.Count < count && attempts < maxAttempts)
            {
                attempts++;
                int cx = spawnRng.Next(room.boundsCells.xMin + 1, room.boundsCells.xMax - 1);
                int cy = spawnRng.Next(room.boundsCells.yMin + 1, room.boundsCells.yMax - 1);
                if (IsNearDoor(room, cx, cy)) continue;
                if (grid.GetRoomId(cx - outer.xMin, cy - outer.yMin) != room.id) continue;

                float range = Mathf.Max(0f, m - cfg.coverWidthMeters);
                float mx = (float)spawnRng.NextDouble() * range;
                float mz = (float)spawnRng.NextDouble() * range;
                Vector3 pos = new Vector3(
                    cx * m + mx + cfg.coverWidthMeters * 0.5f,
                    cfg.coverHeightMeters * 0.5f,
                    cy * m + mz + cfg.coverWidthMeters * 0.5f);

                bool tooClose = false;
                foreach (var p in placed)
                {
                    if ((p - pos).sqrMagnitude < cfg.coverMinSpacingMeters * cfg.coverMinSpacingMeters)
                    { tooClose = true; break; }
                }
                if (tooClose) continue;

                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"Cover_{placed.Count}";
                pillar.transform.SetParent(coverRoot.transform, false);
                pillar.transform.position = pos;
                pillar.transform.localScale = new Vector3(cfg.coverWidthMeters, cfg.coverHeightMeters, cfg.coverWidthMeters);
                var rend = pillar.GetComponent<MeshRenderer>();
                if (rend != null && coverMat != null) rend.sharedMaterial = coverMat;
                placed.Add(pos);
            }
        }

        public static void BuildAnchors(ArenaRoomData room, ArenaRunConfig cfg, Transform roomRoot)
        {
            if (!cfg.generateAnchors) return;
            var anchorsRoot = new GameObject("Anchors");
            anchorsRoot.transform.SetParent(roomRoot, false);

            float m = cfg.macroCellMeters;
            var b = room.boundsCells;

            // Corner anchors
            var corners = new GameObject("CornerAnchors").transform;
            corners.SetParent(anchorsRoot.transform, false);
            SpawnAnchor(corners, "CornerSW", new Vector3(b.xMin * m, 0f, b.yMin * m));
            SpawnAnchor(corners, "CornerSE", new Vector3(b.xMax * m, 0f, b.yMin * m));
            SpawnAnchor(corners, "CornerNW", new Vector3(b.xMin * m, 0f, b.yMax * m));
            SpawnAnchor(corners, "CornerNE", new Vector3(b.xMax * m, 0f, b.yMax * m));

            // Wall anchors along each edge per macro cell (skip door cells)
            var walls = new GameObject("WallAnchors").transform;
            walls.SetParent(anchorsRoot.transform, false);
            for (int x = b.xMin; x < b.xMax; x++)
            {
                if (!IsDoorCell(room, x, b.yMin))
                    SpawnAnchor(walls, $"WS_{x}", new Vector3(x * m + m * 0.5f, 0f, b.yMin * m));
                if (!IsDoorCell(room, x, b.yMax - 1))
                    SpawnAnchor(walls, $"WN_{x}", new Vector3(x * m + m * 0.5f, 0f, b.yMax * m));
            }
            for (int y = b.yMin; y < b.yMax; y++)
            {
                if (!IsDoorCell(room, b.xMin, y))
                    SpawnAnchor(walls, $"WW_{y}", new Vector3(b.xMin * m, 0f, y * m + m * 0.5f));
                if (!IsDoorCell(room, b.xMax - 1, y))
                    SpawnAnchor(walls, $"WE_{y}", new Vector3(b.xMax * m, 0f, y * m + m * 0.5f));
            }

            // Ceiling anchors grid
            var ceiling = new GameObject("CeilingAnchors").transform;
            ceiling.SetParent(anchorsRoot.transform, false);
            float spacing = Mathf.Max(cfg.ceilingAnchorSpacingMeters, 1f);
            int stepsX = Mathf.Max(1, Mathf.FloorToInt(b.width * m / spacing));
            int stepsY = Mathf.Max(1, Mathf.FloorToInt(b.height * m / spacing));
            for (int iy = 0; iy < stepsY; iy++)
            for (int ix = 0; ix < stepsX; ix++)
            {
                float px = b.xMin * m + (ix + 0.5f) * (b.width * m / stepsX);
                float pz = b.yMin * m + (iy + 0.5f) * (b.height * m / stepsY);
                SpawnAnchor(ceiling, $"C_{ix}_{iy}", new Vector3(px, cfg.wallHeightMeters, pz));
            }

            // Floor anchors (1 per interior macro cell)
            var floor = new GameObject("FloorAnchors").transform;
            floor.SetParent(anchorsRoot.transform, false);
            for (int y = b.yMin; y < b.yMax; y++)
            for (int x = b.xMin; x < b.xMax; x++)
                SpawnAnchor(floor, $"F_{x}_{y}", new Vector3(x * m + m * 0.5f, 0f, y * m + m * 0.5f));

            // Door frame anchors
            var doors = new GameObject("DoorFrameAnchors").transform;
            doors.SetParent(anchorsRoot.transform, false);
            int d = 0;
            foreach (var door in room.doorAnchorsCells)
            {
                Vector3 worldCenter = new Vector3(door.x * m + m * 0.5f, 0f, door.y * m + m * 0.5f);
                SpawnAnchor(doors, $"D{d}_L", worldCenter + new Vector3(-m * 0.5f, 0f, 0f));
                SpawnAnchor(doors, $"D{d}_R", worldCenter + new Vector3( m * 0.5f, 0f, 0f));
                d++;
            }
        }

        static void SpawnAnchor(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
        }

        public static bool IsCombatRoom(RoomType t)
        {
            return t == RoomType.CombatSmall || t == RoomType.CombatMedium || t == RoomType.CombatLarge;
        }

        static bool IsDoorCell(ArenaRoomData room, int x, int y)
        {
            foreach (var d in room.doorAnchorsCells)
                if (d.x == x && d.y == y) return true;
            return false;
        }

        static bool IsNearDoor(ArenaRoomData room, int x, int y)
        {
            foreach (var d in room.doorAnchorsCells)
            {
                if (Mathf.Abs(d.x - x) + Mathf.Abs(d.y - y) <= 1) return true;
            }
            return false;
        }
    }
}
