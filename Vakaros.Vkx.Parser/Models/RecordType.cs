namespace Vakaros.Vkx.Parser.Models;

/// <summary>Identifies the type of a VKX record by its 1-byte key.</summary>
public enum RecordType : byte
{
    InternalType01 = 0x01,
    PositionVelocityOrientation = 0x02,
    Declination = 0x03,
    RaceTimerEvent = 0x04,
    LinePosition = 0x05,
    ShiftAngle = 0x06,
    InternalType07 = 0x07,
    DeviceConfiguration = 0x08,
    Wind = 0x0A,
    SpeedThroughWater = 0x0B,
    Depth = 0x0C,
    InternalType0E = 0x0E,
    Load = 0x0F,
    Temperature = 0x10,
    InternalType20 = 0x20,
    InternalType21 = 0x21,
    PageTerminator = 0xFE,
    PageHeader = 0xFF,
}
