using System;

namespace YardMasterSuite.Core
{
    /// <summary>Left/right turn-cue side for behind-camera sticky markers.</summary>
    public enum ArHorizontalEdge
    {
        None = 0,
        Left = 1,
        Right = 2,
    }

    /// <summary>
    /// Angular deadband so looking almost directly away does not flip L↔R every frame.
    /// Uses atan2(viewRight, −viewForward) so thresholds are distance-independent.
    /// </summary>
    public static class ArEdgeHysteresis
    {
        public const float EnterRadians = 0.12f;
        public const float HoldRadians = 0.05f;

        public static float BehindBearingRadians(float viewRight, float viewForward)
        {
            var behindAxis = -viewForward;
            if (behindAxis < 1e-3f)
            {
                behindAxis = 1e-3f;
            }

            return (float)Math.Atan2(viewRight, behindAxis);
        }

        public static ArHorizontalEdge Resolve(
            float viewRight,
            float viewForward,
            ArHorizontalEdge previous,
            float enterRad = EnterRadians,
            float holdRad = HoldRadians)
        {
            if (holdRad > enterRad)
            {
                holdRad = enterRad;
            }

            var bearing = BehindBearingRadians(viewRight, viewForward);

            switch (previous)
            {
                case ArHorizontalEdge.Left:
                    return bearing > holdRad ? ArHorizontalEdge.Right : ArHorizontalEdge.Left;
                case ArHorizontalEdge.Right:
                    return bearing < -holdRad ? ArHorizontalEdge.Left : ArHorizontalEdge.Right;
                default:
                    if (bearing > enterRad)
                    {
                        return ArHorizontalEdge.Right;
                    }

                    if (bearing < -enterRad)
                    {
                        return ArHorizontalEdge.Left;
                    }

                    return ArHorizontalEdge.Left;
            }
        }
    }
}
