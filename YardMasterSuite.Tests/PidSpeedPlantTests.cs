using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// DE2 plant rejects off-grid levers (2.9.1.6 <c>thr=0.125</c> /
/// <c>indy=0.22</c> did nothing in the cab).
/// </summary>
public class PidSpeedPlantTests
{
    [Fact]
    public void PidSpeedPlant_WhenExpanderYieldsOffGrid_CoastsToGearShift()
    {
        var independent = PidSpeedNotch.ApplyExpander(3f / 11f, 0f, firstPunchFromZero: false);
        Assert.Equal(0f, independent);
        Assert.False(PidSpeedNotch.IsExact(3f / 11f));

        var offGrid = 26f;
        var hud = 26f;
        var alongOff = 0f;
        var alongHud = 0f;
        for (var i = 0; i < 80; i++)
        {
            PidSpeedPlant.Step(ref offGrid, ref alongOff, 0.09f, independent, 0.05f, LocoTypeId.De2);
            PidSpeedPlant.Step(
                ref hud,
                ref alongHud,
                0.09f,
                PidSpeedHold.OverspeedIndependent,
                0.05f,
                LocoTypeId.De2);
        }

        Assert.True(offGrid > hud + 2f);
        Assert.True(hud < 26f);
    }

    [Fact]
    public void Smoke_9_1_de2_plant_rejects_two_elevenths_accepts_hud_18()
    {
        var off = 10f;
        var hud = 10f;
        var along = 0f;
        PidSpeedPlant.Step(ref off, ref along, 2f / 11f, 0f, 1f, LocoTypeId.De2);
        along = 0f;
        PidSpeedPlant.Step(ref hud, ref along, 0.18f, 0f, 1f, LocoTypeId.De2);
        Assert.True(hud > off);
    }

    [Fact]
    public void Smoke_9_1_de2_plant_rejects_off_grid_throttle()
    {
        var off = 10f;
        var exact = 10f;
        var along = 0f;
        PidSpeedPlant.Step(ref off, ref along, 0.125f, 0f, 1f, LocoTypeId.De2);
        along = 0f;
        PidSpeedPlant.Step(ref exact, ref along, PidSpeedNotch.Step, 0f, 1f, LocoTypeId.De2);
        Assert.True(exact > off);
        Assert.True(off < 10f);
    }

    [Fact]
    public void Smoke_9_1_de2_plant_rejects_off_grid_independent()
    {
        var rejected = 40f;
        var notched = 40f;
        var along = 0f;
        PidSpeedPlant.Step(ref rejected, ref along, 0f, 0.22f, 1f, LocoTypeId.De2);
        along = 0f;
        PidSpeedPlant.Step(
            ref notched,
            ref along,
            0f,
            PidSpeedHold.OverspeedIndependent,
            1f,
            LocoTypeId.De2);
        Assert.True(rejected > notched);
    }

    [Fact]
    public void Smoke_9_1_non_de2_plant_keeps_analog()
    {
        var de2 = 10f;
        var dh4 = 10f;
        var along = 0f;
        PidSpeedPlant.Step(ref de2, ref along, 0.5f, 0f, 1f, LocoTypeId.De2);
        along = 0f;
        PidSpeedPlant.Step(ref dh4, ref along, 0.5f, 0f, 1f, "DH4");
        Assert.True(dh4 > de2);
    }
}
