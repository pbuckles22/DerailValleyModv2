using System;

namespace YardMasterSuite.Core;

/// <summary>
/// 1-D along-track plant for HTP CP1. Not Unity rigidbody. Shared numbers
/// with <see cref="PidSpeedHold"/> feedforward so the walk is the same model.
/// </summary>
public static class PidSpeedPlant
{
    public const float MaxAccelKmhPerS = 8f;
    public const float DragPerKmh = 0.08f;
    public const float BrakeDecelKmhPerS = 18f;

    public static void Step(
        ref float speedKmh,
        ref float alongM,
        float throttle,
        float independent,
        float dt,
        string? locoTypeId = LocoTypeId.De2)
    {
        var t = Clamp01(throttle);
        var b = Clamp01(independent);
        if (locoTypeId == null || LocoTypeId.IsDe2(locoTypeId))
        {
            if (!PidSpeedNotch.IsExact(t))
            {
                t = 0f;
            }

            if (!PidSpeedNotch.IsExact(b))
            {
                b = 0f;
            }
        }

        var d = Math.Max(0f, dt);
        var speed = speedKmh < 0f || float.IsNaN(speedKmh) ? 0f : speedKmh;
        var accel = (t * MaxAccelKmhPerS) - (b * BrakeDecelKmhPerS) - (DragPerKmh * speed);
        speed = Math.Max(0f, speed + (accel * d));
        alongM += SpeedDisplay.ToMetersPerSecond(speed) * d;
        speedKmh = speed;
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
