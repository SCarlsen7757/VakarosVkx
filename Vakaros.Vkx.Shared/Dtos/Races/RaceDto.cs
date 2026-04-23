namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceDto(
    int RaceNumber,
    Guid? CourseId,
    string? CourseName,
    DateTimeOffset? CountdownStartedAt,
    int? CountdownDurationSeconds,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    double? DurationSeconds,
    double SailedDistanceMeters,
    float MaxSpeedOverGround,
    string? Notes);
