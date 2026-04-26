using Vakaros.Vkx.Shared.Dtos.Races;

namespace Vakaros.Vkx.Shared.Dtos.Sessions;

public record SessionDetailDto(
    Guid Id,
    Guid? BoatId,
    string? BoatName,
    Guid? CourseId,
    string? CourseName,
    string FileName,
    string ContentHash,
    short FormatVersion,
    short TelemetryRateHz,
    bool IsFixedToBodyFrame,
    bool IsPublic,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    DateTimeOffset UploadedAt,
    string? Notes,
    bool IsOwned,
    List<RaceDto> Races);
