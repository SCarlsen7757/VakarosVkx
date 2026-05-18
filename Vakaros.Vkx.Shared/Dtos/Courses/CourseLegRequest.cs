namespace Vakaros.Vkx.Shared.Dtos.Courses;

public record CourseLegRequest(
    Guid MarkId,
    Guid? GateMarkId,
    string? LegName,
    string LegType,
    string PassingSide);
