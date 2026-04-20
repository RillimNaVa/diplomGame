// ============================================================================
// [DEPRECATED] Phase 2 r4 pivot (2026-04-20). No corridors in active pipeline.
// Kept for reference only — see ARENA_GENERATION_TZ.md (r4).
// ============================================================================
using UnityEngine;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Build
{
    public static class CorridorBlockoutBuilder
    {
        public static GameObject BuildCorridorContainer(int index, Transform parent)
        {
            var go = new GameObject($"Corridor_{index}");
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void BuildAnchors(
            ArenaCorridorData corridor, ArenaRunConfig cfg, Transform corridorRoot, RectInt outer, ArenaOccupancy grid)
        {
            if (!cfg.generateAnchors) return;
            var anchors = new GameObject("Anchors");
            anchors.transform.SetParent(corridorRoot, false);
            float m = cfg.macroCellMeters;

            // Wall anchors for every corridor cell whose side faces empty
            foreach (var cell in corridor.pathCells)
            {
                int cx = cell.x - outer.xMin;
                int cy = cell.y - outer.yMin;
                if (!grid.InBounds(cx, cy)) continue;
                Vector3 center = new Vector3(cell.x * m + m * 0.5f, 0f, cell.y * m + m * 0.5f);
                if (grid.Get(cx, cy - 1) == CellKind.Empty)
                    SpawnAnchor(anchors.transform, $"WS_{cell.x}_{cell.y}", center + new Vector3(0f, 0f, -m * 0.5f));
                if (grid.Get(cx, cy + 1) == CellKind.Empty)
                    SpawnAnchor(anchors.transform, $"WN_{cell.x}_{cell.y}", center + new Vector3(0f, 0f,  m * 0.5f));
                if (grid.Get(cx - 1, cy) == CellKind.Empty)
                    SpawnAnchor(anchors.transform, $"WW_{cell.x}_{cell.y}", center + new Vector3(-m * 0.5f, 0f, 0f));
                if (grid.Get(cx + 1, cy) == CellKind.Empty)
                    SpawnAnchor(anchors.transform, $"WE_{cell.x}_{cell.y}", center + new Vector3( m * 0.5f, 0f, 0f));
            }
        }

        static void SpawnAnchor(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
        }
    }
}
