namespace Vakaros.Vkx.Parser.Models;

/// <summary>
/// 0xFF — Page header record. Occurs approximately every 2 kB and carries the VKX format version.
/// </summary>
public record PageHeaderRecord : VkxRecord
{
    public override RecordType Type => RecordType.PageHeader;

    /// <summary>VKX format version number encoded in this page header.</summary>
    public byte FormatVersion { get; init; }
}
