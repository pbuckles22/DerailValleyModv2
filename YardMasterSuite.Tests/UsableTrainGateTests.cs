using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class UsableTrainGateTests
{
    [Fact]
    public void Smoke_on_foot_empty_yard_hides_loco_bar()
    {
        Assert.False(UsableTrainGate.ShouldShowLocoBar(hasUsableLocoTrain: false));
    }

    [Fact]
    public void Smoke_boarded_usable_train_shows_loco_bar()
    {
        Assert.True(UsableTrainGate.ShouldShowLocoBar(hasUsableLocoTrain: true));
    }

    [Fact]
    public void Smoke_look_at_switch_consist_publishes_new_anchor_while_still_usable()
    {
        Assert.True(
            UsableTrainGate.ShouldPublish(
                seeded: true,
                lastUsable: true,
                lastAnchorId: 100,
                usable: true,
                anchorId: 200));
        Assert.False(
            UsableTrainGate.ShouldPublish(
                seeded: true,
                lastUsable: true,
                lastAnchorId: 100,
                usable: true,
                anchorId: 100));
    }
}

public class CabLeverDisplayTests
{
    [Fact]
    public void TrainBrake_product_label_sample()
    {
        Assert.Equal("TrainBrake 35 %", CabLeverDisplay.FormatTrainBrake(35f));
    }
}
