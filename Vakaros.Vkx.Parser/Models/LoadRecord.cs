namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x0F — Load. Reading from a Cyclops load cell.
/// Only present when the sensor is attached to the device.
/// </summary>
public record LoadRecord : VkxRecord
{
    public override RecordType Type => RecordType.Load;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Short name identifying the load cell sensor (up to 4 ASCII characters).</summary>
    public string SensorName { get; init; } = string.Empty;

    /// <summary>Load amount (unit depends on sensor configuration).</summary>
    public float Load { get; init; }
}
