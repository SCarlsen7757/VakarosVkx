namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CourseLegDto(int Id, int MarkId, string MarkName, int SortOrder, string? LegName, double Latitude, double Longitude);
