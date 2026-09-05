using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CouplingLinkTests
{
    [Fact]
    public void Resolve_open_loose_linked_mu_warning_mu_team()
    {
        Assert.Equal(
            CouplerLinkStatus.Open,
            CouplingLink.Resolve(
                mechanicallyCoupled: false,
                tightened: false,
                airHoseConnected: false,
                cocksOpenBothSides: false,
                cockOpenThisEnd: false,
                muCablePresent: false,
                muCableConnected: false));

        Assert.Equal(
            CouplerLinkStatus.Loose,
            CouplingLink.Resolve(
                mechanicallyCoupled: true,
                tightened: false,
                airHoseConnected: false,
                cocksOpenBothSides: false,
                cockOpenThisEnd: false,
                muCablePresent: false,
                muCableConnected: false));

        Assert.Equal(
            CouplerLinkStatus.Linked,
            CouplingLink.Resolve(
                mechanicallyCoupled: true,
                tightened: true,
                airHoseConnected: true,
                cocksOpenBothSides: true,
                cockOpenThisEnd: true,
                muCablePresent: false,
                muCableConnected: false));

        Assert.Equal(
            CouplerLinkStatus.MuWarning,
            CouplingLink.Resolve(
                mechanicallyCoupled: true,
                tightened: true,
                airHoseConnected: true,
                cocksOpenBothSides: true,
                cockOpenThisEnd: true,
                muCablePresent: true,
                muCableConnected: false));

        Assert.Equal(
            CouplerLinkStatus.MuTeam,
            CouplingLink.Resolve(
                mechanicallyCoupled: true,
                tightened: true,
                airHoseConnected: true,
                cocksOpenBothSides: true,
                cockOpenThisEnd: true,
                muCablePresent: true,
                muCableConnected: true));
    }

    [Fact]
    public void IsUsable_true_for_linked_mu_warning_and_team()
    {
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.Linked));
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.MuWarning));
        Assert.True(CouplingLink.IsUsable(CouplerLinkStatus.MuTeam));
        Assert.False(CouplingLink.IsUsable(CouplerLinkStatus.Loose));
        Assert.False(CouplingLink.IsUsable(CouplerLinkStatus.Open));
    }

    [Fact]
    public void Mid_couple_progress_any_started_end()
    {
        Assert.True(CouplingLink.HasMidCoupleProgress(
            false, false, false, false, cockOpenThisEnd: true, muCableConnected: false));
        Assert.True(CouplingLink.HasMidCoupleProgress(
            false, false, false, false, cockOpenThisEnd: false, muCableConnected: true));
        Assert.False(CouplingLink.HasMidCoupleProgress(
            false, false, false, false, cockOpenThisEnd: false, muCableConnected: false));
        Assert.True(CouplingLink.IsUsableLink(true, true, true, true));
        Assert.False(CouplingLink.IsUsableLink(true, true, true, false));
    }
}

public class CouplingDisplayTests
{
    [Fact]
    public void Format_null_side_is_dash_couplers()
    {
        Assert.Equal("— Couplers", CouplingDisplay.Format(null, CouplerLinkStatus.Open));
        Assert.Equal("— Couplers", CouplingDisplay.FormatHud(CouplerLinkStatus.Open, null));
    }

    [Fact]
    public void Format_plain_and_hud_colors()
    {
        Assert.Equal(
            "Couplers F+ R+",
            CouplingDisplay.Format(CouplerLinkStatus.Linked, CouplerLinkStatus.Linked));
        Assert.Equal(
            "Couplers F*Y R-",
            CouplingDisplay.Format(CouplerLinkStatus.MuWarning, CouplerLinkStatus.Open));
        var hudLoose = CouplingDisplay.FormatHud(CouplerLinkStatus.Loose, CouplerLinkStatus.MuTeam);
        Assert.Contains(CouplingDisplay.NoGoColor, hudLoose);
        Assert.Contains(CouplingDisplay.MuTeamColor, hudLoose);
        Assert.Contains("F*", hudLoose);
        Assert.Contains("R+", hudLoose);
        var hudWarn = CouplingDisplay.FormatHud(CouplerLinkStatus.MuWarning, CouplerLinkStatus.Open);
        Assert.Contains(CouplingDisplay.MuWarningColor, hudWarn);
    }
}
