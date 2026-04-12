using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Vakaros.Vkx.Shared.Dtos.BoatClasses;
using Vakaros.Vkx.Shared.Dtos.Boats;
using Vakaros.Vkx.Shared.Dtos.Courses;
using Vakaros.Vkx.Shared.Dtos.Marks;
using Vakaros.Vkx.Shared.Dtos.Races;
using Vakaros.Vkx.Shared.Dtos.Sessions;
using Vakaros.Vkx.Shared.Dtos.Stats;
using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Web.Services;

public class VakarosApiClient(HttpClient http, IMemoryCache cache)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TelemetryTtl = TimeSpan.FromHours(1);

    private static readonly Dictionary<string, CancellationTokenSource> _tagCts = [];
    private static readonly Lock _tagLock = new();

    // ── Stats ────────────────────────────────────────────────────────────────

    public Task<GlobalStatsDto?> GetGlobalStatsAsync(CancellationToken ct = default)
        => GetCachedAsync<GlobalStatsDto>("api/stats/summary", DefaultTtl, "stats", ct);

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
        return GetCachedAsync<List<SessionSummaryDto>>(url, DefaultTtl, "sessions", ct);
    }

    public Task<SessionDetailDto?> GetSessionAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<SessionDetailDto>($"api/sessions/{id}", DefaultTtl, "sessions", ct);

    public async Task<SessionDetailDto?> UploadSessionAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        content.Add(streamContent, "file", fileName);
        var response = await http.PostAsync("api/sessions/upload", content, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("sessions", "stats");
        return await response.Content.ReadFromJsonAsync<SessionDetailDto>(ct);
    }

    public async Task<SessionDetailDto?> PatchSessionAsync(int id, PatchSessionRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/sessions/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("sessions");
        return await response.Content.ReadFromJsonAsync<SessionDetailDto>(ct);
    }

    public async Task DeleteSessionAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/sessions/{id}", ct);
        response.EnsureSuccessStatusCode();
        EvictTag("sessions", "races", "telemetry", "stats");
    }

    // ── Races ───────────────────────────────────────────────────────────────

    public Task<List<RaceDto>?> GetRacesAsync(int sessionId, CancellationToken ct = default)
        => GetCachedAsync<List<RaceDto>>($"api/sessions/{sessionId}/races", DefaultTtl, "races", ct);

    public Task<RaceDetailDto?> GetRaceDetailAsync(int sessionId, int raceNumber, CancellationToken ct = default)
        => GetCachedAsync<RaceDetailDto>($"api/sessions/{sessionId}/races/{raceNumber}", DefaultTtl, "races", ct);

    public async Task<RaceDto?> PatchRaceAsync(int sessionId, int raceNumber, PatchRaceRequest request, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"api/sessions/{sessionId}/races/{raceNumber}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("races");
        return await response.Content.ReadFromJsonAsync<RaceDto>(ct);
    }

    // ── Telemetry ───────────────────────────────────────────────────────────

    public Task<List<PositionDto>?> GetPositionsAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/positions",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<PositionDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<WindDto>?> GetWindAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/wind",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<WindDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<SpeedThroughWaterDto>?> GetSpeedThroughWaterAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/speed-through-water",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<SpeedThroughWaterDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<DepthDto>?> GetDepthAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/depth",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<DepthDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<TemperatureDto>?> GetTemperatureAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/temperature",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<TemperatureDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<LoadDto>?> GetLoadAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/load",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<LoadDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    public Task<List<ShiftAngleDto>?> GetShiftAnglesAsync(int sessionId, int raceNumber, double? from = null, double? to = null, CancellationToken ct = default)
    {
        var url = BuildUrl($"api/sessions/{sessionId}/races/{raceNumber}/shift-angles",
            ("from", from?.ToString(CultureInfo.InvariantCulture)),
            ("to", to?.ToString(CultureInfo.InvariantCulture)));
        return GetCachedAsync<List<ShiftAngleDto>>(url, TelemetryTtl, "telemetry", ct);
    }

    // ── Courses ─────────────────────────────────────────────────────────────

    public Task<List<CourseSummaryDto>?> GetCoursesAsync(int? year = null, CancellationToken ct = default)
    {
        var url = BuildUrl("api/courses", ("year", year?.ToString()));
        return GetCachedAsync<List<CourseSummaryDto>>(url, DefaultTtl, "courses", ct);
    }

    public Task<CourseDto?> GetCourseAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<CourseDto>($"api/courses/{id}", DefaultTtl, "courses", ct);

    public async Task<CourseDto?> CreateCourseAsync(CreateCourseRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/courses", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("courses");
        return await response.Content.ReadFromJsonAsync<CourseDto>(ct);
    }

    public async Task<CourseDto?> UpdateCourseAsync(int id, UpdateCourseRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/courses/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("courses");
        return await response.Content.ReadFromJsonAsync<CourseDto>(ct);
    }

    public async Task DeleteCourseAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/courses/{id}", ct);
        response.EnsureSuccessStatusCode();
        EvictTag("courses");
    }

    // ── Marks ───────────────────────────────────────────────────────────────

    public Task<List<MarkDto>?> GetMarksAsync(DateOnly? activeOn = null, bool? activeOnly = null, CancellationToken ct = default)
    {
        var url = BuildUrl("api/marks",
            ("activeOn", activeOn?.ToString("yyyy-MM-dd")),
            ("activeOnly", activeOnly?.ToString().ToLowerInvariant()));
        return GetCachedAsync<List<MarkDto>>(url, DefaultTtl, "marks", ct);
    }

    public Task<MarkDto?> GetMarkAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<MarkDto>($"api/marks/{id}", DefaultTtl, "marks", ct);

    public async Task<MarkDto?> CreateMarkAsync(CreateMarkRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/marks", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("marks");
        return await response.Content.ReadFromJsonAsync<MarkDto>(ct);
    }

    public async Task<MarkDto?> UpdateMarkAsync(int id, UpdateMarkRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/marks/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("marks");
        return await response.Content.ReadFromJsonAsync<MarkDto>(ct);
    }

    public async Task DeleteMarkAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/marks/{id}", ct);
        response.EnsureSuccessStatusCode();
        EvictTag("marks");
    }

    // ── Boat Classes ─────────────────────────────────────────────────────────

    public Task<List<BoatClassSummaryDto>?> GetBoatClassesAsync(CancellationToken ct = default)
        => GetCachedAsync<List<BoatClassSummaryDto>>("api/boatclasses", DefaultTtl, "boatclasses", ct);

    public Task<BoatClassDto?> GetBoatClassAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<BoatClassDto>($"api/boatclasses/{id}", DefaultTtl, "boatclasses", ct);

    public async Task<BoatClassDto?> CreateBoatClassAsync(CreateBoatClassRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/boatclasses", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boatclasses");
        return await response.Content.ReadFromJsonAsync<BoatClassDto>(ct);
    }

    public async Task<BoatClassDto?> UpdateBoatClassAsync(int id, UpdateBoatClassRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/boatclasses/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boatclasses");
        return await response.Content.ReadFromJsonAsync<BoatClassDto>(ct);
    }

    public async Task DeleteBoatClassAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/boatclasses/{id}", ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boatclasses");
    }

    // ── Boats ───────────────────────────────────────────────────────────────

    public Task<List<BoatDto>?> GetBoatsAsync(CancellationToken ct = default)
        => GetCachedAsync<List<BoatDto>>("api/boats", DefaultTtl, "boats", ct);

    public Task<BoatDto?> GetBoatAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<BoatDto>($"api/boats/{id}", DefaultTtl, "boats", ct);

    public Task<BoatStatsDto?> GetBoatStatsAsync(int id, CancellationToken ct = default)
        => GetCachedAsync<BoatStatsDto>($"api/boats/{id}/stats", DefaultTtl, "boats", ct);

    public async Task<BoatDto?> CreateBoatAsync(CreateBoatRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/boats", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boats", "stats");
        return await response.Content.ReadFromJsonAsync<BoatDto>(ct);
    }

    public async Task<BoatDto?> UpdateBoatAsync(int id, UpdateBoatRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"api/boats/{id}", request, ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boats");
        return await response.Content.ReadFromJsonAsync<BoatDto>(ct);
    }

    public async Task DeleteBoatAsync(int id, CancellationToken ct = default)
    {
        using var response = await http.DeleteAsync($"api/boats/{id}", ct);
        response.EnsureSuccessStatusCode();
        EvictTag("boats", "stats");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<T?> GetCachedAsync<T>(string url, TimeSpan ttl, string tag, CancellationToken ct)
    {
        if (cache.TryGetValue<T>(url, out var cached))
            return cached;

        var result = await http.GetFromJsonAsync<T>(url, ct);
        if (result is not null)
            cache.Set(url, result, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }
                .AddExpirationToken(new CancellationChangeToken(GetOrCreateTagToken(tag))));
        return result;
    }

    private static CancellationToken GetOrCreateTagToken(string tag)
    {
        lock (_tagLock)
        {
            if (!_tagCts.TryGetValue(tag, out var cts) || cts.IsCancellationRequested)
                _tagCts[tag] = cts = new CancellationTokenSource();
            return cts.Token;
        }
    }

    private static void EvictTag(params string[] tags)
    {
        lock (_tagLock)
        {
            foreach (var tag in tags)
            {
                if (_tagCts.Remove(tag, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }
        }
    }

    private static string BuildUrl(string baseUrl, params (string Key, string? Value)[] queryParams)
    {
        var pairs = queryParams.Where(p => p.Value is not null).ToList();
        if (pairs.Count == 0) return baseUrl;
        var qs = string.Join("&", pairs.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));
        return $"{baseUrl}?{qs}";
    }
}
