namespace YardMasterSuite.Core;

/// <summary>Desk click kinds. Payload is this enum; dest strings live in <see cref="RouteDestSession"/>.</summary>
public enum MapsDestKind : byte
{
    None = 0,
    Set = 1,
    Recheck = 2,
    Clear = 3,
    RejectEmpty = 4,
}

/// <summary>Type A Maps dest command. No strings — session already holds city/track.</summary>
public readonly struct MapsDestCommand
{
    public readonly MapsDestKind Kind;

    public MapsDestCommand(MapsDestKind kind)
    {
        Kind = kind;
    }
}

/// <summary>
/// Bind dest on Set dest / Recheck / Clear. Does **not** pathfind — **8.2** listens
/// to <see cref="YmsEventBus.OnMapsDestCommand"/>.
/// </summary>
public static class MapsDestApply
{
    public static MapsDestKind SetDest(string? city, string? track)
    {
        var c = city?.Trim();
        var t = track?.Trim();
        if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(t))
        {
            return MapsDestKind.RejectEmpty;
        }

        RouteDestSession.Set(c, t);
        return MapsDestKind.Set;
    }

    public static MapsDestKind Recheck(string? city, string? track)
    {
        if (!RouteDestSession.HasDestination)
        {
            if (SetDest(city, track) == MapsDestKind.RejectEmpty)
            {
                return MapsDestKind.RejectEmpty;
            }
        }

        return MapsDestKind.Recheck;
    }

    public static MapsDestKind Clear()
    {
        RouteDestSession.Clear();
        // Route + Per job Clear must wipe the shared Switch List (8.5 smoke: dest clear left stale legs).
        SwitchListSession.Clear();
        return MapsDestKind.Clear;
    }
}
