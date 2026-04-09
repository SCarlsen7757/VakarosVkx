using Vakaros.Vkx.Shared.Dtos.Races;

namespace Vakaros.Vkx.Shared.Dtos;

public record RaceDetailDto(
    int RaceNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    LinePositionDto? PinEnd,
    LinePositionDto? BoatEnd);
