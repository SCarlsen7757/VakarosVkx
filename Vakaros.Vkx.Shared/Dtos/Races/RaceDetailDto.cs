namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceDetailDto(
    int RaceNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    double SailedDistanceMeters,
    float MaxSpeedOverGround,
    LinePositionDto? PinEnd,
    LinePositionDto? BoatEnd);
