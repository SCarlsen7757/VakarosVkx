using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Vakaros.Vkx.Api.Data;
using Vakaros.Vkx.Api.Models.Entities;
using Vakaros.Vkx.Parser;

namespace Vakaros.Vkx.Api.Services;

/// <summary>
/// Orchestrates parsing a VKX file, persisting all records into the database,
/// and extracting races via <see cref="RaceDetectionService"/>.
/// </summary>
public class VkxIngestionService(AppDbContext db, RaceDetectionService raceDetection)
{
    /// <summary>
    /// Computes the SHA-256 hash of raw file bytes and returns it as a lowercase hex string.
    /// </summary>
    public static string ComputeHash(byte[] fileBytes)
    {
        var hashBytes = SHA256.HashData(fileBytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Checks whether a session with the given content hash already exists.
    /// </summary>
    public async Task<bool> IsDuplicateAsync(string contentHash, CancellationToken ct = default)
    {
        return await db.Sessions.AnyAsync(s => s.ContentHash == contentHash, ct);
    }

    /// <summary>
    /// Parses the VKX data and persists all records, returning the created session entity.
    /// </summary>
    public async Task<Session> IngestAsync(byte[] fileBytes, string fileName, string contentHash, CancellationToken ct = default)
    {
        var vkxSession = VkxParser.Parse(fileBytes);

        // Extract session-level metadata.
        var deviceConfig = vkxSession.DeviceConfigurationRecords.FirstOrDefault();
        var firstPosition = vkxSession.PositionRecords.FirstOrDefault();
        var lastPosition = vkxSession.PositionRecords.LastOrDefault();

        //TODO : Consider more robust approaches for determining session start/end times and telemetry rate. Or make null checks and throw if assumptions are violated.

        var session = new Session
        {
            FileName = fileName,
            ContentHash = contentHash,
            FormatVersion = vkxSession.FormatVersion,
            TelemetryRateHz = deviceConfig!.TelemetryLoggingRate, //HACK : Assume telemetry rate is constant and use value from device config record.
            IsFixedToBodyFrame = deviceConfig!.IsFixedToBodyFrame, //HACK : Assume fixed-to-body-frame is constant and use value from device config record.
            StartedAt = firstPosition!.Timestamp, //HACK : Assume session start time is timestamp of first position record.
            EndedAt = lastPosition!.Timestamp, //HACK : Assume session end time is timestamp of last position record.
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        // Detect and insert races.
        var races = raceDetection.DetectRaces(vkxSession, session.Id);
        if (races.Count > 0)
        {
            db.Races.AddRange(races);
            await db.SaveChangesAsync(ct);
            session.Races = races;
        }

        // Insert time-series data.
        await InsertTimeSeriesDataAsync(vkxSession, session.Id, ct);

        return session;
    }

    private async Task InsertTimeSeriesDataAsync(VkxSession vkxSession, int sessionId, CancellationToken ct)
    {
        // Positions (highest frequency — bulk insert)
        var positions = vkxSession.PositionRecords.Select(p => new PositionReading
        {
            Time = p.Timestamp,
            SessionId = sessionId,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            SpeedOverGround = p.SpeedOverGround,
            CourseOverGround = p.CourseOverGround,
            Altitude = p.Altitude,
            QuaternionW = p.QuaternionW,
            QuaternionX = p.QuaternionX,
            QuaternionY = p.QuaternionY,
            QuaternionZ = p.QuaternionZ,
        });
        db.Positions.AddRange(positions);

        // Wind readings
        var windReadings = vkxSession.WindRecords.Select(w => new WindReading
        {
            Time = w.Timestamp,
            SessionId = sessionId,
            WindDirection = w.WindDirection,
            WindSpeed = w.WindSpeed,
        });
        db.WindReadings.AddRange(windReadings);

        // Speed through water
        var speedReadings = vkxSession.SpeedThroughWaterRecords.Select(s => new SpeedThroughWaterReading
        {
            Time = s.Timestamp,
            SessionId = sessionId,
            ForwardSpeed = s.ForwardSpeed,
            HorizontalSpeed = s.HorizontalSpeed,
        });
        db.SpeedThroughWater.AddRange(speedReadings);

        // Depth
        var depthReadings = vkxSession.DepthRecords.Select(d => new DepthReading
        {
            Time = d.Timestamp,
            SessionId = sessionId,
            Depth = d.Depth,
        });
        db.DepthReadings.AddRange(depthReadings);

        // Temperature
        var tempReadings = vkxSession.TemperatureRecords.Select(t => new TemperatureReading
        {
            Time = t.Timestamp,
            SessionId = sessionId,
            Temperature = t.Temperature,
        });
        db.TemperatureReadings.AddRange(tempReadings);

        // Load
        var loadReadings = vkxSession.LoadRecords.Select(l => new LoadReading
        {
            Time = l.Timestamp,
            SessionId = sessionId,
            SensorName = l.SensorName,
            Load = l.Load,
        });
        db.LoadReadings.AddRange(loadReadings);

        // Declinations
        var declinations = vkxSession.DeclinationRecords.Select(d => new DeclinationReading
        {
            Time = d.Timestamp,
            SessionId = sessionId,
            DeclinationOffset = d.DeclinationOffset,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
        });
        db.Declinations.AddRange(declinations);

        // Race timer events
        var timerEvents = vkxSession.RaceTimerEventRecords.Select(e => new RaceTimerEvent
        {
            Time = e.Timestamp,
            SessionId = sessionId,
            EventType = (short)e.EventType,
            TimerValue = e.TimerValue,
        });
        db.RaceTimerEvents.AddRange(timerEvents);

        // Line positions
        var linePositions = vkxSession.LinePositionRecords.Select(l => new LinePositionReading
        {
            Time = l.Timestamp,
            SessionId = sessionId,
            LineEnd = (short)l.LineEnd,
            Latitude = l.Latitude,
            Longitude = l.Longitude,
        });
        db.LinePositions.AddRange(linePositions);

        // Shift angles
        var shiftAngles = vkxSession.ShiftAngleRecords.Select(s => new ShiftAngleReading
        {
            Time = s.Timestamp,
            SessionId = sessionId,
            IsPort = s.IsPort,
            IsManual = s.IsManual,
            TrueHeading = s.TrueHeading,
            SpeedOverGround = s.SpeedOverGround,
        });
        db.ShiftAngles.AddRange(shiftAngles);

        await db.SaveChangesAsync(ct);
    }
}
