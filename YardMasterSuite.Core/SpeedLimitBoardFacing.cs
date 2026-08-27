namespace YardMasterSuite.Core
{
    /// <summary>
    /// Which posted boards apply to our travel direction (v1 1.10).
    /// Mainline: right of travel when track is unknown; board faces us
    /// (<c>sign.forward · travel ≈ −1</c>). Switch duals skip the right-hand
    /// gate when a junction is nearby. Track identity beats lateral corridor.
    /// </summary>
    public static class SpeedLimitBoardFacing
    {
        public const float MinRightLateralMeters = 0.75f;
        public const float MinForwardAlign = 0.5f;
        public const float MaxRightLateralMeters = 20f;
        public const float LateralCorridorSlope = 0.12f;
        public const float MaxLateralCeilingMeters = 60f;
        public const float NearbyEitherSideMeters = 80f;

        public const string KindMainline = "main";
        public const string KindSwitch = "switch";

        public static float MaxLateralFor(float alongMeters)
        {
            var along = alongMeters < 0f ? -alongMeters : alongMeters;
            var widened = MaxRightLateralMeters + (LateralCorridorSlope * along);
            return widened > MaxLateralCeilingMeters ? MaxLateralCeilingMeters : widened;
        }

        public readonly struct Eval
        {
            public Eval(
                bool governs,
                float forwardDot,
                float rightDot,
                float lateralMeters,
                float maxLateralMeters,
                bool onRight,
                bool onOurTrack,
                bool trackKnown,
                string axis,
                float align,
                string kind)
            {
                Governs = governs;
                ForwardDot = forwardDot;
                RightDot = rightDot;
                LateralMeters = lateralMeters;
                MaxLateralMeters = maxLateralMeters;
                OnRight = onRight;
                OnOurTrack = onOurTrack;
                TrackKnown = trackKnown;
                Axis = axis;
                Align = align;
                Kind = kind;
            }

            public bool Governs { get; }
            public float ForwardDot { get; }
            public float RightDot { get; }
            public float LateralMeters { get; }
            public float MaxLateralMeters { get; }
            public bool OnRight { get; }
            public bool OnOurTrack { get; }
            public bool TrackKnown { get; }
            public string Axis { get; }
            public float Align { get; }
            public string Kind { get; }
        }

        public static Eval Evaluate(
            float signForwardX,
            float signForwardZ,
            float signRightX,
            float signRightZ,
            float travelForwardX,
            float travelForwardZ,
            float deltaToSignX,
            float deltaToSignZ,
            bool isSwitchSign,
            bool junctionNearby,
            bool onOurTrack = false,
            bool trackKnown = false)
        {
            if (!TryNormalize(travelForwardX, travelForwardZ, out var tx, out var tz))
            {
                return Reject("none");
            }

            var rx = tz;
            var rz = -tx;
            var lateral = (deltaToSignX * rx) + (deltaToSignZ * rz);
            var along = (deltaToSignX * tx) + (deltaToSignZ * tz);
            var maxLateral = MaxLateralFor(along);
            var onRight = lateral >= MinRightLateralMeters;

            var ours = trackKnown
                ? onOurTrack
                : (lateral < 0f ? -lateral : lateral) <= maxLateral;

            var hasF = TryNormalize(signForwardX, signForwardZ, out var fx, out var fz);
            var hasR = TryNormalize(signRightX, signRightZ, out var srx, out var srz);
            var fDot = hasF ? (fx * tx) + (fz * tz) : 0f;
            var rDot = hasR ? (srx * tx) + (srz * tz) : 0f;
            var facesUs = hasF && fDot <= -MinForwardAlign;
            var alongAbs = along < 0f ? -along : along;
            var nearbyEitherSide = alongAbs <= NearbyEitherSideMeters;
            var sideOk = trackKnown || onRight || nearbyEitherSide;

            if (isSwitchSign && junctionNearby)
            {
                return new Eval(
                    governs: facesUs && ours,
                    fDot,
                    rDot,
                    lateral,
                    maxLateral,
                    onRight,
                    onOurTrack,
                    trackKnown,
                    "switch",
                    fDot,
                    KindSwitch);
            }

            return new Eval(
                governs: sideOk && facesUs && ours,
                fDot,
                rDot,
                lateral,
                maxLateral,
                onRight,
                onOurTrack,
                trackKnown,
                "fwd",
                fDot,
                KindMainline);
        }

        private static Eval Reject(string axis) =>
            new Eval(false, 0f, 0f, 0f, 0f, false, false, false, axis, 0f, KindMainline);

        private static bool TryNormalize(float x, float z, out float nx, out float nz)
        {
            var len = (float)System.Math.Sqrt((x * x) + (z * z));
            if (len < 1e-4f)
            {
                nx = nz = 0f;
                return false;
            }

            nx = x / len;
            nz = z / len;
            return true;
        }
    }
}
