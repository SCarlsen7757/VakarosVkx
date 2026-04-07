namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x0B — Speed Through Water. Reading from a transducer.
/// Only present when the sensor is attached to the device.
/// </summary>
public record SpeedThroughWaterRecord : VkxRecord
{
    public override RecordType Type => RecordType.SpeedThroughWater;

    /// <summary>UTC timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Speed of water in the forward direction in metres per second.</summary>
    public float ForwardSpeed { get; init; }

    /// <summary>Speed of water in the horizontal (lateral) direction in metres per second.</summary>
    public float HorizontalSpeed { get; init; }
}
