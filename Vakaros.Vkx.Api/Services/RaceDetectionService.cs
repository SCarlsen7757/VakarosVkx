using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Parser;
using Vakaros.Vkx.Parser.Models;

namespace Vakaros.Vkx.Api.Services;

/// <summary>
/// Extracts numbered races from a parsed VKX session by pairing
/// RaceStart (event 3) and RaceEnd (event 4) timer events.
/// </summary>
public class RaceDetectionService
{
    /// <summary>
    /// Detects races from the race timer events in a parsed VKX session.
    /// </summary>
    /// <param name="session">The parsed VKX session.</param>
    /// <param name="sessionId">The database session ID to associate races with.</param>
    /// <returns>A list of detected <see cref="Race"/> entities numbered chronologically.</returns>
    public List<Race> DetectRaces(VkxSession session, int sessionId)
    {
        var races = new List<Race>();
        var events = session.RaceTimerEventRecords
            .OrderBy(e => e.Timestamp)
            .ToList();

        var state = RaceState.Idle;
        var raceNumber = 0;
        Race? currentRace = null;

        foreach (var evt in events)
        {
            switch (evt.EventType)
            {
                case TimerEventType.RaceStart when state == RaceState.Idle:
                    state = RaceState.Racing;
                    raceNumber++;
                    currentRace = new Race
                    {
                        SessionId = sessionId,
                        RaceNumber = raceNumber,
                        StartedAt = evt.Timestamp
                    };
                    races.Add(currentRace);
                    break;

                case TimerEventType.RaceEnd when state == RaceState.Racing:
                    state = RaceState.Idle;
                    currentRace!.EndedAt = evt.Timestamp;
                    currentRace = null;
                    break;
            }
        }

        // If still racing at EOF, ended_at remains null.
        return races;
    }

    private enum RaceState
    {
        Idle,
        Racing
    }
}
