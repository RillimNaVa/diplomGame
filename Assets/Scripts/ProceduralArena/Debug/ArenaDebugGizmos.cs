using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Build;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.DebugTools
{
    [ExecuteAlways]
    public class ArenaDebugGizmos : MonoBehaviour
    {
        public ArenaRunConfig config;

        [Header("r4 single-arena")]
        [Tooltip("Assign an ArenaTypeProfile to use the r4 SingleArenaGenerator path. Leave null to use legacy BSP.")]
        public ArenaTypeProfile typeProfile;
        [Tooltip("Per-arena seed for r4 path. 0 = time-based.")]
        public int arenaSeed = 12345;

        public ArenaDebugSettings debug = new ArenaDebugSettings();
        public bool generateOnStart = true;
        public bool buildGeometryOnStart = true;

        [SerializeField, HideInInspector] int lastSeedUsed;
        ArenaRuntimeContext ctx;
        bool usingSinglePath;

        public ArenaRuntimeContext Context => ctx;
        public int LastSeed => lastSeedUsed;

        void Start()
        {
            if (!Application.isPlaying) return;
            if (generateOnStart && config != null)
            {
                if (typeProfile != null) GenerateSingleArena();
                else GenerateFromConfigSeed();
                if (buildGeometryOnStart) BuildGeometry();
            }
        }

        [ContextMenu("Generate From Seed")]
        public void GenerateFromConfigSeed()
        {
            if (config == null) { UnityEngine.Debug.LogWarning("[ArenaDebugGizmos] No config assigned."); return; }
            ctx = ArenaGenerator.Generate(config);
            lastSeedUsed = ctx.masterSeed;
            usingSinglePath = false;
            UnityEngine.Debug.Log(ArenaGenerationLog.BuildSummary(ctx));
        }

        [ContextMenu("r4 / Generate Single Arena")]
        public void GenerateSingleArena()
        {
            if (config == null) { UnityEngine.Debug.LogWarning("[ArenaDebugGizmos] No config assigned."); return; }
            if (typeProfile == null) { UnityEngine.Debug.LogWarning("[ArenaDebugGizmos] No typeProfile assigned."); return; }
            ctx = SingleArenaGenerator.Generate(arenaSeed, typeProfile, config);
            lastSeedUsed = ctx.masterSeed;
            usingSinglePath = true;
            UnityEngine.Debug.Log(ArenaGenerationLog.BuildSingleSummary(ctx));
        }

        [ContextMenu("r4 / Generate + Build Single Arena")]
        public void GenerateAndBuildSingle()
        {
            GenerateSingleArena();
            if (ctx == null || ctx.layout == null) return;
            ArenaBuilder.BuildSingle(ctx, config, transform);
        }

        [ContextMenu("r4 / Randomize Seed + Build")]
        public void GenerateRandomSingle()
        {
            int prev = arenaSeed;
            arenaSeed = 0;
            GenerateAndBuildSingle();
            arenaSeed = prev;
        }

        [ContextMenu("Generate Random")]
        public void GenerateRandom()
        {
            if (config == null) { UnityEngine.Debug.LogWarning("[ArenaDebugGizmos] No config assigned."); return; }
            int prev = config.seed;
            config.seed = 0;
            ctx = ArenaGenerator.Generate(config);
            config.seed = prev;
            lastSeedUsed = ctx.masterSeed;
            UnityEngine.Debug.Log(ArenaGenerationLog.BuildSummary(ctx));
        }

        [ContextMenu("Build Geometry")]
        public void BuildGeometry()
        {
            if (ctx == null || ctx.layout == null) GenerateFromConfigSeed();
            if (ctx == null || ctx.layout == null) return;
            ArenaBuilder.Build(ctx, config, transform);
        }

        [ContextMenu("Generate + Build")]
        public void GenerateAndBuild()
        {
            GenerateFromConfigSeed();
            BuildGeometry();
        }

        [ContextMenu("Clear Geometry")]
        public void ClearGeometry()
        {
            ArenaBuilder.Clear(transform);
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            ArenaBuilder.Clear(transform);
            ctx = null;
            lastSeedUsed = 0;
        }

        void OnDrawGizmos()
        {
            if (ctx == null || ctx.layout == null || config == null) return;
            var layout = ctx.layout;
            float m = config.macroCellMeters;
            Vector3 origin = transform.position;

            if (debug.drawOuterBounds)
            {
                Gizmos.color = debug.outerColor;
                DrawRect(origin, layout.outerBoundsCells, m, 0.01f);
            }

            if (debug.drawBspLeaves && layout.bspRoot != null)
            {
                Gizmos.color = debug.leafColor;
                DrawBspLeaves(layout.bspRoot, origin, m);
            }

            if (debug.drawCorridors)
            {
                Gizmos.color = debug.corridorColor;
                foreach (var c in layout.corridors) DrawCorridor(c, origin, m);
            }

            if (debug.drawRooms)
            {
                foreach (var r in layout.rooms)
                {
                    Gizmos.color = r.id == layout.startRoomId ? debug.startColor
                                 : r.id == layout.exitRoomId ? debug.exitColor
                                 : debug.roomColor;
                    DrawRect(origin, r.boundsCells, m, 0.02f);
                }
            }

            if (debug.drawDoors)
            {
                Gizmos.color = debug.doorColor;
                foreach (var r in layout.rooms)
                    foreach (var d in r.doorAnchorsCells)
                        Gizmos.DrawSphere(CellToWorld(origin, d, m) + new Vector3(m * 0.5f, 0.1f, m * 0.5f), m * 0.2f);
            }

#if UNITY_EDITOR
            if (debug.drawLabels)
            {
                foreach (var r in layout.rooms)
                {
                    var p = CellToWorld(origin, r.CenterCell, m) + new Vector3(m * 0.5f, 0.2f, m * 0.5f);
                    UnityEditor.Handles.Label(p, $"#{r.id} {r.type}");
                }
            }
#endif

            if (usingSinglePath) DrawSingleArenaGizmos(origin, m);
        }

        void DrawSingleArenaGizmos(Vector3 origin, float m)
        {
            if (ctx.layout.rooms.Count == 0) return;
            var r = ctx.layout.rooms[0];

            if (debug.drawShapeMask && r.shapeMask != null)
            {
                Gizmos.color = debug.shapeMaskColor;
                var mask = r.shapeMask;
                var b = r.boundsCells;
                int w = mask.GetLength(0), h = mask.GetLength(1);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!mask[x, y]) continue;
                    Vector3 c = origin + new Vector3((x + b.xMin) * m + m * 0.5f, 0.03f, (y + b.yMin) * m + m * 0.5f);
                    Gizmos.DrawCube(c, new Vector3(m * 0.9f, 0.02f, m * 0.9f));
                }
            }

            if (debug.drawArenaExits)
            {
                Gizmos.color = debug.arenaExitColor;
                foreach (var e in r.exitDoorAnchors)
                {
                    Vector3 c = origin + new Vector3(e.worldCenter.x, 1f, e.worldCenter.z);
                    Gizmos.DrawSphere(c, m * 0.35f);
                    Vector3 dir = new Vector3(e.outwardDir.x, 0f, e.outwardDir.y) * m;
                    Gizmos.DrawLine(c, c + dir);
                }
            }

            if (debug.drawCoverPlacements)
            {
                Gizmos.color = debug.coverGizmoColor;
                foreach (var cov in r.coverPlacements)
                {
                    Gizmos.DrawWireCube(origin + cov.position, cov.size);
                }
            }

            if (debug.drawStartSpawn)
            {
                Gizmos.color = debug.startSpawnColor;
                Vector3 s = origin + new Vector3(r.startSpawnPoint.x, 1f, r.startSpawnPoint.z);
                Gizmos.DrawSphere(s, m * 0.4f);
            }

            if (debug.drawCombatSpawns)
            {
                Gizmos.color = debug.combatSpawnColor;
                foreach (var sp in r.combatSpawnPoints)
                    Gizmos.DrawSphere(origin + sp + new Vector3(0f, 0.5f, 0f), m * 0.2f);
            }
        }

        void DrawBspLeaves(BspNode node, Vector3 origin, float m)
        {
            if (node.IsLeaf) { DrawRect(origin, node.bounds, m, 0f); return; }
            if (node.left != null) DrawBspLeaves(node.left, origin, m);
            if (node.right != null) DrawBspLeaves(node.right, origin, m);
        }

        void DrawRect(Vector3 origin, RectInt rect, float m, float yOffset)
        {
            Vector3 a = origin + new Vector3(rect.xMin * m, yOffset, rect.yMin * m);
            Vector3 b = origin + new Vector3(rect.xMax * m, yOffset, rect.yMin * m);
            Vector3 c = origin + new Vector3(rect.xMax * m, yOffset, rect.yMax * m);
            Vector3 d = origin + new Vector3(rect.xMin * m, yOffset, rect.yMax * m);
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }

        void DrawCorridor(ArenaCorridorData corridor, Vector3 origin, float m)
        {
            for (int i = 1; i < corridor.pathCells.Count; i++)
            {
                var p0 = corridor.pathCells[i - 1];
                var p1 = corridor.pathCells[i];
                Vector3 a = CellToWorld(origin, p0, m) + new Vector3(m * 0.5f, 0.05f, m * 0.5f);
                Vector3 b = CellToWorld(origin, p1, m) + new Vector3(m * 0.5f, 0.05f, m * 0.5f);
                Gizmos.DrawLine(a, b);
            }
        }

        static Vector3 CellToWorld(Vector3 origin, Vector2Int cell, float m) =>
            origin + new Vector3(cell.x * m, 0f, cell.y * m);
    }
}
