namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x02 — Position, Velocity, and Orientation. Primary telemetry message logged at the
/// device's configured rate.
/// </summary>
public record PositionRecord : VkxRecord
{
    public override RecordType Type => RecordType.PositionVelocityOrientation;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Latitude in decimal degrees (WGS-84).</summary>
    public double Latitude { get; init; }

    /// <summary>Longitude in decimal degrees (WGS-84).</summary>
    public double Longitude { get; init; }

    /// <summary>Speed Over Ground in metres per second.</summary>
    public float SpeedOverGround { get; init; }

    /// <summary>Course Over Ground in radians.</summary>
    public float CourseOverGround { get; init; }

    /// <summary>Altitude in metres.</summary>
    public float Altitude { get; init; }

    /// <summary>Orientation quaternion W component (true NED frame).</summary>
    public float QuaternionW { get; init; }

    /// <summary>Orientation quaternion X component.</summary>
    public float QuaternionX { get; init; }

    /// <summary>Orientation quaternion Y component.</summary>
    public float QuaternionY { get; init; }

    /// <summary>Orientation quaternion Z component.</summary>
    public float QuaternionZ { get; init; }
}
