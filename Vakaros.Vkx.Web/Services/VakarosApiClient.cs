using Vakaros.Vkx.Shared.Dtos;
using Vakaros.Vkx.Shared.Dtos.Boat;
using Vakaros.Vkx.Shared.Dtos.Courses;
using Vakaros.Vkx.Shared.Dtos.Sessions;
using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Web.Services;

public class VakarosApiClient(HttpClient http)
{
    // ── Sessions ────────────────────────────────────────────────────────────

    public Task<List<SessionSummaryDto>?> GetSessionsAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<SessionSummaryDto>>("api/sessions", ct);

    public Task<SessionDetailDto?> GetSessionAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<SessionDetailDto>($"api/sessions/{id}", ct);

    // ── Races ───────────────────────────────────────────────────────────────

    public Task<List<RaceDto>?> GetRacesAsync(int sessionId, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<RaceDto>>($"api/sessions/{sessionId}/races", ct);

    public Task<RaceDetailDto?> GetRaceDetailAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<RaceDetailDto>($"api/sessions/{sessionId}/races/{raceNumber}", ct);

    // ── Telemetry ───────────────────────────────────────────────────────────

    public Task<List<PositionDto>?> GetPositionsAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<PositionDto>>($"api/sessions/{sessionId}/races/{raceNumber}/positions", ct);

    public Task<List<WindDto>?> GetWindAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<WindDto>>($"api/sessions/{sessionId}/races/{raceNumber}/wind", ct);

    public Task<List<SpeedThroughWaterDto>?> GetSpeedThroughWaterAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<SpeedThroughWaterDto>>($"api/sessions/{sessionId}/races/{raceNumber}/speed-through-water", ct);

    public Task<List<DepthDto>?> GetDepthAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<DepthDto>>($"api/sessions/{sessionId}/races/{raceNumber}/depth", ct);

    public Task<List<TemperatureDto>?> GetTemperatureAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<TemperatureDto>>($"api/sessions/{sessionId}/races/{raceNumber}/temperature", ct);

    public Task<List<LoadDto>?> GetLoadAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<LoadDto>>($"api/sessions/{sessionId}/races/{raceNumber}/load", ct);

    public Task<List<ShiftAngleDto>?> GetShiftAnglesAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<ShiftAngleDto>>($"api/sessions/{sessionId}/races/{raceNumber}/shift-angles", ct);

    // ── Courses ─────────────────────────────────────────────────────────────

    public Task<CourseDto?> GetCourseAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<CourseDto>($"api/courses/{id}", ct);

    public Task<List<CourseSummaryDto>?> GetCoursesAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<CourseSummaryDto>>("api/courses", ct);

    // ── Marks ───────────────────────────────────────────────────────────────

    public Task<List<MarkDto>?> GetMarksAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<MarkDto>>("api/marks", ct);

    // ── Boats ───────────────────────────────────────────────────────────────

    public Task<List<BoatDto>?> GetBoatsAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<BoatDto>>("api/boats", ct);
}
