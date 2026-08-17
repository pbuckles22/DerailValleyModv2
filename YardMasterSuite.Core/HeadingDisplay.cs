using System;
using System.Text;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Personal compass math (Unity-free). World +Z = north; degrees increase
    /// clockwise toward +X. 16-point rose only — no degree readout.
    /// </summary>
    public static class HeadingDisplay
    {
        public const int PointCount = 16;
        public const int UnknownIndex = -1;

        private const float MinForwardSqr = 1e-8f;
        private const double Rad2Deg = 180.0 / Math.PI;
        private const double SectorDegrees = 22.5;

        private static readonly string[] Points =
        {
            "N", "NNE", "NE", "ENE",
            "E", "ESE", "SE", "SSE",
            "S", "SSW", "SW", "WSW",
            "W", "WNW", "NW", "NNW",
        };

        public static float? FromForward(float x, float z)
        {
            if (x * x + z * z < MinForwardSqr)
            {
                return null;
            }

            var heading = Math.Atan2(x, z) * Rad2Deg;
            if (heading < 0)
            {
                heading += 360.0;
            }

            return (float)heading;
        }

        public static int ToPointIndex(float? degrees)
        {
            if (degrees is null)
            {
                return UnknownIndex;
            }

            var normalized = degrees.Value % 360f;
            if (normalized < 0)
            {
                normalized += 360f;
            }

            var index = (int)Math.Floor((normalized + SectorDegrees / 2.0) / SectorDegrees) % PointCount;
            if (index < 0)
            {
                index += PointCount;
            }

            return index;
        }

        public static string? PointName(int index)
        {
            if (index < 0 || index >= PointCount)
            {
                return null;
            }

            return Points[index];
        }

        public static string? ToCompassPoint(float? degrees) =>
            PointName(ToPointIndex(degrees));

        public static void AppendLabel(StringBuilder sb, int index)
        {
            var name = PointName(index);
            if (name == null)
            {
                sb.Append("— Heading");
                return;
            }

            sb.Append("Heading ");
            sb.Append(name);
        }
    }
}
