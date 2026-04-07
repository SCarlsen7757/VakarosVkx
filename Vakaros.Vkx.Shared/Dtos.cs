namespace Vakaros.Vkx.Shared.Dtos;

// ── Boats ───────────────────────────────────────────────────────────────────

public record CreateBoatRequest(string Name, string? SailNumber, string? BoatClass, string? Description);
public record UpdateBoatRequest(string Name, string? SailNumber, string? BoatClass, string? Description);
public record BoatDto(int Id, string Name, string? SailNumber, string? BoatClass, string? Description, DateTimeOffset CreatedAt);

// ── Marks ───────────────────────────────────────────────────────────────────

public record CreateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
public record UpdateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
public record MarkDto(int Id, string Name, int Year, double Latitude, double Longitude, string? Description);

// ── Courses ─────────────────────────────────────────────────────────────────

public record CourseLegRequest(int MarkId, string? LegName);
public record CreateCourseRequest(string Name, int Year, string? Description, List<CourseLegRequest> Legs);
public record UpdateCourseRequest(string Name, int Year, string? Description, List<CourseLegRequest> Legs);
public record CourseLegDto(int Id, int MarkId, string MarkName, int SortOrder, string? LegName, double Latitude, double Longitude);
public record CourseDto(int Id, string Name, int Year, string? Description, DateTimeOffset CreatedAt, List<CourseLegDto> Legs);
public record CourseSummaryDto(int Id, string Name, int Year, string? Description, DateTimeOffset CreatedAt, int LegCount);
// ── Sessions ────────────────────────────────────────────────────────────────

public record PatchSessionRequest(int? BoatId, int? CourseId, string? Notes);

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

// ── Races ───────────────────────────────────────────────────────────────────

public record RaceDto(int RaceNumber, DateTimeOffset StartedAt, DateTimeOffset EndedAt, double DurationSeconds);

public record RaceDetailDto(
    int RaceNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double DurationSeconds,
    LinePositionDto? PinEnd,
    LinePositionDto? BoatEnd);

public record LinePositionDto(DateTimeOffset Time, double Latitude, double Longitude);

// ── Telemetry ───────────────────────────────────────────────────────────────

public record PositionDto(DateTimeOffset Time, double Latitude, double Longitude, float SpeedOverGround, float CourseOverGround, float Altitude, float QuaternionW, float QuaternionX, float QuaternionY, float QuaternionZ);
public record WindDto(DateTimeOffset Time, float WindDirection, float WindSpeed);
public record SpeedThroughWaterDto(DateTimeOffset Time, float ForwardSpeed, float HorizontalSpeed);
public record DepthDto(DateTimeOffset Time, float Depth);
public record TemperatureDto(DateTimeOffset Time, float Temperature);
public record LoadDto(DateTimeOffset Time, string SensorName, float Load);
public record ShiftAngleDto(DateTimeOffset Time, bool IsPort, bool IsManual, float TrueHeading, float SpeedOverGround);
