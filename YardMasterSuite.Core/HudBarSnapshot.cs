namespace YardMasterSuite.Core;

/// <summary>Type A payload for a single centered HUD bar line.</summary>
public readonly struct HudBarSnapshot
{
    public readonly string Text;
    public readonly bool Visible;

    public HudBarSnapshot(string text, bool visible = true)
    {
        Text = text ?? string.Empty;
        Visible = visible && !string.IsNullOrWhiteSpace(text);
    }
}

/// <summary>Type A payload for usable-loco-train gate (**4.3**).</summary>
public readonly struct UsableTrainState
{
    public readonly bool HasUsableLocoTrain;

    public UsableTrainState(bool hasUsableLocoTrain) => HasUsableLocoTrain = hasUsableLocoTrain;
}
