using System.Text;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.DebugTools
{
    public static class ArenaGenerationLog
    {
        public static string BuildSummary(ArenaRuntimeContext ctx)
        {
            var sb = new StringBuilder(256);
            sb.Append("[Arena] seed=").Append(ctx.masterSeed);
            if (ctx.layout != null)
            {
                sb.Append(" rooms=").Append(ctx.layout.rooms.Count);
                sb.Append(" corridors=").Append(ctx.layout.corridors.Count);
                sb.Append(" start=").Append(ctx.layout.startRoomId);
                sb.Append(" exit=").Append(ctx.layout.exitRoomId);
            }
            sb.Append(" attempts=").Append(ctx.attemptsUsed);
            sb.Append(" time=").Append(ctx.generationTimeMs).Append("ms");
            if (ctx.usedHandCodedFallback) sb.Append(" [HAND_FALLBACK]");
            if (ctx.warnings.Count > 0) sb.Append(" warnings=").Append(ctx.warnings.Count);
            return sb.ToString();
        }

        public static string BuildSingleSummary(ArenaRuntimeContext ctx)
        {
            var sb = new StringBuilder(256);
            sb.Append("[ArenaR4] seed=").Append(ctx.masterSeed);
            if (ctx.layout != null && ctx.layout.rooms.Count > 0)
            {
                var r = ctx.layout.rooms[0];
                sb.Append(" cat=").Append(r.category);
                sb.Append(" shape=").Append(r.shape);
                sb.Append(" bounds=").Append(r.boundsCells.width).Append('x').Append(r.boundsCells.height);
                sb.Append(" ceiling=").Append(r.wallHeightMeters.ToString("F1")).Append('m');
                sb.Append(" cover=").Append(r.coverPlacements.Count);
                sb.Append(" exits=").Append(r.exitDoorAnchors.Count);
                sb.Append(" spawns=").Append(r.combatSpawnPoints.Count);
            }
            sb.Append(" time=").Append(ctx.generationTimeMs).Append("ms");
            if (ctx.warnings.Count > 0) sb.Append(" warnings=").Append(ctx.warnings.Count);
            return sb.ToString();
        }
    }
}
