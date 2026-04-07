using System.Text;
using Vakaros.Vkx.Parser.Models;

namespace Vakaros.Vkx.Parser;

/// <summary>
/// Parses VKX binary log files produced by Vakaros devices into a <see cref="VkxSession"/>.
/// </summary>
/// <example>
/// <code>
/// // From a file path
/// VkxLog log = VkxParser.ParseFile("session.vkx");
///
/// // From a stream
/// VkxLog log = VkxParser.Parse(stream);
///
/// // From a byte array
/// VkxLog log = VkxParser.Parse(bytes);
///
/// // Access typed records
/// foreach (PositionRecord pos in log.PositionRecords)
///     Console.WriteLine($"{pos.Timestamp} lat={pos.Latitude} lon={pos.Longitude}");
/// </code>
/// </example>
public static class VkxParser
{
    // Maps every known 1-byte key to the size of its fixed payload in bytes.
    // Internal messages are included so the parser can skip them correctly.
    private static readonly Dictionary<byte, int> PayloadSizes = new()
    {
        { 0x01, 32 },   // Internal
        { 0x02, 44 },   // Position, Velocity, Orientation
        { 0x03, 20 },   // Declination
        { 0x04, 13 },   // Race Timer Event
        { 0x05, 17 },   // Line Position
        { 0x06, 18 },   // Shift Angle
        { 0x07, 12 },   // Internal
        { 0x08, 13 },   // Device Configuration
        { 0x0A, 16 },   // Wind
        { 0x0B, 16 },   // Speed Through Water
        { 0x0C, 12 },   // Depth
        { 0x0E, 16 },   // Internal
        { 0x0F, 16 },   // Load
        { 0x10, 12 },   // Temperature
        { 0x20, 13 },   // Internal
        { 0x21, 52 },   // Internal
        { 0xFE,  2 },   // Page Terminator
        { 0xFF,  7 },   // Page Header
    };

    /// <summary>Parses a VKX file at the given path.</summary>
    /// <param name="filePath">Absolute or relative path to the .vkx file.</param>
    /// <exception cref="FormatException">
    /// Thrown when an unrecognised record key is encountered.
    /// </exception>
    public static VkxSession ParseFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Parse(stream);
    }

    /// <summary>Parses a VKX file from a byte array.</summary>
    /// <exception cref="FormatException">
    /// Thrown when an unrecognised record key is encountered.
    /// </exception>
    public static VkxSession Parse(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Parse(stream);
    }

    /// <summary>Parses a VKX file from a <see cref="Stream"/>. The stream is left open.</summary>
    /// <exception cref="FormatException">
    /// Thrown when an unrecognised record key is encountered.
    /// </exception>
    public static VkxSession Parse(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

        var records = new List<VkxRecord>();
        byte formatVersion = 0;

        while (stream.Position < stream.Length)
        {
            byte key;
            try
            {
                key = reader.ReadByte();
            }
            catch (EndOfStreamException)
            {
                break;
            }

            if (key == 0xFF)
            {
                // Handle page header separately so we can capture the format version.
                var pageHeader = ParsePageHeader(reader);
                formatVersion = pageHeader.FormatVersion;
                records.Add(pageHeader);
                continue;
            }

            var record = ParseRecord(reader, key);
            if (record is not null)
                records.Add(record);
        }

        return new VkxSession(formatVersion, records);
    }

    // Returns null for internal record types that carry no public data.
    private static VkxRecord? ParseRecord(BinaryReader reader, byte key) => key switch
    {
        0xFE => ParsePageTerminator(reader),
        0x02 => ParsePosition(reader),
        0x03 => ParseDeclination(reader),
        0x04 => ParseRaceTimerEvent(reader),
        0x05 => ParseLinePosition(reader),
        0x06 => ParseShiftAngle(reader),
        0x08 => ParseDeviceConfiguration(reader),
        0x0A => ParseWind(reader),
        0x0B => ParseSpeedThroughWater(reader),
        0x0C => ParseDepth(reader),
        0x0F => ParseLoad(reader),
        0x10 => ParseTemperature(reader),
        // Internal messages — read and discard payload.
        0x01 => Skip(reader, 32),
        0x07 => Skip(reader, 12),
        0x0E => Skip(reader, 16),
        0x20 => Skip(reader, 13),
        0x21 => Skip(reader, 52),
        _ => throw new FormatException($"Unknown VKX record key: 0x{key:X2} at stream position {reader.BaseStream.Position - 1}.")
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static VkxRecord? Skip(BinaryReader reader, int byteCount)
    {
        reader.ReadBytes(byteCount);
        return null;
    }

    /// <summary>Converts a Unix timestamp in milliseconds to a <see cref="DateTimeOffset"/>.</summary>
    private static DateTimeOffset ToTimestamp(ulong milliseconds)
        => DateTimeOffset.FromUnixTimeMilliseconds((long)milliseconds);

    /// <summary>Converts an I4 lat/lon value (10^-7 degrees) to decimal degrees.</summary>
    private static double ToLatLon(int rawValue)
        => rawValue * 1e-7;

    // ── Record parsers ────────────────────────────────────────────────────────

    private static PageHeaderRecord ParsePageHeader(BinaryReader reader)
    {
        byte version = reader.ReadByte();
        reader.ReadBytes(6); // Internal log state — not exposed.
        return new PageHeaderRecord { FormatVersion = version };
    }

    private static PageTerminatorRecord ParsePageTerminator(BinaryReader reader)
        => new() { PreviousPageLength = reader.ReadUInt16() };

    private static PositionRecord ParsePosition(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var lat = reader.ReadInt32();
        var lon = reader.ReadInt32();
        var sog = reader.ReadSingle();
        var cog = reader.ReadSingle();
        var altitude = reader.ReadSingle();
        var qw = reader.ReadSingle();
        var qx = reader.ReadSingle();
        var qy = reader.ReadSingle();
        var qz = reader.ReadSingle();

        return new PositionRecord
        {
            Timestamp = ToTimestamp(timestamp),
            Latitude = ToLatLon(lat),
            Longitude = ToLatLon(lon),
            SpeedOverGround = sog,
            CourseOverGround = cog,
            Altitude = altitude,
            QuaternionW = qw,
            QuaternionX = qx,
            QuaternionY = qy,
            QuaternionZ = qz,
        };
    }

    private static DeclinationRecord ParseDeclination(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var declination = reader.ReadSingle();
        var lat = reader.ReadInt32();
        var lon = reader.ReadInt32();

        return new DeclinationRecord
        {
            Timestamp = ToTimestamp(timestamp),
            DeclinationOffset = declination,
            Latitude = ToLatLon(lat),
            Longitude = ToLatLon(lon),
        };
    }

    private static RaceTimerEventRecord ParseRaceTimerEvent(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var eventType = reader.ReadByte();
        var timerValue = reader.ReadInt32();

        return new RaceTimerEventRecord
        {
            Timestamp = ToTimestamp(timestamp),
            EventType = (TimerEventType)eventType,
            TimerValue = timerValue,
        };
    }

    private static LinePositionRecord ParseLinePosition(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var lineEnd = reader.ReadByte();
        var lat = reader.ReadSingle();
        var lon = reader.ReadSingle();

        return new LinePositionRecord
        {
            Timestamp = ToTimestamp(timestamp),
            LineEnd = (LineEndType)lineEnd,
            Latitude = lat,
            Longitude = lon,
        };
    }

    private static ShiftAngleRecord ParseShiftAngle(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var tackId = reader.ReadByte();
        var setBy = reader.ReadByte();
        var trueHeading = reader.ReadSingle();
        var sog = reader.ReadSingle();

        return new ShiftAngleRecord
        {
            Timestamp = ToTimestamp(timestamp),
            IsPort = tackId == 1,
            IsManual = setBy == 1,
            TrueHeading = trueHeading,
            SpeedOverGround = sog,
        };
    }

    private static DeviceConfigurationRecord ParseDeviceConfiguration(BinaryReader reader)
    {
        reader.ReadUInt64();              // Unused field per spec.
        var bitfield = reader.ReadUInt32();
        var loggingRate = reader.ReadByte();

        return new DeviceConfigurationRecord
        {
            IsFixedToBodyFrame = (bitfield & 0x01) != 0,
            TelemetryLoggingRate = loggingRate,
        };
    }

    private static WindRecord ParseWind(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var direction = reader.ReadSingle();
        var speed = reader.ReadSingle();

        return new WindRecord
        {
            Timestamp = ToTimestamp(timestamp),
            WindDirection = direction,
            WindSpeed = speed,
        };
    }

    private static SpeedThroughWaterRecord ParseSpeedThroughWater(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var forwardSpeed = reader.ReadSingle();
        var horizontalSpeed = reader.ReadSingle();

        return new SpeedThroughWaterRecord
        {
            Timestamp = ToTimestamp(timestamp),
            ForwardSpeed = forwardSpeed,
            HorizontalSpeed = horizontalSpeed,
        };
    }

    private static DepthRecord ParseDepth(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var depth = reader.ReadSingle();

        return new DepthRecord
        {
            Timestamp = ToTimestamp(timestamp),
            Depth = depth,
        };
    }

    private static TemperatureRecord ParseTemperature(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var temperature = reader.ReadSingle();

        return new TemperatureRecord
        {
            Timestamp = ToTimestamp(timestamp),
            Temperature = temperature,
        };
    }

    private static LoadRecord ParseLoad(BinaryReader reader)
    {
        var timestamp = reader.ReadUInt64();
        var nameBytes = reader.ReadBytes(4);
        var sensorName = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
        var load = reader.ReadSingle();

        return new LoadRecord
        {
            Timestamp = ToTimestamp(timestamp),
            SensorName = sensorName,
            Load = load,
        };
    }
}
