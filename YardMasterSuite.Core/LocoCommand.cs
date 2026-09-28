namespace YardMasterSuite.Core;

/// <summary>Kinetic cue from a <see cref="PathPlanResult"/> (Gemini Step 7).</summary>
public enum LocoCommandAction
{
    Drive = 0,
    Stop = 1,
    ThrowSwitch = 2,
    ChangeDirection = 3,
}

/// <summary>One driving / Align instruction. Zero-alloc friendly value type.</summary>
public readonly struct LocoCommand
{
    public LocoCommand(LocoCommandAction action, string? targetId = null, bool travelReverse = false)
    {
        Action = action;
        TargetId = targetId ?? string.Empty;
        TravelReverse = travelReverse;
    }

    public LocoCommandAction Action { get; }

    /// <summary>
    /// Drive → track id. ThrowSwitch → junction id or pivot track. ChangeDirection → unused.
    /// </summary>
    public string TargetId { get; }

    /// <summary>
    /// Drive: reverse travel. ChangeDirection: set reverser to reverse when true.
    /// </summary>
    public bool TravelReverse { get; }
}
