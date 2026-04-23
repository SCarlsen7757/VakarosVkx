namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CourseLegDto(Guid Id, Guid MarkId, string MarkName, int SortOrder, string? LegName, double Latitude, double Longitude);
