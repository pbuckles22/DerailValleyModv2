using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>8.5 face-into-Exit inject polarity (v1 3.7 / MF log vectors @ 0.6.28).</summary>
public class SwitchListTurnAroundTests
{
    // Locked MF vectors: Exit ≈ south; face N → Prep-ready; face S into Exit → table.
    private const float ExitDx = -3.47f;
    private const float ExitDz = -125f;

    [Fact]
    public void NeedsTurntableBeforePrep_log_MF_face_N_exit_S_no_table()
    {
        Assert.False(SwitchListTurnAround.NeedsTurntableBeforePrep(0.03f, 1f, ExitDx, ExitDz));
    }

    [Fact]
    public void NeedsTurntableBeforePrep_face_into_exit_S_needs_table()
    {
        Assert.True(SwitchListTurnAround.NeedsTurntableBeforePrep(0f, -1f, ExitDx, ExitDz));
    }

    [Fact]
    public void NeedsTurntableBeforePrep_missing_vectors_fail_closed()
    {
        Assert.False(SwitchListTurnAround.NeedsTurntableBeforePrep(0f, 0f, ExitDx, ExitDz));
        Assert.False(SwitchListTurnAround.NeedsTurntableBeforePrep(0f, -1f, 0f, 0f));
    }

    [Fact]
    public void NeedsReverseInto_when_last_hop_requires_reverse()
    {
        Assert.True(SwitchListTurnAround.NeedsReverseInto(lastHopRequiresReverse: true));
        Assert.False(SwitchListTurnAround.NeedsReverseInto(lastHopRequiresReverse: false));
    }

    [Fact]
    public void ResolveTurntable_prefers_origin_yard_when_pathable()
    {
        var candidates = new[]
        {
            new TurntableCandidate("DEST-TT", "SM", 10f),
            new TurntableCandidate("ORIG-TT", "CS", 50f),
        };
        var pick = SwitchListTurnAround.ResolveTurntable(
            "CS",
            "SM",
            candidates,
            tableId => tableId == "ORIG-TT" || tableId == "DEST-TT");
        Assert.Equal("ORIG-TT", pick);
    }

    [Fact]
    public void ResolveTurntable_falls_back_to_dest_yard()
    {
        var candidates = new[]
        {
            new TurntableCandidate("DEST-TT", "SM", 10f),
            new TurntableCandidate("ORIG-TT", "CS", 50f),
        };
        var pick = SwitchListTurnAround.ResolveTurntable(
            "CS",
            "SM",
            candidates,
            tableId => tableId == "DEST-TT");
        Assert.Equal("DEST-TT", pick);
    }

    [Fact]
    public void ResolveTurntable_null_when_none_pathable()
    {
        var candidates = new[]
        {
            new TurntableCandidate("ORIG-TT", "CS", 10f),
        };
        Assert.Null(SwitchListTurnAround.ResolveTurntable(
            "CS",
            "SM",
            candidates,
            _ => false));
    }
}
