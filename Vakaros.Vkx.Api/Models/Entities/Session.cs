namespace Vakaros.Vkx.Api.Models.Entities;

public class Session
{
    public int Id { get; set; }
    public int? BoatId { get; set; }
    public int? CourseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public short FormatVersion { get; set; }
    public short TelemetryRateHz { get; set; }
    public bool IsFixedToBodyFrame { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }

    public Boat? Boat { get; set; }
    public Course? Course { get; set; }
    public ICollection<Race> Races { get; set; } = [];
}
