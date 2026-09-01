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

    internal static string Dir =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Htp");

    internal static string CorridorPath => Path.Combine(Dir, CorridorFileName);

    internal static string GraphPath => Path.Combine(Dir, GraphFileName);

    internal static string Pid2918Path => Path.Combine(Dir, Pid2918FileName);

    internal static string BoardsSw20260831Path => Path.Combine(Dir, BoardsSw20260831FileName);

    internal static string GraphSw20260901Path => Path.Combine(Dir, GraphSw20260901FileName);

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

    internal static RouteCorridorPose DumpedPose(in RouteHarvestSnapshot snap, string? pinId = null)
    {
        Assert.True(snap.NoseX.HasValue && snap.NoseZ.HasValue);
        Assert.True(snap.FwdX.HasValue && snap.FwdZ.HasValue);
        Assert.True(snap.ConsistLengthM.HasValue);
        float pinX, pinZ;
        if (snap.PinX.HasValue && snap.PinZ.HasValue)
        {
            pinX = snap.PinX.Value;
            pinZ = snap.PinZ.Value;
        }
        else
        {
            Assert.True(TryJunctionXz(in snap, pinId ?? snap.PinJunctionId, out pinX, out pinZ));
        }

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
        float pinX, pinZ;
        if (snap.PinX.HasValue && snap.PinZ.HasValue)
        {
            pinX = snap.PinX.Value;
            pinZ = snap.PinZ.Value;
        }
        else
        {
            Assert.True(TryJunctionXz(in snap, pinId ?? snap.PinJunctionId, out pinX, out pinZ));
        }

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
