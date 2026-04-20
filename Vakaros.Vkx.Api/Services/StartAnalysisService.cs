using Microsoft.EntityFrameworkCore;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Helpers;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Shared.Dtos.Races;


namespace Vakaros.Vkx.Api.Services;

public class StartAnalysisService(AppDbContext db)
{
    /// <summary>
    /// Small buffer added after StartedAt to catch boats that cross just after the gun.
    /// </summary>
    private static readonly TimeSpan PostStartBuffer = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Computes the start-line crossing analysis for a race.
    /// Returns null when no valid crossing is found (missing data or boat never crossed).
    /// A valid crossing requires the boat to cross with the committee boat (boat end)
    /// on starboard (right) and the pin end on port (left).
    /// <c>LineFraction</c> is 0 at the committee boat (boat end) and 1 at the pin end.
    /// <para>
    /// All valid crossings in the window are collected and the <b>last</b> one is returned.
    /// This handles the case where a boat crosses early (OCS), returns behind the line,
    /// and crosses again — the final crossing is always the one that counts.
    /// A negative <c>TimeBiasSeconds</c> indicates the boat was still early (OCS).
    /// </para>
    /// </summary>
    public async Task<StartAnalysisDto?> ComputeAsync(
        Race race, int sessionId,
        LinePositionDto? pinEnd, LinePositionDto? boatEnd,
        CancellationToken ct)
    {
        if (pinEnd is null || boatEnd is null)
            return null;

        var windowStart = race.CountdownStartedAt ?? race.StartedAt.AddMinutes(-5);
        var windowEnd = race.StartedAt + PostStartBuffer;

        var positions = await db.Positions
            .Where(p => p.SessionId == sessionId && p.Time >= windowStart && p.Time <= windowEnd)
            .OrderBy(p => p.Time)
            .ToListAsync(ct);

        if (positions.Count < 2)
            return null;

        // Line endpoints (pin = C, boat = D).
        double cx = pinEnd.Latitude, cy = pinEnd.Longitude;
        double dx = boatEnd.Latitude, dy = boatEnd.Longitude;

        // Collect all valid crossings — there may be more than one when the boat
        // crosses early, returns behind the line, and crosses again.
        StartAnalysisDto? lastCrossing = null;

        for (var i = 0; i < positions.Count - 1; i++)
        {
            var a = positions[i];
            var b = positions[i + 1];

            var t = GeoHelper.SegmentIntersection(
                a.Latitude, a.Longitude, b.Latitude, b.Longitude,
                cx, cy, dx, dy);

            if (t is null) continue;

            // Direction check: the boat must cross with the boat end (committee boat)
            // on the right (starboard) and pin end on the left (port).
            // The cross product of the boat's movement vector (A→B) with the line
            // vector (PinEnd→BoatEnd) must be positive for a correct crossing.
            var moveDx = b.Latitude - a.Latitude;
            var moveDy = b.Longitude - a.Longitude;
            var lineDx = dx - cx;
            var lineDy = dy - cy;
            var cross = moveDx * lineDy - moveDy * lineDx;

            if (cross <= 0)
                continue; // wrong side — boat end is on the left, not valid

            var tVal = t.Value;
            var interval = (b.Time - a.Time).TotalSeconds;
            var crossedAt = a.Time.AddSeconds(interval * tVal);
            var timeBias = (crossedAt - race.StartedAt).TotalSeconds;
            var speed = Lerp(a.SpeedOverGround, b.SpeedOverGround, tVal);
            var course = LerpAngle(a.CourseOverGround, b.CourseOverGround, tVal);
            var u = ComputeLineFraction(
                a.Latitude, a.Longitude, b.Latitude, b.Longitude,
                cx, cy, dx, dy);

            lastCrossing = new StartAnalysisDto(crossedAt, timeBias, speed, course, u);
        }

        return lastCrossing;
    }

    /// <summary>
    /// Computes the length of the start line in metres using the Haversine formula.
    /// Returns null when either line end position is missing.
    /// </summary>
    public static StartLineLengthDto? ComputeLineLength(LinePositionDto? pinEnd, LinePositionDto? boatEnd)
    {
        if (pinEnd is null || boatEnd is null)
            return null;

        var lengthMeters = GeoHelper.HaversineMeters(
            pinEnd.Latitude, pinEnd.Longitude,
            boatEnd.Latitude, boatEnd.Longitude);

        return new StartLineLengthDto(lengthMeters);
    }

    private static float Lerp(float a, float b, double t) => (float)(a + (b - a) * t);

    private static float LerpAngle(float a, float b, double t)
    {
        var diff = ((b - a + 540) % 360) - 180;
        var result = a + diff * (float)t;
        return ((result % 360) + 360) % 360;
    }

    private static double ComputeLineFraction(
        double ax, double ay, double bx, double by,
        double cx, double cy, double dx, double dy)
    {
        var dxCD = dx - cx;
        var dyCD = dy - cy;
        var dxAB = bx - ax;
        var dyAB = by - ay;
        var denom = dxAB * dyCD - dyAB * dxCD;
        if (Math.Abs(denom) < 1e-15) return 0.5;
        return 1.0 - ((cx - ax) * dyAB - (cy - ay) * dxAB) / denom;
    }
}
