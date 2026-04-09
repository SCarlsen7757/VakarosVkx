namespace Vakaros.Vkx.Shared.Dtos.Sessions;

public record SessionSummaryDto(
    int Id,
    int? BoatId,
    string? BoatName,
    int? CourseId,
    string? CourseName,
    string FileName,
    short FormatVersion,
    short TelemetryRateHz,
    bool IsFixedToBodyFrame,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    DateTimeOffset UploadedAt,
    string? Notes,
    int RaceCount);
