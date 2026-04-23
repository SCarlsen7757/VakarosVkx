namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CourseDto(Guid Id, string Name, int Year, string? Description, DateTimeOffset CreatedAt, double TotalLengthMeters, List<CourseLegDto> Legs);
