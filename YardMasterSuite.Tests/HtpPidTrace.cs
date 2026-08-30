using YardMasterSuite.Core;
using Xunit.Abstractions;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP study transcript: try (PID/write) vs applied (expander+plant).
/// Change-only. xUnit shows <see cref="ITestOutputHelper"/> on fail.
/// </summary>
internal sealed class HtpPidTrace
{
    private readonly ITestOutputHelper _output;
    private PidSpeedWalkTraceCache _cache;

    public HtpPidTrace(ITestOutputHelper output) => _output = output;

    public int Events { get; private set; }

    public void Tick(in PidSpeedCommand cmd, float appliedThrottle, float appliedIndependent, float speedKmh)
    {
        var mode = PidSpeedTelemetry.Mode(
            armed: true,
            derailIntervening: false,
            cmd.GearPending,
            cmd.BrakePending);
        var speed = (int)Math.Round(speedKmh, MidpointRounding.AwayFromZero);
        var tryThr = ControlTelemetry.ToPct(cmd.DesiredThrottle);
        var tryIndy = ControlTelemetry.ToPct(cmd.DesiredIndependent);
        var appliedThr = ControlTelemetry.ToPct(appliedThrottle);
        var appliedIndy = ControlTelemetry.ToPct(appliedIndependent);
        if (!PidSpeedWalkTrace.Observe(
                speed,
                tryThr,
                tryIndy,
                appliedThr,
                appliedIndy,
                mode,
                ref _cache))
        {
            return;
        }

        Events++;
        _output.WriteLine(
            "HTP try thr=" + tryThr
            + " indy=" + tryIndy
            + " | applied thr=" + appliedThr
            + " indy=" + appliedIndy
            + " | speed=" + speed
            + " "
            + mode);
    }
}
