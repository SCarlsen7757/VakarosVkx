namespace Vakaros.Vkx.Web.Components.Shared;

/// <summary>
/// Holds the current playback cursor position, shared between map, charts, and gauge components.
/// </summary>
public class PlaybackState
{
    public DateTimeOffset? CurrentTimestamp { get; set; }
    public event Action? OnChanged;

    public void SetTimestamp(DateTimeOffset timestamp)
    {
        CurrentTimestamp = timestamp;
        OnChanged?.Invoke();
    }
}
