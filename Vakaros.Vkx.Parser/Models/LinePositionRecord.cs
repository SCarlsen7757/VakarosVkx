namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x05 — Line Position. Logged when the user sets the pin or boat end of the start line.
/// </summary>
public record LinePositionRecord : VkxRecord
{
    public override RecordType Type => RecordType.LinePosition;

    /// <summary>UTC timestamp when the line end was set.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Which end of the start line this position represents.</summary>
    public LineEndType LineEnd { get; init; }

    /// <summary>Latitude of the line end in decimal degrees.</summary>
    public float Latitude { get; init; }

    /// <summary>Longitude of the line end in decimal degrees.</summary>
    public float Longitude { get; init; }
}
