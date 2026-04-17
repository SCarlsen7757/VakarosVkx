namespace Vakaros.Vkx.Api.Models.Entities;

public class Race
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int? CourseId { get; set; }
    public int RaceNumber { get; set; }
    public DateTimeOffset? CountdownStartedAt { get; set; }
    public int? CountdownDurationSeconds { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public double SailedDistanceMeters { get; set; }
    public float MaxSpeedOverGround { get; set; }
    public string? Notes { get; set; }

    public Session? Session { get; set; } = null;
    public Course? Course { get; set; } = null;
}
