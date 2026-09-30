using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

public enum RouteExecStartResult
{
    Ok = 0,
    NoQueue = 1,
    NeedDispatcher = 2,
    SwitchListGoActive = 3,
    JobListActive = 4,
    AlignFailed = 5,
}

/// <summary>
/// Route-tab GO (16.2 ship 2). Owns a copy of the queue captured at GO so Align
/// re-freezing <see cref="RoutePlanSession.Commands"/> cannot rewind the cursor.
/// The governor reads <see cref="Active"/> / <see cref="WantsBrake"/> / <see cref="RequestKmh"/>.
/// </summary>
public static class RouteExecSession
{
    private static IReadOnlyList<LocoCommand> _cmds = Array.Empty<LocoCommand>();
    private static RouteExecState _state;
    private static RouteExecDecision _last;

    public static bool Active { get; private set; }

    public static IReadOnlyList<LocoCommand> Commands => _cmds;

    public static RouteExecState State => _state;

    /// <summary>Governor brakes (Stop GO path) instead of PID drive.</summary>
    public static bool WantsBrake =>
        Active && _last.Action != RouteExecAction.Drive;

    public static bool TravelReverse => _state.TravelReverse;

    public static float RequestKmh =>
        Active && _last.Action == RouteExecAction.Drive ? _last.RequestKmh : 0f;

    /// <summary>
    /// Route tab only: a job Switch List or a running Switch List GO keeps the Epic 13 chain.
    /// Route-bound lists (Set dest sawtooth) are the Route tab.
    /// </summary>
    public static RouteExecStartResult CanStart(
        IReadOnlyList<LocoCommand>? cmds,
        bool hasDispatcher,
        bool switchListGoActive,
        bool jobListActive)
    {
        if (jobListActive)
        {
            return RouteExecStartResult.JobListActive;
        }

        if (switchListGoActive)
        {
            return RouteExecStartResult.SwitchListGoActive;
        }

        if (!hasDispatcher)
        {
            return RouteExecStartResult.NeedDispatcher;
        }

        return cmds == null || cmds.Count == 0
            ? RouteExecStartResult.NoQueue
            : RouteExecStartResult.Ok;
    }

    public static void Start(IReadOnlyList<LocoCommand> cmds)
    {
        _cmds = cmds ?? Array.Empty<LocoCommand>();
        _state = RouteCommandExecutor.Begin(_cmds);
        _last = new RouteExecDecision(RouteExecAction.Brake, _state.TravelReverse);
        Active = _cmds.Count > 0;
    }

    public static RouteExecDecision Tick(in RouteExecInput input)
    {
        if (!Active)
        {
            _last = default;
            return _last;
        }

        _last = RouteCommandExecutor.Tick(_cmds, ref _state, in input);
        return _last;
    }

    public static void ReportThrow(bool ok) =>
        RouteCommandExecutor.ReportThrow(ref _state, ok);

    public static void Stop()
    {
        Active = false;
        _cmds = Array.Empty<LocoCommand>();
        _state = default;
        _last = default;
        // Cab 2.16.36.8: Cruise left on after Done re-armed a 25 km/h hold
        // on the leftover dest and wound throttle on a stopped consist.
        PidCruiseSession.SetEnabled(false);
    }

    public static string FormatStartRefusal(RouteExecStartResult result) =>
        result switch
        {
            RouteExecStartResult.NoQueue => "Route GO needs a route plan (Set dest)",
            RouteExecStartResult.NeedDispatcher => "Route GO needs Dispatcher",
            RouteExecStartResult.SwitchListGoActive => "Route GO: Stop GO first",
            RouteExecStartResult.JobListActive => "Route GO: job Switch List active",
            RouteExecStartResult.AlignFailed => "Route GO: Align failed",
            _ => "Route GO",
        };
}

/// <summary><c>T2 route-exec:</c> lines — one per queue step / action change, never per tick.</summary>
public static class RouteExecTelemetry
{
    public const string Prefix = "T2 route-exec: ";

    public static string FormatStart(IReadOnlyList<LocoCommand>? cmds) =>
        Prefix + "go n=" + (cmds?.Count ?? 0) + " | " + RouteCommandTelemetry.FormatQueue(cmds);

    public static string FormatRefuse(RouteExecStartResult result) =>
        Prefix + "refuse " + result;

    public static string FormatStop(string why) => Prefix + "stop " + why;

    /// <summary>Transition key: (index, action, reason). Log only when it changes.</summary>
    public static int Key(in RouteExecState state, in RouteExecDecision d) =>
        (state.Index * 397) ^ ((int)d.Action * 31) ^ (d.Reason?.GetHashCode() ?? 0);

    public static string Format(in RouteExecState state, int count, in RouteExecDecision d)
    {
        var line = Prefix + "step " + state.Index + "/" + count + " " + d.Action
            + (d.TravelReverse ? " R" : " F");
        switch (d.Action)
        {
            case RouteExecAction.Drive:
                line += " >" + d.TargetId + " req=" + d.RequestKmh.ToString("0");
                if (d.LegPinId != null)
                {
                    line += " pin=" + d.LegPinId;
                }

                break;
            case RouteExecAction.Throw:
                line += " " + d.TargetId + " branch=" + d.Branch;
                break;
            case RouteExecAction.Align:
                line += " @" + d.TargetId;
                break;
        }

        if (d.Reason != null)
        {
            line += " · " + d.Reason;
        }

        return line;
    }
}
