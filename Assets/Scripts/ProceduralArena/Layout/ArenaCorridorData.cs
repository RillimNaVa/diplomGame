// ============================================================================
// [DEPRECATED] Phase 2 r4 pivot (2026-04-20). Corridors are no longer part of
// the active generator. Kept for reference only — see ARENA_GENERATION_TZ.md (r4).
// ============================================================================
using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Layout
{
    public class ArenaCorridorData
    {
        public int roomAId;
        public int roomBId;
        public int widthCells;
        public readonly List<Vector2Int> pathCells = new List<Vector2Int>();
    }
}
