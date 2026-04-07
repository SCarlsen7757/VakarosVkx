namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x10 — Temperature. Reading from a transducer.
/// Only present when the sensor is attached to the device.
/// </summary>
public record TemperatureRecord : VkxRecord
{
    public override RecordType Type => RecordType.Temperature;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Temperature in degrees Celsius.</summary>
    public float Temperature { get; init; }
}
