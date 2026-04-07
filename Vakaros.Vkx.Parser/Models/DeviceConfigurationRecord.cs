namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x08 — Device configuration record.
/// </summary>
public record DeviceConfigurationRecord : VkxRecord
{
    public override RecordType Type => RecordType.DeviceConfiguration;

    /// <summary>When <see langword="true"/> the device is fixed to the body frame.</summary>
    public bool IsFixedToBodyFrame { get; init; }

    /// <summary>Telemetry logging rate in Hz.</summary>
    public byte TelemetryLoggingRate { get; init; }
}
