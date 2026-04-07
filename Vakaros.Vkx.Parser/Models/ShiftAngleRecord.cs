namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x06 — Shift Angle. Records the heading angle for a port or starboard tack,
/// together with the average speed on that tack.
/// </summary>
public record ShiftAngleRecord : VkxRecord
{
    public override RecordType Type => RecordType.ShiftAngle;

    /// <summary>UTC timestamp when the shift was recorded.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// <see langword="true"/> for port tack; <see langword="false"/> for starboard tack.
    /// </summary>
    public bool IsPort { get; init; }

    /// <summary>
    /// <see langword="true"/> if the angle was set manually;
    /// <see langword="false"/> if set by the auto-shift process.
    /// </summary>
    public bool IsManual { get; init; }

    /// <summary>True heading (not magnetic) in degrees.</summary>
    public float TrueHeading { get; init; }

    /// <summary>Average Speed Over Ground on this tack in knots.</summary>
    public float SpeedOverGround { get; init; }
}
