using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitBoardFacingTests
{
    private static SpeedLimitBoardFacing.Eval Board(
        float lateral,
        float along,
        float signForwardZ = -1f,
        bool isSwitchSign = false,
        bool junctionNearby = false,
        bool onOurTrack = false,
        bool trackKnown = false) =>
        SpeedLimitBoardFacing.Evaluate(
            signForwardX: 0f,
            signForwardZ: signForwardZ,
            signRightX: 1f,
            signRightZ: 0f,
            travelForwardX: 0f,
            travelForwardZ: 1f,
            deltaToSignX: lateral,
            deltaToSignZ: along,
            isSwitchSign,
            junctionNearby,
            onOurTrack,
            trackKnown);

    [Fact]
    public void Mainline_governs_board_on_right_turned_toward_us()
    {
        var ok = Board(lateral: 2f, along: -5f);
        Assert.True(ok.Governs);
        Assert.Equal(SpeedLimitBoardFacing.KindMainline, ok.Kind);
        Assert.True(ok.ForwardDot < 0f);
    }

    [Fact]
    public void Board_alongside_facing_us_governs()
    {
        Assert.True(Board(lateral: 2f, along: -0.4f).Governs);
    }

    [Fact]
    public void Mainline_rejects_board_facing_away()
    {
        Assert.False(Board(lateral: 2f, along: -5f, signForwardZ: 1f).Governs);
        Assert.False(Board(lateral: -2f, along: -5f, signForwardZ: 1f).Governs);
    }

    [Fact]
    public void Smoke_facing_40_on_left_nearby_governs()
    {
        var leftAhead = Board(lateral: -2f, along: 12f);
        Assert.True(leftAhead.Governs);
        Assert.False(leftAhead.OnRight);

        var leftJustPassed = Board(lateral: -2f, along: -5f);
        Assert.True(leftJustPassed.Governs);
    }

    [Fact]
    public void Mainline_rejects_distant_board_on_left()
    {
        Assert.False(Board(lateral: -2f, along: 200f).Governs);
        Assert.False(Board(lateral: -2f, along: -5f, signForwardZ: 1f).Governs);
    }

    [Fact]
    public void On_path_mainline_skips_right_hand_when_track_known()
    {
        var leftButOurs = Board(
            lateral: -0.4f,
            along: 29.6f,
            onOurTrack: true,
            trackKnown: true);
        Assert.True(leftButOurs.Governs);
        Assert.False(leftButOurs.OnRight);
    }

    [Fact]
    public void Track_identity_overrides_lateral_distance()
    {
        var ours = Board(lateral: 45f, along: -5f, onOurTrack: true, trackKnown: true);
        Assert.True(ours.Governs);
        Assert.True(ours.OnOurTrack);

        var theirs = Board(lateral: 2f, along: -5f, onOurTrack: false, trackKnown: true);
        Assert.False(theirs.Governs);
        Assert.True(theirs.TrackKnown);
    }

    [Fact]
    public void Corridor_is_the_fallback_when_track_is_unresolved()
    {
        var ghost = Board(lateral: 144.6f, along: -1.4f);
        Assert.False(ghost.Governs);
        Assert.False(ghost.TrackKnown);
        Assert.True(ghost.LateralMeters > ghost.MaxLateralMeters);

        Assert.True(Board(lateral: 20f, along: 213f).Governs);
    }

    [Fact]
    public void Corridor_widens_with_along_distance_but_is_capped()
    {
        Assert.Equal(
            SpeedLimitBoardFacing.MaxRightLateralMeters,
            SpeedLimitBoardFacing.MaxLateralFor(0f));
        Assert.True(
            SpeedLimitBoardFacing.MaxLateralFor(-250f) > SpeedLimitBoardFacing.MaxLateralFor(-50f));
        Assert.Equal(
            SpeedLimitBoardFacing.MaxLateralCeilingMeters,
            SpeedLimitBoardFacing.MaxLateralFor(5000f));
    }

    [Fact]
    public void Switch_dual_at_junction_skips_right_hand_rule()
    {
        var sw = Board(
            lateral: -2f,
            along: 10f,
            isSwitchSign: true,
            junctionNearby: true,
            onOurTrack: true,
            trackKnown: true);
        Assert.True(sw.Governs);
        Assert.Equal(SpeedLimitBoardFacing.KindSwitch, sw.Kind);
    }

    [Fact]
    public void Switch_dual_without_junction_falls_back_to_mainline_on_path_rules()
    {
        var noJunction = Board(
            lateral: -2f,
            along: 10f,
            isSwitchSign: true,
            junctionNearby: false,
            onOurTrack: true,
            trackKnown: true);
        Assert.True(noJunction.Governs);

        var otherTrack = Board(
            lateral: -2f,
            along: 10f,
            isSwitchSign: true,
            junctionNearby: true,
            onOurTrack: false,
            trackKnown: true);
        Assert.False(otherTrack.Governs);
    }

    [Fact]
    public void Switch_dual_facing_away_does_not_govern()
    {
        var away = Board(
            lateral: -2f,
            along: 10f,
            signForwardZ: 1f,
            isSwitchSign: true,
            junctionNearby: true,
            onOurTrack: true,
            trackKnown: true);
        Assert.False(away.Governs);
    }

    [Fact]
    public void On_path_uses_route_tangent_not_skewed_loco_heading()
    {
        var withTangent = SpeedLimitBoardFacing.Evaluate(
            signForwardX: 0f,
            signForwardZ: -1f,
            signRightX: 1f,
            signRightZ: 0f,
            travelForwardX: 0f,
            travelForwardZ: 1f,
            deltaToSignX: 2f,
            deltaToSignZ: 12f,
            isSwitchSign: false,
            junctionNearby: false,
            onOurTrack: true,
            trackKnown: true);
        Assert.True(withTangent.Governs);
        Assert.True(withTangent.ForwardDot <= -SpeedLimitBoardFacing.MinForwardAlign);

        const float yawDeg = 67f;
        var yaw = yawDeg * (float)System.Math.PI / 180f;
        var locoX = (float)System.Math.Sin(yaw);
        var locoZ = (float)System.Math.Cos(yaw);
        var withLocoHeading = SpeedLimitBoardFacing.Evaluate(
            signForwardX: 0f,
            signForwardZ: -1f,
            signRightX: 1f,
            signRightZ: 0f,
            travelForwardX: locoX,
            travelForwardZ: locoZ,
            deltaToSignX: 2f,
            deltaToSignZ: 12f,
            isSwitchSign: false,
            junctionNearby: false,
            onOurTrack: true,
            trackKnown: true);
        Assert.InRange(withLocoHeading.ForwardDot, -0.45f, -0.30f);
        Assert.False(withLocoHeading.Governs);
    }
}
