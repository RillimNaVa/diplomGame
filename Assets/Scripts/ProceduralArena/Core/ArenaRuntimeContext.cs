using System;
using System.Collections.Generic;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.Core
{
    public class ArenaRuntimeContext
    {
        public int masterSeed;
        public System.Random layoutRng;
        public System.Random roomRng;
        public System.Random corridorRng;
        public System.Random spawnRng;
        public System.Random biomeRng;

        public ArenaLayout layout;
        public long generationTimeMs;
        public int attemptsUsed;
        public bool usedHandCodedFallback;
        public List<string> warnings = new List<string>();

        public static ArenaRuntimeContext Create(int masterSeed)
        {
            var ctx = new ArenaRuntimeContext { masterSeed = masterSeed };
            ctx.layoutRng   = new System.Random(unchecked(masterSeed ^ 0x1A2B));
            ctx.roomRng     = new System.Random(unchecked(masterSeed ^ 0x3C4D));
            ctx.corridorRng = new System.Random(unchecked(masterSeed ^ 0x5E6F));
            ctx.spawnRng    = new System.Random(unchecked(masterSeed ^ 0x7081));
            ctx.biomeRng    = new System.Random(unchecked(masterSeed ^ 0x9293));
            return ctx;
        }

        public void Warn(string message) => warnings.Add(message);
    }
}
