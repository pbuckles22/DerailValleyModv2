using System.Text;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Zero-alloc HUD label builders. Unity OnGUI assigns GUIContent.text only
    /// when <see cref="GuiContentCache.TryCommit"/> returns true.
    /// </summary>
    public static class HudShell
    {
        public const int SlotCompass = 0;
        public const int SlotTopBar = 1;
        public const int SlotCount = 2;

        public static bool ShouldDraw(bool playerTransformPresent) =>
            HudWorldSession.IsActive(playerTransformPresent);

        public static bool ShouldDrawTopBar(bool hasConsist, bool hasCab) =>
            hasConsist || hasCab;

        public static void AppendCompass(StringBuilder sb, int pointIndex)
        {
            HeadingDisplay.AppendLabel(sb, pointIndex);
        }

        public static void AppendTopBar(
            StringBuilder sb,
            bool hasConsist,
            int cars,
            int tonnes,
            bool hasCab,
            int thr,
            int indy,
            int train,
            bool engPresent,
            int eng,
            int rev)
        {
            if (hasConsist)
            {
                sb.Append("cars=");
                sb.Append(cars);
                sb.Append(" t=");
                sb.Append(tonnes);
            }

            if (!hasCab)
            {
                return;
            }

            if (hasConsist)
            {
                sb.Append(" | ");
            }

            sb.Append("thr=");
            sb.Append(thr);
            sb.Append(" indy=");
            sb.Append(indy);
            sb.Append(" train=");
            sb.Append(train);
            sb.Append(" eng=");
            if (engPresent)
            {
                sb.Append(eng);
            }
            else
            {
                sb.Append("na");
            }

            sb.Append(" rev=");
            sb.Append(rev);
        }
    }
}
