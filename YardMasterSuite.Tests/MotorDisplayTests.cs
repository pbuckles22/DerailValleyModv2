using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MotorDisplayTests
{
    [Fact]
    public void StatusFromSignals_null_when_no_tm_signals()
    {
        Assert.Null(MotorDisplay.StatusFromSignals(null, null, null, null, null));
    }

    [Theory]
    [InlineData(MotorDisplay.TmsFuseOff)]
    [InlineData(MotorDisplay.TmsHasDead)]
    public void StatusFromSignals_dead_from_tms(float tms)
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(tms, temperature: 40f, overheatingThreshold: 105f, workingMotors: 2f, totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_dead_when_working_count_below_total()
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 1f,
                totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_hot_when_temperature_at_or_over_threshold()
    {
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 110f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
    }

    [Theory]
    [InlineData(MotorCabTempBand.Warning)]
    [InlineData(MotorCabTempBand.Critical)]
    [InlineData(MotorCabTempBand.WarningAndCritical)]
    public void StatusFromSignals_hot_when_cab_mu_warning_or_critical(MotorCabTempBand cab)
    {
        Assert.Equal(
            MotorStatus.Hot,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 90f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f,
                cabTempBand: cab));
    }

    [Fact]
    public void StatusFromSignals_ok_when_cool_and_alive()
    {
        Assert.Equal(
            MotorStatus.Ok,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f));
    }

    [Fact]
    public void StatusFromSignals_dead_when_tm_knife_off_even_if_tms_still_ok()
    {
        Assert.Equal(
            MotorStatus.Dead,
            MotorDisplay.StatusFromSignals(
                MotorDisplay.TmsOk,
                temperature: 40f,
                overheatingThreshold: 105f,
                workingMotors: 2f,
                totalMotors: 2f,
                cabTempBand: MotorCabTempBand.Nominal,
                tmFuseOn: false));
    }

    [Fact]
    public void Format_shows_placeholder_and_plain_labels()
    {
        Assert.Equal("— Motors", MotorDisplay.Format(null));
        Assert.Equal("Motors OK", MotorDisplay.Format(MotorStatus.Ok));
        Assert.Equal("Motors Hot", MotorDisplay.Format(MotorStatus.Hot));
        Assert.Equal("Motors Dead", MotorDisplay.Format(MotorStatus.Dead));
    }

    [Fact]
    public void FormatHud_colors_ok_hot_dead()
    {
        Assert.Equal("— Motors", MotorDisplay.FormatHud(null));
        Assert.Equal(
            $"<color={MotorDisplay.OkColor}>Motors OK</color>",
            MotorDisplay.FormatHud(MotorStatus.Ok));
        Assert.Equal(
            $"<color={MotorDisplay.HotColor}>Motors Hot</color>",
            MotorDisplay.FormatHud(MotorStatus.Hot));
        Assert.Equal(
            $"<color={MotorDisplay.DeadColor}>Motors Dead</color>",
            MotorDisplay.FormatHud(MotorStatus.Dead));
    }

    [Fact]
    public void FormatToken_is_ok_hot_dead_or_unknown()
    {
        Assert.Equal("—", MotorDisplay.FormatToken(null));
        Assert.Equal("OK", MotorDisplay.FormatToken(MotorStatus.Ok));
        Assert.Equal("Hot", MotorDisplay.FormatToken(MotorStatus.Hot));
        Assert.Equal("Dead", MotorDisplay.FormatToken(MotorStatus.Dead));
        Assert.Equal(int.MinValue, MotorDisplay.Bucket(null));
        Assert.Equal((int)MotorStatus.Ok, MotorDisplay.Bucket(MotorStatus.Ok));
    }
}
