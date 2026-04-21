using System;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    [Serializable]
    public struct CoverPlacement
    {
        public Vector3 position;
        public Vector3 size;
        public float yawDeg;
    }

    [Serializable]
    public struct ExitDoorAnchor
    {
        public Vector2Int cell;
        public Vector2Int outwardDir;
        public Vector3 worldCenter;
        public float yawDeg;
    }

    [Serializable]
    public struct PlatformPlacement
    {
        public Vector3 center;
        public Vector3 size;
        public float heightMeters;
    }
}
