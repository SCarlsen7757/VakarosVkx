namespace Vakaros.Vkx.Shared.Dtos.Boats;

public record CreateBoatRequest(string Name,
                                string? SailNumber,
                                int BoatClassId,
                                string? Description);
