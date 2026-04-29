using System.Collections.Generic;
using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Arena
{
    // Phase 2 / PR 2.H1 — default cover structures shipped in code so existing
    // arenas immediately get hand-authored silhouettes without anyone touching
    // the Editor. All sizes are in meters; offsets are local-space relative to
    // the structure pivot (which sits at floor level on the chosen cell).
    //
    // 4 baseline structures ship in v1:
    //   - Bunker: 3-walled half-shelter, opens toward the player approach.
    //   - Sandbag Line: 4 chest-high boxes in a line, with one taller corner
    //     post that doubles as a sniper perch.
    //   - Pillar Cluster: 4 vertical columns with a low debris ring around
    //     them. Forces the player to weave between LoS lanes.
    //   - Sniper Nest: 2-tier raised box with a forward firing slit, slightly
    //     rotated so it reads as "intentional" rather than a generated cover.
    //
    // Adjust here if a Phase 3 playtest shows any structure dominates.
    public static class BuiltInStructures
    {
        static StructureDefinition[] s_cache;

        public static StructureDefinition[] All()
        {
            if (s_cache != null) return s_cache;
            s_cache = new[]
            {
                Bunker(),
                SandbagLine(),
                PillarCluster(),
                SniperNest(),
            };
            return s_cache;
        }

        public static StructureDefinition Bunker()
        {
            // 3 walls forming a U, opens along +X. Footprint ~3×3 cells.
            // Wall thickness 0.4m, height 1.7m. Width ~3.6m, depth ~3m.
            const float h = 1.7f;
            const float th = 0.4f;
            const float w = 3.6f;
            const float d = 3.0f;
            var parts = new List<StructureBoxPart>
            {
                // back wall (along Z, at -X side)
                new StructureBoxPart { localOffset = new Vector3(-d * 0.5f + th * 0.5f, 0f, 0f), size = new Vector3(th, h, w), slot = StructureSlot.Wall },
                // side wall +Z
                new StructureBoxPart { localOffset = new Vector3(0f, 0f,  w * 0.5f - th * 0.5f), size = new Vector3(d, h, th), slot = StructureSlot.Wall },
                // side wall -Z
                new StructureBoxPart { localOffset = new Vector3(0f, 0f, -w * 0.5f + th * 0.5f), size = new Vector3(d, h, th), slot = StructureSlot.Wall },
                // emissive trim along the top of the back wall
                new StructureBoxPart { localOffset = new Vector3(-d * 0.5f + th * 0.5f, h - 0.08f, 0f), size = new Vector3(th + 0.05f, 0.12f, w - 0.1f), slot = StructureSlot.EmissiveAccent },
            };
            return new StructureDefinition
            {
                structureId = "bunker",
                parts = parts.ToArray(),
                footprintCells = new Vector2Int(3, 3),
                weight = 1f,
            };
        }

        public static StructureDefinition SandbagLine()
        {
            // 4 chest-high boxes in a row + 1 taller post at the end.
            // Footprint ~4×1 cells.
            const float bagW = 0.9f;
            const float bagH = 1.05f;
            const float bagD = 0.7f;
            const float gap = 0.05f;
            float spacing = bagW + gap;
            float startX = -1.5f * spacing;

            var parts = new List<StructureBoxPart>();
            for (int i = 0; i < 4; i++)
            {
                parts.Add(new StructureBoxPart
                {
                    localOffset = new Vector3(startX + i * spacing, 0f, 0f),
                    size = new Vector3(bagW, bagH, bagD),
                    slot = StructureSlot.Cover,
                });
            }
            // taller corner post — sniper-friendly
            parts.Add(new StructureBoxPart
            {
                localOffset = new Vector3(startX + 4f * spacing, 0f, 0f),
                size = new Vector3(0.7f, 1.7f, 0.7f),
                slot = StructureSlot.Wall,
            });
            // emissive strip on top of the row
            parts.Add(new StructureBoxPart
            {
                localOffset = new Vector3(0f, bagH - 0.05f, 0f),
                size = new Vector3(spacing * 4f - gap, 0.06f, 0.08f),
                slot = StructureSlot.EmissiveAccent,
            });

            return new StructureDefinition
            {
                structureId = "sandbag_line",
                parts = parts.ToArray(),
                footprintCells = new Vector2Int(4, 1),
                weight = 1f,
            };
        }

        public static StructureDefinition PillarCluster()
        {
            // 4 columns + a low debris ring. Footprint ~3×3 cells.
            const float pillarW = 0.55f;
            const float pillarH = 4.5f;
            const float ringH = 0.45f;
            const float ringW = 0.6f;
            float r = 1.4f;

            var parts = new List<StructureBoxPart>();
            // columns at the corners of a square
            float[,] offsets = new float[,] { { -r, -r }, { r, -r }, { -r, r }, { r, r } };
            for (int i = 0; i < 4; i++)
            {
                parts.Add(new StructureBoxPart
                {
                    localOffset = new Vector3(offsets[i, 0], 0f, offsets[i, 1]),
                    size = new Vector3(pillarW, pillarH, pillarW),
                    slot = StructureSlot.Wall,
                });
            }
            // debris ring (4 sides between columns)
            parts.Add(new StructureBoxPart { localOffset = new Vector3(0f, 0f, -r), size = new Vector3(2f * r, ringH, ringW), slot = StructureSlot.Cover });
            parts.Add(new StructureBoxPart { localOffset = new Vector3(0f, 0f,  r), size = new Vector3(2f * r, ringH, ringW), slot = StructureSlot.Cover });
            parts.Add(new StructureBoxPart { localOffset = new Vector3(-r, 0f, 0f), size = new Vector3(ringW, ringH, 2f * r), slot = StructureSlot.Cover });
            parts.Add(new StructureBoxPart { localOffset = new Vector3( r, 0f, 0f), size = new Vector3(ringW, ringH, 2f * r), slot = StructureSlot.Cover });
            // tiny emissive cap on each pillar
            for (int i = 0; i < 4; i++)
            {
                parts.Add(new StructureBoxPart
                {
                    localOffset = new Vector3(offsets[i, 0], pillarH - 0.05f, offsets[i, 1]),
                    size = new Vector3(pillarW + 0.1f, 0.1f, pillarW + 0.1f),
                    slot = StructureSlot.EmissiveAccent,
                });
            }

            return new StructureDefinition
            {
                structureId = "pillar_cluster",
                parts = parts.ToArray(),
                footprintCells = new Vector2Int(3, 3),
                weight = 0.85f,
            };
        }

        public static StructureDefinition SniperNest()
        {
            // 2-tier raised box: tier 1 is a ~1m raised platform, tier 2 is
            // a back wall with a horizontal firing slit (achieved by spawning
            // top + bottom plates + two short jambs).
            // Footprint ~3×2 cells.
            const float baseW = 2.6f;
            const float baseH = 1.0f;
            const float baseD = 1.6f;
            const float slitH = 0.5f; // horizontal gap height
            const float wallTop = 1.6f;
            const float th = 0.3f;
            float backX = -baseD * 0.5f + th * 0.5f;

            var parts = new List<StructureBoxPart>
            {
                // platform (raised step)
                new StructureBoxPart { localOffset = new Vector3(0f, 0f, 0f), size = new Vector3(baseD, baseH, baseW), slot = StructureSlot.Floor },
                // back wall lower portion (below slit)
                new StructureBoxPart { localOffset = new Vector3(backX, baseH, 0f), size = new Vector3(th, 0.5f, baseW), slot = StructureSlot.Wall },
                // back wall upper portion (above slit)
                new StructureBoxPart { localOffset = new Vector3(backX, baseH + 0.5f + slitH, 0f), size = new Vector3(th, wallTop - (0.5f + slitH), baseW), slot = StructureSlot.Wall },
                // left jamb
                new StructureBoxPart { localOffset = new Vector3(backX, baseH + 0.5f, -baseW * 0.5f + 0.25f), size = new Vector3(th, slitH, 0.5f), slot = StructureSlot.Wall },
                // right jamb
                new StructureBoxPart { localOffset = new Vector3(backX, baseH + 0.5f, baseW * 0.5f - 0.25f), size = new Vector3(th, slitH, 0.5f), slot = StructureSlot.Wall },
                // emissive lip along the slit
                new StructureBoxPart { localOffset = new Vector3(backX + 0.06f, baseH + 0.45f, 0f), size = new Vector3(0.06f, 0.06f, baseW - 0.6f), slot = StructureSlot.EmissiveAccent },
            };

            return new StructureDefinition
            {
                structureId = "sniper_nest",
                parts = parts.ToArray(),
                footprintCells = new Vector2Int(3, 2),
                eligibleCategories = new[] { ArenaCategory.Combat, ArenaCategory.Elite, ArenaCategory.Boss },
                weight = 0.7f,
            };
        }
    }
}
