using UnityEngine;
using VoidSurvivor.ProceduralArena.Build;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Layout;

namespace VoidSurvivor.ProceduralArena.DebugTools
{
    [ExecuteAlways]
    public class ArenaDebugGizmos : MonoBehaviour
    {
        public ArenaRunConfig config;
        public ArenaDebugSettings debug = new ArenaDebugSettings();
        public bool generateOnStart = true;
        public bool buildGeometryOnStart = true;

        [SerializeField, HideInInspector] int lastSeedUsed;
        ArenaRuntimeContext ctx;

        public ArenaRuntimeContext Context => ctx;
        public int LastSeed => lastSeedUsed;

        void Start()
        {
            if (!Application.isPlaying) return;
            if (generateOnStart && config != null)
            {
                GenerateFromConfigSeed();
                if (buildGeometryOnStart) BuildGeometry();
            }
        }

        [ContextMenu("Generate From Seed")]
        public void GenerateFromConfigSeed()
        {
            if (config == null) { UnityEngine.Debug.LogWarning("[ArenaDebugGizmos] No config assigned."); return; }
            ctx = ArenaGenerator.Generate(config);
            lastSeedUsed = ctx.masterSeed;
            UnityEngine.Debug.Log(ArenaGenerationLog.BuildSummary(ctx));
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
