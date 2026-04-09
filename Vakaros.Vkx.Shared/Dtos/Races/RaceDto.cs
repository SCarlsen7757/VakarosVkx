namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceDto(
    int RaceNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    double SailedDistanceMeters,
    float MaxSpeedOverGround);
