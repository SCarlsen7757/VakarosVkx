namespace Vakaros.Vkx.Shared.Dtos;

public record RaceDto(int RaceNumber, DateTimeOffset StartedAt, DateTimeOffset EndedAt, double DurationSeconds);
