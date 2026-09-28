using System.Collections.Generic;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary><c>T2 route-cmd:</c> and desk line for the <see cref="RouteCommandParser"/> queue.</summary>
public static class RouteCommandTelemetry
{
    private const string Separator = " | ";

    /// <summary>e.g. <c>Drive&gt;B | Stop | Throw J-1 | Rev | Drive&lt;C</c>; <c>—</c> when empty.</summary>
    public static string FormatQueue(IReadOnlyList<LocoCommand>? cmds)
    {
        if (cmds == null || cmds.Count == 0)
        {
            return "—";
        }

        var sb = new StringBuilder(cmds.Count * 16);
        for (var i = 0; i < cmds.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(Separator);
            }

            var c = cmds[i];
            switch (c.Action)
            {
                case LocoCommandAction.Drive:
                    sb.Append(c.TravelReverse ? "Drive<" : "Drive>").Append(c.TargetId);
                    break;
                case LocoCommandAction.Stop:
                    sb.Append("Stop");
                    break;
                case LocoCommandAction.ThrowSwitch:
                    sb.Append(c.TargetIsJunction ? "Throw " : "Align@").Append(c.TargetId);
                    break;
                case LocoCommandAction.ChangeDirection:
                    sb.Append(c.TravelReverse ? "Rev" : "Fwd");
                    break;
            }
        }

        return sb.ToString();
    }

    public static string FormatLog(IReadOnlyList<LocoCommand>? cmds, string? reason)
    {
        var why = string.IsNullOrWhiteSpace(reason) ? "plan" : reason!.Trim();
        return "T2 route-cmd: " + why + " n=" + (cmds?.Count ?? 0) + Separator + FormatQueue(cmds);
    }

    /// <summary>Null when there is no queue (desk hides the line).</summary>
    public static string? FormatDesk(IReadOnlyList<LocoCommand>? cmds) =>
        cmds == null || cmds.Count == 0 ? null : "Cmd: " + FormatQueue(cmds);
}
