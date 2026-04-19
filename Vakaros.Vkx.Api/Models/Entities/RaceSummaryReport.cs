namespace Vakaros.Vkx.Api.Models.Entities;

public class RaceSummaryReport
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int RaceNumber { get; set; }
    public string Content { get; set; } = "";
    public string Model { get; set; } = "";
    public string ContextHash { get; set; } = "";
    public DateTimeOffset GeneratedAt { get; set; }

    public Session? Session { get; set; }
}
