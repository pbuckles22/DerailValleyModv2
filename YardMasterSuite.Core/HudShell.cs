using System.Text;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Zero-alloc HUD label builders. Unity OnGUI assigns GUIContent.text only
    /// when <see cref="GuiContentCache.TryCommit"/> returns true.
    /// Stack order: loco → look-at → job → always-on (bottom).
    /// </summary>
    public static class HudShell
    {
        public const int SlotLocoBar = 0;
        public const int SlotLookAtBar = 1;
        public const int SlotJobBar = 2;
        public const int SlotAlwaysOnBar = 3;
        public const int SlotCount = 4;

        public static bool ShouldDraw(bool playerTransformPresent) =>
            HudWorldSession.IsActive(playerTransformPresent);

        public static bool ShouldDrawLocoBar(bool hasUsableLocoTrain) =>
            UsableTrainGate.ShouldShowLocoBar(hasUsableLocoTrain);

        public static void AppendAlwaysOn(
            StringBuilder sb,
            int headingIndex,
            string? marked = null,
            string? station = null,
            string? path = null,
            string? clock = null)
        {
            var heading = HeadingDisplay.PointName(headingIndex) is { } name
                ? "Heading " + name
                : "— Heading";
            sb.Append(AlwaysOnHudLine.Format(
                heading,
                park: marked,
                station: station,
                path: path,
                clock: clock,
                version: null));
        }

        public static void AppendLocoStopState(
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
            string? freeMotion = null,
            string? backup = null)
        {
            LocoHudLine.AppendStopState(
                sb,
                reverser01,
                throttlePct,
                indyPct,
                trainBrakePct,
                speedLabel,
                limitLabel,
                carCount,
                massTonnes,
                fuel,
                oil,
                grade,
                load,
                motors,
                handbrakes,
                stress,
                freeMotion,
                backup);
        }

        public static bool ShouldDrawTopBar(bool hasUsable, bool hasCab) =>
            ShouldDrawLocoBar(hasUsable);

        public static void AppendLocoBar(
            StringBuilder sb,
            bool hasUsable,
            int cars,
            int tonnes,
            bool hasCab,
            int thr,
            int indy,
            int train,
            bool engPresent,
            int eng,
            int rev,
            string speedLabel,
            string limitLabel) =>
            AppendLocoStopState(
                sb,
                hasCab ? rev / 100f : (float?)null,
                hasCab ? thr : (float?)null,
                hasCab ? indy : (float?)null,
                hasCab ? train : (float?)null,
                speedLabel,
                limitLabel,
                hasUsable ? cars : (int?)null,
                hasUsable ? tonnes : (float?)null);

        /// <summary>Legacy top bar without Speed/Limit (tests / migration).</summary>
        public static void AppendTopBar(
            StringBuilder sb,
            bool hasUsable,
            int? cars,
            float? tonnes,
            bool hasCab,
            float? reverser01,
            float? throttlePct,
            float? indyPct,
            float? trainBrakePct)
        {
            if (!hasUsable)
            {
                return;
            }

            AppendLocoStopState(
                sb,
                hasCab ? reverser01 : null,
                hasCab ? throttlePct : null,
                hasCab ? indyPct : null,
                hasCab ? trainBrakePct : null,
                speedLabel: string.Empty,
                limitLabel: string.Empty,
                carCount: cars,
                massTonnes: tonnes);
        }

        public static void AppendHeading(StringBuilder sb, int pointIndex)
        {
            HeadingDisplay.AppendLabel(sb, pointIndex);
        }
    }
}
