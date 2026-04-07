namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x0C — Depth. Reading from a transducer.
/// Only present when the sensor is attached to the device.
/// </summary>
public record DepthRecord : VkxRecord
{
    public override RecordType Type => RecordType.Depth;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Water depth in metres.</summary>
    public float Depth { get; init; }
}
