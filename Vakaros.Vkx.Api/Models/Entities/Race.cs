namespace Vakaros.Vkx.Api.Models.Entities;

public class Race
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public int RaceNumber { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }

    public Session Session { get; set; } = null!;
}
