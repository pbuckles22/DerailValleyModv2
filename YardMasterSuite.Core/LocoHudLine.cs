using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// Stop-state loco bar (**3.3.1** / Epic **6.8** subset): center-weighted v1 chip order via
/// <see cref="TrainHudLine"/> — levers → Speed · Limit → Cars.
/// </summary>
public static class LocoHudLine
{
    public static void AppendStopState(
        StringBuilder sb,
        float? reverser01,
        float? throttlePct,
        float? indyPct,
        float? trainBrakePct,
        string speedLabel,
        string limitLabel,
        int? carCount,
        float? massTonnes,
        string? fuel = null,
        string? oil = null,
        string? grade = null,
        string? load = null,
        string? motors = null,
        string? handbrakes = null,
        string? stress = null,
        string? freeMotion = null)
    {
        var line = TrainHudLine.Format(
            fuel ?? string.Empty,
            oil ?? string.Empty,
            massTonnes is null ? string.Empty : TonnageDisplay.FormatTonnes(massTonnes),
            grade ?? string.Empty,
            load ?? string.Empty,
            speedLabel,
            limitLabel,
            motors ?? string.Empty,
            handbrakes ?? string.Empty,
            carCount is null ? string.Empty : CarsDisplay.Format(carCount),
            reverser: reverser01 is null ? string.Empty : ReverserDisplay.FormatHud(reverser01),
            throttle: throttlePct is null ? string.Empty : CabLeverDisplay.FormatThrottle(throttlePct),
            indy: indyPct is null ? string.Empty : CabLeverDisplay.FormatIndy(indyPct),
            trainBrake: trainBrakePct is null ? string.Empty : CabLeverDisplay.FormatTrainBrake(trainBrakePct),
            stress: stress ?? string.Empty,
            freeMotion: freeMotion ?? string.Empty);

        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        sb.Append(line);
    }
}
