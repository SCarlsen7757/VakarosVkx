namespace Vakaros.Vkx.Api.Models.Entities;

public class CourseLeg
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public int MarkId { get; set; }
    public int SortOrder { get; set; }
    public string? LegName { get; set; }

    public Course Course { get; set; } = null!;
    public Mark Mark { get; set; } = null!;
}
