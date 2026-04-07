namespace Vakaros.Vkx.Parser.Models;

/// <summary>Base type for all parsed VKX records.</summary>
public abstract record VkxRecord
{
    /// <summary>The record type key identifying this record's format.</summary>
    public abstract RecordType Type { get; }
}
