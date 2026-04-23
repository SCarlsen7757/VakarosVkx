namespace Vakaros.Vkx.Shared.Dtos.Boats;

public record UpdateBoatRequest(string Name,
                                string? SailNumber,
                                Guid BoatClassId,
                                string? Description);
