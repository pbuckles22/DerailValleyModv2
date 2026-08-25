namespace YardMasterSuite.Core;

/// <summary>
/// Named Three-Gate predicates. Callers pass these into
/// <see cref="ThreeGate.TryApply"/> — do not write around the gate.
/// </summary>
public static class ThreeGateWrite
{
    /// <summary>World session + the actor that owns the write (on a car, has a loco).</summary>
    public static bool Integrity(bool worldActive, bool actorPresent) =>
        worldActive && actorPresent;

    /// <summary>The control / target exists in the sim.</summary>
    public static bool StateRegistry(bool controlPresent) => controlPresent;

    /// <summary>Legal to intervene (no pause overlay; control not blocked).</summary>
    public static bool Safety(bool overlayClear, bool controlNotBlocked) =>
        overlayClear && controlNotBlocked;
}
