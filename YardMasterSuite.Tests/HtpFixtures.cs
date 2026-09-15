using System.IO;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>One-off SW harvest folded into CI. Player gathers; tests replay.</summary>
internal static class HtpFixtures
{
    internal const string CorridorFileName = "corridor.txt";
    internal const string GraphFileName = "graph.txt";

    internal const string Pid2918FileName = "pid-2.9.1.8.txt";

    internal const string BoardsSw20260831FileName = "boards-sw-2026-08-31.txt";

    internal const string GraphSw20260901FileName = "graph-sw-2026-09-01.txt";

    /// <summary>SL-55 cab fail→fix harvest 2026-09-04 (list-load pin-corridor → TT).</summary>
    internal const string CorridorSwSl5520260904FileName = "corridor-sw-sl-55-2026-09-04.txt";

    internal const string GraphSw20260904FileName = "graph-sw-2026-09-04.txt";

    /// <summary>SU-34 sit-still harvest 2026-09-15 (B4L → GF TT #Y-#S1775#T).</summary>
    internal const string CorridorSwSu3420260915FileName = "corridor-sw-su-34-2026-09-15.txt";

    /// <summary>FH-82 Route Set dest harvest 2026-09-15 (B4L → GF-D5I).</summary>
    internal const string CorridorSwFh8220260915FileName = "corridor-sw-fh-82-2026-09-15.txt";

    /// <summary>SL-52 Route Set dest harvest 2026-09-15 (B4L → SW-C1O).</summary>
    internal const string CorridorSwSl5220260915FileName = "corridor-sw-sl-52-2026-09-15.txt";

    internal static string Dir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Htp");

    internal static string CorridorPath => Path.Combine(Dir, CorridorFileName);

    internal static string GraphPath => Path.Combine(Dir, GraphFileName);

    internal static string Pid2918Path => Path.Combine(Dir, Pid2918FileName);

    internal static string BoardsSw20260831Path => Path.Combine(Dir, BoardsSw20260831FileName);

    internal static string GraphSw20260901Path => Path.Combine(Dir, GraphSw20260901FileName);

    internal static string CorridorSwSl5520260904Path =>
        Path.Combine(Dir, CorridorSwSl5520260904FileName);

    internal static string GraphSw20260904Path => Path.Combine(Dir, GraphSw20260904FileName);

    internal static string CorridorSwSu3420260915Path =>
        Path.Combine(Dir, CorridorSwSu3420260915FileName);

    internal static string CorridorSwFh8220260915Path =>
        Path.Combine(Dir, CorridorSwFh8220260915FileName);

    internal static string CorridorSwSl5220260915Path =>
        Path.Combine(Dir, CorridorSwSl5220260915FileName);

    internal static PostedBoardHarvestSnapshot LoadBoardsSw20260831()
    {
        Assert.True(File.Exists(BoardsSw20260831Path), "missing " + BoardsSw20260831Path);
        Assert.True(PostedBoardHarvestCodec.TryParse(File.ReadAllText(BoardsSw20260831Path), out var snap));
        return snap;
    }

    internal static TrackGraphHarvestSnapshot LoadGraphSw20260901()
    {
        Assert.True(File.Exists(GraphSw20260901Path), "missing " + GraphSw20260901Path);
        Assert.True(TrackGraphHarvestCodec.TryParse(File.ReadAllText(GraphSw20260901Path), out var snap));
        return snap;
    }

    internal static RouteHarvestSnapshot LoadCorridorSwSl5520260904()
    {
        Assert.True(File.Exists(CorridorSwSl5520260904Path), "missing " + CorridorSwSl5520260904Path);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(CorridorSwSl5520260904Path), out var snap));
        return snap;
    }

    internal static TrackGraphHarvestSnapshot LoadGraphSw20260904()
    {
        Assert.True(File.Exists(GraphSw20260904Path), "missing " + GraphSw20260904Path);
        Assert.True(TrackGraphHarvestCodec.TryParse(File.ReadAllText(GraphSw20260904Path), out var snap));
        return snap;
    }

    internal static RouteHarvestSnapshot LoadCorridorSwSu3420260915()
    {
        Assert.True(File.Exists(CorridorSwSu3420260915Path), "missing " + CorridorSwSu3420260915Path);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(CorridorSwSu3420260915Path), out var snap));
        return snap;
    }

    internal static RouteHarvestSnapshot LoadCorridorSwFh8220260915()
    {
        Assert.True(File.Exists(CorridorSwFh8220260915Path), "missing " + CorridorSwFh8220260915Path);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(CorridorSwFh8220260915Path), out var snap));
        return snap;
    }

    internal static RouteHarvestSnapshot LoadCorridorSwSl5220260915()
    {
        Assert.True(File.Exists(CorridorSwSl5220260915Path), "missing " + CorridorSwSl5220260915Path);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(CorridorSwSl5220260915Path), out var snap));
        return snap;
    }

    internal static ParsedPostedBoard RequireBoard(in PostedBoardHarvestSnapshot snap, int instanceId)
    {
        for (var i = 0; i < snap.Boards.Count; i++)
        {
            if (snap.Boards[i].InstanceId == instanceId)
            {
                return snap.Boards[i];
            }
        }

        Assert.Fail("board " + instanceId.ToString() + " missing from harvest");
        return default;
    }

    internal static HarvestedGraphBoard RequireGraphBoard(in TrackGraphHarvestSnapshot snap, int id)
    {
        for (var i = 0; i < snap.Boards.Count; i++)
        {
            if (snap.Boards[i].Id == id)
            {
                return snap.Boards[i];
            }
        }

        Assert.Fail("graph board " + id.ToString() + " missing from dump");
        return default;
    }

    internal static RouteHarvestSnapshot LoadCorridor()
    {
        Assert.True(File.Exists(CorridorPath), "missing " + CorridorPath);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(CorridorPath), out var snap));
        return snap;
    }

    internal static RouteHarvestSnapshot LoadGraph()
    {
        Assert.True(File.Exists(GraphPath), "missing " + GraphPath);
        Assert.True(RouteHarvestCodec.TryParse(File.ReadAllText(GraphPath), out var snap));
        return snap;
    }

    internal static RouteCorridorSpec ToSpec(in RouteHarvestSnapshot snap, string? expectedPinJunctionId = null)
    {
        Assert.False(string.IsNullOrEmpty(snap.OriginTrackId));
        Assert.False(string.IsNullOrEmpty(snap.DestTrackId));
        Assert.False(string.IsNullOrEmpty(snap.YardId));
        var pin = expectedPinJunctionId ?? snap.PinJunctionId;
        if (string.IsNullOrEmpty(pin))
        {
            var planned = PathPlan.Find(
                snap.Edges,
                snap.Selected,
                snap.OriginTrackId,
                snap.DestTrackId,
                destYardId: snap.YardId,
                mode: snap.Mode);
            pin = SwitchListRouteLeg.PickPinJunctionId(planned);
        }

        Assert.False(string.IsNullOrEmpty(pin));
        return new RouteCorridorSpec(
            snap.Edges,
            snap.Selected,
            snap.OriginTrackId!,
            snap.DestTrackId!,
            snap.YardId!,
            snap.Mode,
            pin!,
            expectedPastSwitchTrackId: snap.OriginTrackId!,
            expectedReverseIntoTrackId: snap.DestTrackId!);
    }

    internal static bool TryJunctionXz(in RouteHarvestSnapshot snap, string? junctionId, out float x, out float z)
    {
        x = z = 0f;
        var id = junctionId?.Trim();
        if (string.IsNullOrEmpty(id) || snap.Junctions == null)
        {
            return false;
        }

        for (var i = 0; i < snap.Junctions.Count; i++)
        {
            var j = snap.Junctions[i];
            if (string.Equals(j.Id, id, System.StringComparison.Ordinal))
            {
                x = j.X;
                z = j.Z;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// An explicit <paramref name="pinId"/> is the frog under test, so it outranks the
    /// dump's own <c>pinXZ</c>. Honouring the dump first made "45 m past frog 1" and
    /// "45 m past frog 8" build the same pose on any harvest that carried a pin.
    /// </summary>
    private static void ResolvePinXz(
        in RouteHarvestSnapshot snap,
        string? pinId,
        out float pinX,
        out float pinZ)
    {
        var requested = pinId?.Trim();
        if (!string.IsNullOrEmpty(requested)
            && !string.Equals(requested, snap.PinJunctionId, System.StringComparison.Ordinal))
        {
            Assert.True(
                TryJunctionXz(in snap, requested, out pinX, out pinZ),
                "pin not in harvest id=" + requested);
            return;
        }

        if (snap.PinX.HasValue && snap.PinZ.HasValue)
        {
            pinX = snap.PinX.Value;
            pinZ = snap.PinZ.Value;
            return;
        }

        Assert.True(TryJunctionXz(in snap, requested ?? snap.PinJunctionId, out pinX, out pinZ));
    }

    internal static RouteCorridorPose DumpedPose(in RouteHarvestSnapshot snap, string? pinId = null)
    {
        Assert.True(snap.NoseX.HasValue && snap.NoseZ.HasValue);
        Assert.True(snap.FwdX.HasValue && snap.FwdZ.HasValue);
        Assert.True(snap.ConsistLengthM.HasValue);
        ResolvePinXz(in snap, pinId, out var pinX, out var pinZ);

        return new RouteCorridorPose(
            snap.NoseX!.Value,
            snap.NoseZ!.Value,
            pinX,
            pinZ,
            snap.FwdX!.Value,
            snap.FwdZ!.Value,
            snap.ConsistLengthM!.Value);
    }

    internal static RouteCorridorPose AlongPinForward(
        in RouteHarvestSnapshot snap,
        float metersAlongFwd,
        string? pinId = null)
    {
        Assert.True(snap.FwdX.HasValue && snap.FwdZ.HasValue);
        Assert.True(snap.ConsistLengthM.HasValue);
        ResolvePinXz(in snap, pinId, out var pinX, out var pinZ);

        var fx = snap.FwdX!.Value;
        var fz = snap.FwdZ!.Value;
        var mag = (float)System.Math.Sqrt((fx * fx) + (fz * fz));
        Assert.True(mag > 1e-6f);
        fx /= mag;
        fz /= mag;
        return new RouteCorridorPose(
            pinX + (metersAlongFwd * fx),
            pinZ + (metersAlongFwd * fz),
            pinX,
            pinZ,
            snap.FwdX.Value,
            snap.FwdZ.Value,
            snap.ConsistLengthM!.Value);
    }
}
