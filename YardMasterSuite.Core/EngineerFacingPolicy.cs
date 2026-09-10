namespace YardMasterSuite.Core;

/// <summary>
/// Engineer travel for GO. Track-correct Dijkstra can be all-Forward hops
/// while the loco nose still faces the wrong way (cab 2.13.2.5.18 B4L → C4S:
/// reverseCount=0, pin behind, bind said Forward).
/// Bind is a desk label. First-hop reverse, when present, forces Reverse.
/// Otherwise an armed pin's travel bit owns GO.
/// </summary>
public static class EngineerFacingPolicy
{
    /// <summary>
    /// <see langword="true"/> when the first corridor hop is reverse.
    /// <see langword="null"/> when the graph does not force gear (pose/pin must).
    /// Never returns <see langword="false"/> — missing reverse hops are not
    /// "force Forward."
    /// </summary>
    public static bool? PlanTravelReverse(PathPlanResult? plan)
    {
        if (plan != null && plan.FirstHopRequiresReverse)
        {
            return true;
        }

        return null;
    }

    public static bool BindLies(bool engineerNeedsReverse, bool bindNeedsReverse) =>
        engineerNeedsReverse != bindNeedsReverse;

    public static bool HasDirectionChange(bool previousTravelReverse, bool nextTravelReverse) =>
        previousTravelReverse != nextTravelReverse;

    /// <summary>
    /// GO reverser. Graph first-hop reverse forces Reverse; else pin travel;
    /// else bind; else dest-behind.
    /// </summary>
    public static bool GoNeedsReverse(
        bool? planTravelReverse,
        bool? bindNeedsReverse,
        bool pinStepActive,
        bool pinTravelReverse,
        bool destBehind,
        RouteClearancePhase clearancePhase = RouteClearancePhase.Idle)
    {
        if (planTravelReverse == true)
        {
            return true;
        }

        if (pinStepActive)
        {
            return RouteFacingPhasePolicy.FacingNeedsReverse(
                clearancePhase,
                pinArmedForClearance: true,
                pinLatched: true,
                pinTravelReverse: pinTravelReverse,
                pinBehindLive: pinTravelReverse,
                destBehindLive: destBehind);
        }

        if (bindNeedsReverse is bool bind)
        {
            return bind;
        }

        return destBehind;
    }
}
