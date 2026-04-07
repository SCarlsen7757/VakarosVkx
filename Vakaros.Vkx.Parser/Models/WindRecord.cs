namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x0A — Wind. Apparent wind reading from a Calypso Wind Sensor.
/// Only present when the sensor is attached to the device.
/// </summary>
public record WindRecord : VkxRecord
{
    public override RecordType Type => RecordType.Wind;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Apparent wind direction in degrees.</summary>
    public float WindDirection { get; init; }

    /// <summary>Apparent wind speed in metres per second.</summary>
    public float WindSpeed { get; init; }
}
