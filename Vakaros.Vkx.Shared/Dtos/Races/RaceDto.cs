namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceDto(
    int RaceNumber,
    int? CourseId,
    string? CourseName,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    double SailedDistanceMeters,
    float MaxSpeedOverGround);
