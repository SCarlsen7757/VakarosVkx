namespace Vakaros.Vkx.Shared.Dtos.Races;

/// <summary>
/// Computed result of the race start line crossing analysis.
/// Null on RaceDetailDto when no start line data is available.
/// </summary>
/// <param name="CrossedAt">Time of the start line crossing.</param>
/// <param name="TimeBiasSeconds">Seconds relative to the start gun (negative = early).</param>
/// <param name="SpeedAtCrossingMs">Speed over ground at the crossing in m/s.</param>
/// <param name="ApproachCourseDegrees">Course over ground at the crossing in degrees.</param>
/// <param name="LineFraction">Position along the line: 0 = committee boat end, 1 = pin end.</param>
/// <param name="IsOcs">
/// True if the boat crossed the start line in the valid direction before the start gun
/// (regardless of whether it subsequently cleared the OCS).
/// </param>
/// <param name="IsOcsCleared">
/// Only meaningful when <see cref="IsOcs"/> is true.
/// True if the boat returned to the pre-start side of the line before the start gun
/// (by any path — through the line or around either end), clearing the OCS.
/// </param>
public record StartAnalysisDto(
    DateTimeOffset CrossedAt,
    double TimeBiasSeconds,
    float SpeedAtCrossingMs,
    float ApproachCourseDegrees,
    double LineFraction,
    bool IsOcs,
    bool IsOcsCleared);
