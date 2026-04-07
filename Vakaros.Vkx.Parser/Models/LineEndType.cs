namespace Vakaros.Vkx.Parser.Models;

/// <summary>Identifies which end of the start line was set in a 0x05 record.</summary>
public enum LineEndType : byte
{
    /// <summary>Pin (left) end of the start line.</summary>
    Pin = 0,
    /// <summary>Boat (right) end of the start line.</summary>
    Boat = 1,
}
