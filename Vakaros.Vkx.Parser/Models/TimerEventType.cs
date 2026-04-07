namespace Vakaros.Vkx.Parser.Models;

/// <summary>Race timer event types from the 0x04 record.</summary>
public enum TimerEventType : byte
{
    /// <summary>Timer has been reset to its default value.</summary>
    Reset = 0,
    /// <summary>Timer has been started.</summary>
    Start = 1,
    /// <summary>Timer has been synced to a different value.</summary>
    Sync = 2,
    /// <summary>Timer has reached 0; race has begun.</summary>
    RaceStart = 3,
    /// <summary>User has indicated the race has ended.</summary>
    RaceEnd = 4,
}
