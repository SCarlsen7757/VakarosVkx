using Vakaros.Vkx.Shared.Dtos.Races;

namespace Vakaros.Vkx.Shared.Dtos.Sessions;

public record SessionDetailDto(
    int Id,
    int? BoatId,
    string? BoatName,
    int? CourseId,
    string? CourseName,
    string FileName,
    string ContentHash,
    short FormatVersion,
    short TelemetryRateHz,
    bool IsFixedToBodyFrame,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    DateTimeOffset UploadedAt,
    string? Notes,
    List<RaceDto> Races);
