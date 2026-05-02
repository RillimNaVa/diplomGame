using System;
using System.Collections.Generic;
using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Phase 4 / PR 4.PD — 10-stage Standard Run graph (TZ §4.5).
    /// Layout (stageIndex / category template / branch factor):
    ///   0  Start                (1 node)
    ///   1  Combat               (2 nodes)
    ///   2  Combat               (2 nodes)
    ///   3  Combat / Elite       (2 nodes)
    ///   4  Combat / Shop / Rest (3 nodes — wide tier)
    ///   5  Combat               (2 nodes)
    ///   6  Combat / Elite       (2 nodes)
    ///   7  Combat / Shop / Rest (3 nodes — wide tier)
    ///   8  Combat / Shop / Rest (2 nodes — final prep)
    ///   9  Boss                 (1 node)
    ///
    /// Total = 20 nodes; player path is always 10 visited rooms.
    /// Shared-subtree wiring: every parent at stage N points to ALL children
    /// at stage N+1 (combinatorially capped by template, not exponential).
    ///
    /// Determinism: single sub-stream RNG seeded from runSeed. Profile picks
    /// avoid duplicating the same profile twice within a stage's alternative
    /// list (so a 3-door wide tier offers 3 distinct categories when possible).
    /// </summary>
    public static class RunGraphGenerator
    {
        // Category template per stage (ArenaCategory entries the stage may produce).
        // Keep in sync with the docstring above.
        static readonly ArenaCategory[][] StageTemplates =
        {
            new[] { ArenaCategory.Start },                                                 // 0
            new[] { ArenaCategory.Combat, ArenaCategory.Combat },                          // 1
            new[] { ArenaCategory.Combat, ArenaCategory.Combat },                          // 2
            new[] { ArenaCategory.Combat, ArenaCategory.Elite },                           // 3
            new[] { ArenaCategory.Combat, ArenaCategory.Shop, ArenaCategory.Rest },        // 4
            new[] { ArenaCategory.Combat, ArenaCategory.Combat },                          // 5
            new[] { ArenaCategory.Combat, ArenaCategory.Elite },                           // 6
            new[] { ArenaCategory.Combat, ArenaCategory.Shop, ArenaCategory.Rest },        // 7
            new[] { ArenaCategory.Combat, ArenaCategory.Shop },                            // 8
            new[] { ArenaCategory.Boss },                                                  // 9
        };

        public static RunGraph Build(int runSeed, RunConfig cfg)
        {
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            if (cfg.startProfile == null) throw new InvalidOperationException("RunConfig.startProfile is null.");
            if (cfg.bossProfile == null) throw new InvalidOperationException("RunConfig.bossProfile is null.");

            int seed = runSeed != 0 ? runSeed : MakeTimeSeed();
            var rng = new System.Random(unchecked(seed ^ 0x12340A0A)); // graphSeed per TZ §16

            var graph = new RunGraph { runSeed = seed };
            int nextId = 0;

            // Build per-stage node lists.
            var stages = new List<RunGraphNode>[StageTemplates.Length];

            for (int s = 0; s < StageTemplates.Length; s++)
            {
                var template = StageTemplates[s];
                stages[s] = new List<RunGraphNode>(template.Length);

                // Stage 0 / 9 are anchors with single fixed profile.
                if (s == 0)
                {
                    stages[0].Add(BuildNode(ref nextId, s, cfg.startProfile, rng, graph));
                    continue;
                }
                if (s == StageTemplates.Length - 1)
                {
                    stages[s].Add(BuildNode(ref nextId, s, cfg.bossProfile, rng, graph));
                    continue;
                }

                // For mid stages, instantiate one node per template slot.
                // Avoid using the same profile twice within the same stage when
                // alternatives are available.
                var usedProfiles = new HashSet<ArenaTypeProfile>();
                for (int slot = 0; slot < template.Length; slot++)
                {
                    var cat = template[slot];
                    var profile = PickProfile(cat, cfg, rng, usedProfiles);
                    if (profile != null) usedProfiles.Add(profile);
                    stages[s].Add(BuildNode(ref nextId, s, profile != null ? profile : cfg.startProfile, rng, graph));
                }
            }

            // Wire: every parent at stage N points to every child at stage N+1.
            for (int s = 0; s < stages.Length - 1; s++)
            {
                var parents = stages[s];
                var children = stages[s + 1];
                for (int p = 0; p < parents.Count; p++)
                    for (int c = 0; c < children.Count; c++)
                        parents[p].children.Add(children[c]);
            }

            graph.startNode = stages[0][0];
            return graph;
        }

        static RunGraphNode BuildNode(ref int nextId, int stageIndex, ArenaTypeProfile profile, System.Random rng, RunGraph graph)
        {
            var node = new RunGraphNode
            {
                id = nextId++,
                stageIndex = stageIndex,
                arenaIndex = stageIndex,
                stage = MapLegacyStage(stageIndex),
                arenaSeed = rng.Next(int.MinValue, int.MaxValue),
                typeProfile = profile
            };
            graph.nodes.Add(node);
            return node;
        }

        // Coarse mapping for legacy callers that still read RunStage.
        static RunStage MapLegacyStage(int stageIndex)
        {
            if (stageIndex <= 0) return RunStage.Start;
            if (stageIndex >= 9) return RunStage.Boss;
            if (stageIndex <= 3) return RunStage.Mid1;
            if (stageIndex <= 6) return RunStage.Mid2;
            return RunStage.Mid3;
        }

        static ArenaTypeProfile PickProfile(ArenaCategory category, RunConfig cfg, System.Random rng, HashSet<ArenaTypeProfile> avoid)
        {
            ArenaTypeProfile[] pool = ResolvePool(category, cfg);
            if (pool == null || pool.Length == 0) pool = cfg.combatPool;
            if (pool == null || pool.Length == 0) return null;

            // Filter out null/Parkour entries (Parkour disabled product-side, see legacy generator).
            var filtered = new List<ArenaTypeProfile>(pool.Length);
            for (int i = 0; i < pool.Length; i++)
                if (pool[i] != null && pool[i].category != ArenaCategory.Parkour) filtered.Add(pool[i]);

            if (filtered.Count == 0) return pool[0];
            if (filtered.Count == 1) return filtered[0];

            // Try a few times to avoid duplicates, then accept whatever.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                var pick = filtered[rng.Next(0, filtered.Count)];
                if (!avoid.Contains(pick)) return pick;
            }
            return filtered[rng.Next(0, filtered.Count)];
        }

        static ArenaTypeProfile[] ResolvePool(ArenaCategory category, RunConfig cfg)
        {
            switch (category)
            {
                case ArenaCategory.Combat: return cfg.combatPool;
                case ArenaCategory.Elite: return cfg.elitePool;
                case ArenaCategory.Shop: return cfg.shopPool;
                case ArenaCategory.Rest: return cfg.restPool;
                case ArenaCategory.Boss: return new[] { cfg.bossProfile };
                case ArenaCategory.Start: return new[] { cfg.startProfile };
                default: return cfg.combatPool;
            }
        }

        static int MakeTimeSeed()
        {
            long t = DateTime.UtcNow.Ticks;
            return unchecked((int)(t ^ (t >> 32)));
        }
    }
}
