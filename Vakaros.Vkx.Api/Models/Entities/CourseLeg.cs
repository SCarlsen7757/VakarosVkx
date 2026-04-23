namespace Vakaros.Vkx.Api.Models.Entities;

public class CourseLeg
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid CourseId { get; set; }
    public Guid MarkId { get; set; }
    public int SortOrder { get; set; }
    public string? LegName { get; set; }

    public Course Course { get; set; } = null!;
    public Mark Mark { get; set; } = null!;
}
