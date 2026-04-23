using Vakaros.Vkx.Shared.Dtos.BoatClasses;

namespace Vakaros.Vkx.Shared.Dtos.Boats;

public record BoatDto(Guid Id,
                      string Name,
                      string? SailNumber,
                      BoatClassSummaryDto BoatClass,
                      string? Description,
                      DateTimeOffset CreatedAt);
