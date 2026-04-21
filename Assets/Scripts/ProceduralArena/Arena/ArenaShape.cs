using System;

namespace VoidSurvivor.ProceduralArena.Arena
{
    public enum ArenaShape
    {
        Rect = 0,
        LShape = 1,
        TShape = 2,
        Octagon = 3
    }

    [Serializable]
    public struct ShapeWeight
    {
        public ArenaShape shape;
        public float weight;
    }
}
