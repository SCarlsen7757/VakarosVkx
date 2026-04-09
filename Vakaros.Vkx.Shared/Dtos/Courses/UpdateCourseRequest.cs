namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record UpdateCourseRequest(string Name, int Year, string? Description, List<CourseLegRequest> Legs);
