using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Smoke harvest: look-at Job chip (**6.13** / v1 4.2).</summary>
public class LocalCarHudLineTests
{
    [Fact]
    public void Smoke_look_at_job_car_shows_job_chip()
    {
        var line = LocalCarHudLine.Format(
            "Pipe 72",
            "HB 0",
            "Couplers --",
            "Car 1",
            JobDisplay.Format("FH-123"),
            "SW-B3I");

        Assert.Contains("Job FH-123", line);
        Assert.Contains("Car 1", line);
    }

    [Fact]
    public void Smoke_look_at_without_job_omits_job_chip()
    {
        var line = LocalCarHudLine.Format(
            "Pipe 72",
            "HB 0",
            "Couplers --",
            "Car 1",
            JobDisplay.Format(null),
            "SW-B3I");

        Assert.DoesNotContain("Job", line);
        Assert.Contains("Car 1", line);
    }
}
