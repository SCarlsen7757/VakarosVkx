namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CourseSummaryDto(int Id, string Name, int Year, string? Description, DateTimeOffset CreatedAt, int LegCount);
