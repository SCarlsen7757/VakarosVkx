namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0x04 — Race Timer Event. Logged when the timer is set, reset, synced, incremented,
/// or when a race begins or ends.
/// </summary>
public record RaceTimerEventRecord : VkxRecord
{
    public override RecordType Type => RecordType.RaceTimerEvent;

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>Type of race timer event.</summary>
    public TimerEventType EventType { get; init; }

    /// <summary>Timer value in seconds at the moment of the event.</summary>
    public int TimerValue { get; init; }
}
