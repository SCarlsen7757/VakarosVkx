namespace Vakaros.Vkx.Web.Client.Components.Shared;

/// <summary>
/// Utility methods for computing derived sailing channels from raw telemetry.
/// </summary>
public static class TelemetryMath
{
    /// <summary>
    /// Compute VMG (Velocity Made Good) = SOG × cos(COG − WindDirection).
    /// All angles in degrees.
    /// </summary>
    public static double ComputeVmg(double sogKnots, double cogDegrees, double windDirectionDegrees)
    {
        var angleDiff = (cogDegrees - windDirectionDegrees) * Math.PI / 180.0;
        return sogKnots * Math.Cos(angleDiff);
    }

    /// <summary>
    /// Compute heel (roll) angle from quaternion, returned in degrees.
    /// Positive = starboard heel.
    /// </summary>
    public static double ComputeHeel(float qw, float qx, float qy, float qz)
    {
        var sinRoll = 2.0 * (qw * qx + qy * qz);
        var cosRoll = 1.0 - 2.0 * (qx * qx + qy * qy);
        return Math.Atan2(sinRoll, cosRoll) * 180.0 / Math.PI;
    }

    /// <summary>
    /// Compute trim (pitch) angle from quaternion, returned in degrees.
    /// Positive = bow up.
    /// </summary>
    public static double ComputeTrim(float qw, float qx, float qy, float qz)
    {
        var sinPitch = 2.0 * (qw * qy - qz * qx);
        sinPitch = Math.Clamp(sinPitch, -1.0, 1.0);
        return Math.Asin(sinPitch) * 180.0 / Math.PI;
    }
}
