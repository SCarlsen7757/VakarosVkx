namespace Vakaros.Vkx.Shared.Dtos.Sessions;

public record PatchSessionRequest(Guid? BoatId, Guid? CourseId, string? Notes);
