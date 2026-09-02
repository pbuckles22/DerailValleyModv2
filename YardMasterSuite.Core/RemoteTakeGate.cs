namespace YardMasterSuite.Core;

/// <summary>Why Unity should call <c>JobsManager.TakeJob</c> (**13.6.1**).</summary>
public enum RemoteTakeDecision
{
    NoOp = 0,
    Request = 1,
    RefuseOfficeRequired = 2,
    RefuseNotOnList = 3,
}

/// <summary>Desk Take vs first Transit GO.</summary>
public enum RemoteTakeSource
{
    Desk = 0,
    Go = 1,
}

/// <summary>
/// Preview ticket + Switch List arm. Preview meters are ignored — countdown
/// OUT is not a take.
/// </summary>
public readonly struct RemoteTakeInput
{
    public readonly bool PreviewHeld;
    public readonly bool AlreadyTaken;
    public readonly bool SwitchListLoaded;
    public readonly bool ListJobMatchesHeld;
    public readonly bool GoArm;
    public readonly bool DeskTake;
    public readonly bool ApiAllowsTake;
    public readonly float? PreviewMetersRemaining;

    public RemoteTakeInput(
        bool previewHeld,
        bool alreadyTaken,
        bool switchListLoaded,
        bool listJobMatchesHeld,
        bool goArm,
        bool deskTake,
        bool apiAllowsTake,
        float? previewMetersRemaining)
    {
        PreviewHeld = previewHeld;
        AlreadyTaken = alreadyTaken;
        SwitchListLoaded = switchListLoaded;
        ListJobMatchesHeld = listJobMatchesHeld;
        GoArm = goArm;
        DeskTake = deskTake;
        ApiAllowsTake = apiAllowsTake;
        PreviewMetersRemaining = previewMetersRemaining;
    }
}

/// <summary>Pure take-request gate. Unity owns <c>JobsManager.TakeJob</c>.</summary>
public static class RemoteTakeGate
{
    public static bool CanOfferDeskTake(
        bool previewHeld,
        bool alreadyTaken,
        bool switchListLoaded,
        bool listJobMatchesHeld,
        bool apiAllowsTake) =>
        previewHeld
        && !alreadyTaken
        && switchListLoaded
        && listJobMatchesHeld
        && apiAllowsTake;

    public static bool ListJobMatches(string? heldJobId, string? listJobId)
    {
        var held = heldJobId?.Trim();
        var list = listJobId?.Trim();
        return !string.IsNullOrEmpty(held)
            && !string.IsNullOrEmpty(list)
            && string.Equals(held, list, System.StringComparison.OrdinalIgnoreCase);
    }

    public static RemoteTakeDecision Evaluate(in RemoteTakeInput input)
    {
        _ = input.PreviewMetersRemaining;

        if (input.AlreadyTaken || !input.PreviewHeld)
        {
            return RemoteTakeDecision.NoOp;
        }

        if (!input.DeskTake && !input.GoArm)
        {
            return RemoteTakeDecision.NoOp;
        }

        if (!input.SwitchListLoaded || !input.ListJobMatchesHeld)
        {
            return RemoteTakeDecision.RefuseNotOnList;
        }

        if (!input.ApiAllowsTake)
        {
            return RemoteTakeDecision.RefuseOfficeRequired;
        }

        return RemoteTakeDecision.Request;
    }
}
