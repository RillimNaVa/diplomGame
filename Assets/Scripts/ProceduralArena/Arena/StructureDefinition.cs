using System;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    // Phase 2 / PR 2.H1 — hand-authored cover structure model.
    // A StructureDefinition is a list of axis-aligned BoxParts that compose
    // into a single recognizable silhouette (bunker, pillar cluster, sandbag
    // line, sniper nest). The builder spawns each part via BuildUtils.SpawnBox
    // so WorldUVScaler / per-biome materials / instancing all "just work".
    //
    // Determinism: planning uses a dedicated structureRng sub-stream in
    // SingleArenaGenerator (no UnityEngine.Random). Yaw is quantized to 90°
    // increments so every part stays axis-aligned to the cell grid.
    //
    // v1 ships built-in defaults from BuiltInStructures so existing arenas
    // get structures without anyone authoring assets in Editor. Future SOs
    // can extend the same model.

    public enum StructureSlot
    {
        Wall,
        Cover,
        Trim,
        Floor,
        Decor,
        EmissiveAccent
    }

    [Serializable]
    public struct StructureBoxPart
    {
        // Local-space offset relative to the structure pivot (XZ plane), Y is
        // measured from floor (y=0). Y of `size` is the height; the spawn
        // helper pushes the box up by size.y/2 internally.
        public Vector3 localOffset;
        public Vector3 size;
        public StructureSlot slot;
    }

    [Serializable]
    public class StructureDefinition
    {
        public string structureId;
        public StructureBoxPart[] parts;
        // Cells reserved on the macro-cell grid so the planner does not place
        // cover, spawns, or another structure on top.
        public Vector2Int footprintCells;
        // Categories that may host this structure. Empty / null = all combat.
        public ArenaCategory[] eligibleCategories;
        // Bias for the picker when multiple structures are eligible.
        public float weight;

        public bool IsEligible(ArenaCategory category)
        {
            if (eligibleCategories == null || eligibleCategories.Length == 0)
            {
                // Default: any combat-style arena. Skip Start / Shop / Rest.
                return category == ArenaCategory.Combat
                    || category == ArenaCategory.Elite
                    || category == ArenaCategory.Boss
                    || category == ArenaCategory.Parkour;
            }
            for (int i = 0; i < eligibleCategories.Length; i++)
                if (eligibleCategories[i] == category) return true;
            return false;
        }
    }

    [Serializable]
    public struct StructurePlacement
    {
        public string structureId;
        public Vector3 position;     // world-space pivot at floor level
        public float yawDeg;         // rotation around Y (0/90/180/270)
        public Vector2Int footprintCells;
    }
}
