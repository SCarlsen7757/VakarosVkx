namespace Vakaros.Vkx.Api.Models.Entities;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CourseLeg> Legs { get; set; } = [];
    public ICollection<Session> Sessions { get; set; } = [];
}
