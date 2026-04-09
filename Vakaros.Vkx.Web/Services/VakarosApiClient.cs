using System.Globalization;
using Vakaros.Vkx.Shared.Dtos.Boats;
using Vakaros.Vkx.Shared.Dtos.Courses;
using Vakaros.Vkx.Shared.Dtos.Marks;
using Vakaros.Vkx.Shared.Dtos.Races;
using Vakaros.Vkx.Shared.Dtos.Sessions;
using Vakaros.Vkx.Shared.Dtos.Stats;
using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Web.Services;

public class VakarosApiClient(HttpClient http)
{
    // ── Stats ────────────────────────────────────────────────────────────────

    public Task<GlobalStatsDto?> GetGlobalStatsAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<GlobalStatsDto>("api/stats/summary", ct);

    // ── Sessions ────────────────────────────────────────────────────────────

    public Task<List<SessionSummaryDto>?> GetSessionsAsync(
        int? boatId = null, int? courseId = null, int? year = null,
        DateTimeOffset? from = null, DateTimeOffset? to = null,
        CancellationToken ct = default)
    {
        var url = BuildUrl("api/sessions",
            ("boatId", boatId?.ToString()),
            ("courseId", courseId?.ToString()),
            ("year", year?.ToString()),
            ("from", from?.ToString("O")),
            ("to", to?.ToString("O")));
        return http.GetFromJsonAsync<List<SessionSummaryDto>>(url, ct);
    }

    public Task<SessionDetailDto?> GetSessionAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<SessionDetailDto>($"api/sessions/{id}", ct);

    public async Task<SessionDetailDto?> UploadSessionAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        content.Add(streamContent, "file", fileName);
        var response = await http.PostAsync("api/sessions/upload", content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionDetailDto>(ct);
    }

    public async Task<SessionDetailDto?> PatchSessionAsync(int id, PatchSessionRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/sessions/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionDetailDto>(ct);
    }

    public async Task DeleteSessionAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/sessions/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Races ───────────────────────────────────────────────────────────────

    public Task<List<RaceDto>?> GetRacesAsync(int sessionId, CancellationToken ct = default)
        => http.GetFromJsonAsync<List<RaceDto>>($"api/sessions/{sessionId}/races", ct);

    public Task<RaceDetailDto?> GetRaceDetailAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => http.GetFromJsonAsync<RaceDetailDto>($"api/sessions/{sessionId}/races/{raceNumber}", ct);

    // ── Telemetry ───────────────────────────────────────────────────────────

    public Task<List<PositionDto>?> GetPositionsAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/positions",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<PositionDto>>(url, ct);
    }

    public Task<List<WindDto>?> GetWindAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/wind",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<WindDto>>(url, ct);
    }

    public Task<List<SpeedThroughWaterDto>?> GetSpeedThroughWaterAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/speed-through-water",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<SpeedThroughWaterDto>>(url, ct);
    }

    public Task<List<DepthDto>?> GetDepthAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/depth",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<DepthDto>>(url, ct);
    }

    public Task<List<TemperatureDto>?> GetTemperatureAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/temperature",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<TemperatureDto>>(url, ct);
    }

    public Task<List<LoadDto>?> GetLoadAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/load",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<LoadDto>>(url, ct);
    }

    public Task<List<ShiftAngleDto>?> GetShiftAnglesAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/shift-angles",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return http.GetFromJsonAsync<List<ShiftAngleDto>>(url, ct);
    }

    // ── Courses ─────────────────────────────────────────────────────────────

    public Task<List<CourseSummaryDto>?> GetCoursesAsync(int? year = null, CancellationToken ct = default)
    {
        var url = BuildUrl("api/courses", ("year", year?.ToString()));
        return http.GetFromJsonAsync<List<CourseSummaryDto>>(url, ct);
    }

    public Task<CourseDto?> GetCourseAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<CourseDto>($"api/courses/{id}", ct);

    public async Task<CourseDto?> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/courses", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseDto>(ct);
    }

    public async Task<CourseDto?> UpdateCourseAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/courses/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CourseDto>(ct);
    }

    public async Task DeleteCourseAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/courses/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Marks ───────────────────────────────────────────────────────────────

    public Task<List<MarkDto>?> GetMarksAsync(DateOnly? activeOn = null, bool? activeOnly = null, CancellationToken ct = default)
    {
        var url = BuildUrl("api/marks",
            ("activeOn", activeOn?.ToString("yyyy-MM-dd")),
            ("activeOnly", activeOnly?.ToString().ToLowerInvariant()));
        return http.GetFromJsonAsync<List<MarkDto>>(url, ct);
    }

    public Task<MarkDto?> GetMarkAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<MarkDto>($"api/marks/{id}", ct);

    public async Task<MarkDto?> CreateMarkAsync(CreateMarkRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/marks", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MarkDto>(ct);
    }

    public async Task<MarkDto?> UpdateMarkAsync(int id, UpdateMarkRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/marks/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MarkDto>(ct);
    }

    public async Task DeleteMarkAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/marks/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Boats ───────────────────────────────────────────────────────────────

    public Task<List<BoatDto>?> GetBoatsAsync(CancellationToken ct = default)
        => http.GetFromJsonAsync<List<BoatDto>>("api/boats", ct);

    public Task<BoatDto?> GetBoatAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<BoatDto>($"api/boats/{id}", ct);

    public Task<BoatStatsDto?> GetBoatStatsAsync(int id, CancellationToken ct = default)
        => http.GetFromJsonAsync<BoatStatsDto>($"api/boats/{id}/stats", ct);

    public async Task<BoatDto?> CreateBoatAsync(CreateBoatRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/boats", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BoatDto>(ct);
    }

    public async Task<BoatDto?> UpdateBoatAsync(int id, UpdateBoatRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/boats/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BoatDto>(ct);
    }

    public async Task DeleteBoatAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/boats/{id}", ct);
        response.EnsureSuccessStatusCode();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string BuildUrl(string baseUrl, params (string Key, string? Value)[] queryParams)
    {
        var pairs = queryParams.Where(p => p.Value is not null).ToList();
        if (pairs.Count == 0) return baseUrl;
        var qs = string.Join("&", pairs.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
        return $"{baseUrl}?{qs}";
    }
}
