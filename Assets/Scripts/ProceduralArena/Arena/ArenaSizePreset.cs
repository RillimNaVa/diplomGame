using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    public enum ArenaSizePreset
    {
        S = 0,
        M = 1,
        L = 2
    }

    public static class ArenaSizePresetEx
    {
        public static Vector2Int BaseCells(this ArenaSizePreset p)
        {
            switch (p)
            {
                case ArenaSizePreset.S: return new Vector2Int(10, 10);
                case ArenaSizePreset.L: return new Vector2Int(20, 20);
                case ArenaSizePreset.M:
                default: return new Vector2Int(15, 15);
            }
        }
    }
}
