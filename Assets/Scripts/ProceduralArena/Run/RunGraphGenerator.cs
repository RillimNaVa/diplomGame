using System;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Deterministic procedural run-graph builder. 5 stages,
    /// 1 + 2 + 2 + 2 + 1 = 8 nodes. Player traverses 5 arenas.
    ///
    /// Pipeline:
    ///   1. Start node (fixed profile)
    ///   2. For Mid1/Mid2/Mid3: two nodes from the corresponding stage-pool.
    ///      Each child node gets its own arenaSeed from the master runRng.
    ///   3. Boss node (fixed profile).
    ///
    /// All children of a given parent point at the same Mid-N nodes
    /// (door-choice reveals the two alternatives; picking either advances
    /// to its own subtree). For PR 2.B we keep the subtree simple — every
    /// parent exposes the same two Mid-N alternatives, then those two Mid-N
    /// nodes share the same Mid-(N+1) pair, and so on. This keeps the run
    /// length fixed at 5 without exploding into 16 nodes per stage.
    /// </summary>
    public static class RunGraphGenerator
    {
        public static RunGraph Build(int runSeed, RunConfig cfg)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (cfg.startProfile == null) throw new InvalidOperationException("RunConfig.startProfile is null.");
            if (cfg.bossProfile == null) throw new InvalidOperationException("RunConfig.bossProfile is null.");

            int seed = runSeed != 0 ? runSeed : MakeTimeSeed();
            var rng = new System.Random(unchecked(seed ^ 0x5E6D));

            var graph = new RunGraph { runSeed = seed };
            int nextId = 0;

            var start = new RunGraphNode
            {
                id = nextId++,
                stage = RunStage.Start,
                arenaIndex = 0,
                arenaSeed = rng.Next(int.MinValue, int.MaxValue),
                typeProfile = cfg.startProfile
            };
            graph.nodes.Add(start);
            graph.startNode = start;

            var mid1 = BuildMidPair(cfg.mid1Pool, RunStage.Mid1, 1, ref nextId, rng, graph, cfg.startProfile);
            var mid2 = BuildMidPair(cfg.mid2Pool, RunStage.Mid2, 2, ref nextId, rng, graph, cfg.startProfile);
            var mid3 = BuildMidPair(cfg.mid3Pool, RunStage.Mid3, 3, ref nextId, rng, graph, cfg.startProfile);

            var boss = new RunGraphNode
            {
                id = nextId++,
                stage = RunStage.Boss,
                arenaIndex = 4,
                arenaSeed = rng.Next(int.MinValue, int.MaxValue),
                typeProfile = cfg.bossProfile
            };
            graph.nodes.Add(boss);

            // wire: start -> mid1 pair; each mid1 -> mid2 pair; each mid2 -> mid3 pair; each mid3 -> boss
            start.children.Add(mid1.a); start.children.Add(mid1.b);
            mid1.a.children.Add(mid2.a); mid1.a.children.Add(mid2.b);
            mid1.b.children.Add(mid2.a); mid1.b.children.Add(mid2.b);
            mid2.a.children.Add(mid3.a); mid2.a.children.Add(mid3.b);
            mid2.b.children.Add(mid3.a); mid2.b.children.Add(mid3.b);
            mid3.a.children.Add(boss);
            mid3.b.children.Add(boss);

            return graph;
        }

        struct Pair { public RunGraphNode a, b; }

        static Pair BuildMidPair(
            ArenaTypeProfile[] pool, RunStage stage, int arenaIndex,
            ref int nextId, System.Random rng, RunGraph graph, ArenaTypeProfile fallback)
        {
            var a = new RunGraphNode
            {
                id = nextId++,
                stage = stage,
                arenaIndex = arenaIndex,
                arenaSeed = rng.Next(int.MinValue, int.MaxValue),
                typeProfile = PickProfile(pool, rng, fallback)
            };
            var b = new RunGraphNode
            {
                id = nextId++,
                stage = stage,
                arenaIndex = arenaIndex,
                arenaSeed = rng.Next(int.MinValue, int.MaxValue),
                typeProfile = PickProfile(pool, rng, fallback, avoid: a.typeProfile)
            };
            graph.nodes.Add(a);
            graph.nodes.Add(b);
            return new Pair { a = a, b = b };
        }

        static ArenaTypeProfile PickProfile(
            ArenaTypeProfile[] pool, System.Random rng, ArenaTypeProfile fallback, ArenaTypeProfile avoid = null)
        {
            if (pool == null || pool.Length == 0) return fallback;
            if (pool.Length == 1) return pool[0];
            if (avoid != null)
            {
                // try to pick a different one; limited attempts
                for (int i = 0; i < 4; i++)
                {
                    var pick = pool[rng.Next(0, pool.Length)];
                    if (pick != avoid && pick != null) return pick;
                }
            }
            var chosen = pool[rng.Next(0, pool.Length)];
            return chosen != null ? chosen : fallback;
        }

        static int MakeTimeSeed()
        {
            long t = DateTime.UtcNow.Ticks;
            return unchecked((int)(t ^ (t >> 32)));
        }
    }
}
