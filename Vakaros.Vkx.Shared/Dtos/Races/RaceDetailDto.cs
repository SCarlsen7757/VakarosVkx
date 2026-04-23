namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceDetailDto(
    int RaceNumber,
    Guid? CourseId,
    DateTimeOffset? CountdownStartedAt,
    int? CountdownDurationSeconds,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    double? DurationSeconds,
    double SailedDistanceMeters,
    float MaxSpeedOverGround,
    string? Notes,
    LinePositionDto? PinEnd,
    LinePositionDto? BoatEnd,
    StartAnalysisDto? StartAnalysis);
