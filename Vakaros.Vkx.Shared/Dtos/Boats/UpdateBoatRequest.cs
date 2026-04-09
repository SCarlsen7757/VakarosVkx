namespace Vakaros.Vkx.Shared.Dtos.Boats;

public record UpdateBoatRequest(string Name,
                                string? SailNumber,
                                int BoatClassId,
                                string? Description);
