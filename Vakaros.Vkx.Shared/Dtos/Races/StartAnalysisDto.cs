namespace Vakaros.Vkx.Shared.Dtos.Races;

/// <summary>
/// Computed result of the race start line crossing analysis.
/// Null on RaceDetailDto when no start line data is available.
/// </summary>
public record StartAnalysisDto(
    DateTimeOffset CrossedAt,
    double TimeBiasSeconds,
    float SpeedAtCrossingMs,
    float ApproachCourseDegrees,
    double LineFraction);
