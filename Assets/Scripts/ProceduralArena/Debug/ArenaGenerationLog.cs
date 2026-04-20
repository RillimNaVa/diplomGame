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
    }
}
