namespace Vakaros.Vkx.Api.Models.Entities;

public class Course
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseLeg> Legs { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
    public ICollection<Race> Races { get; set; } = [];
}
