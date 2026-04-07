namespace Vakaros.Vkx.Api.Models.Entities;

public class Mark
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Description { get; set; }

    public ICollection<CourseLeg> CourseLegs { get; set; } = [];
}
