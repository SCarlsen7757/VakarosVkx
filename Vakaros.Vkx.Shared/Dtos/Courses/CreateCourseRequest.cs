namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CreateCourseRequest(string Name, int Year, string? Description, List<CourseLegRequest> Legs);
