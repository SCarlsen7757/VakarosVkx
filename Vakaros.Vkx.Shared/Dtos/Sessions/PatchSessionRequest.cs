namespace Vakaros.Vkx.Shared.Dtos.Sessions;

public record PatchSessionRequest(int? BoatId, int? CourseId, string? Notes);
