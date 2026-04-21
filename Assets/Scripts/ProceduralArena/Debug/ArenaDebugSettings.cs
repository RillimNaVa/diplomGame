using System;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.DebugTools
{
    [Serializable]
    public class ArenaDebugSettings
    {
        public bool drawOuterBounds = true;
        public bool drawBspLeaves = true;
        public bool drawRooms = true;
        public bool drawCorridors = true;
        public bool drawDoors = true;
        public bool drawLabels = true;

        public Color outerColor = new Color(1f, 1f, 1f, 0.25f);
        public Color leafColor = new Color(0.3f, 0.6f, 1f, 0.35f);
        public Color roomColor = new Color(0.2f, 0.9f, 0.3f, 0.5f);
        public Color startColor = new Color(0.4f, 1f, 0.4f, 0.8f);
        public Color exitColor = new Color(1f, 0.4f, 0.4f, 0.8f);
        public Color corridorColor = new Color(1f, 0.8f, 0.2f, 0.7f);
        public Color doorColor = new Color(1f, 0.2f, 0.9f, 0.9f);

        [Header("r4 single-arena gizmos")]
        public bool drawShapeMask = true;
        public bool drawArenaExits = true;
        public bool drawCoverPlacements = true;
        public bool drawStartSpawn = true;
        public bool drawCombatSpawns = true;
        public Color shapeMaskColor = new Color(0.2f, 0.6f, 1f, 0.35f);
        public Color arenaExitColor = new Color(1f, 0.4f, 0.2f, 1f);
        public Color coverGizmoColor = new Color(0.9f, 0.8f, 0.3f, 0.8f);
        public Color startSpawnColor = new Color(0.3f, 1f, 0.5f, 1f);
        public Color combatSpawnColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    }
}
