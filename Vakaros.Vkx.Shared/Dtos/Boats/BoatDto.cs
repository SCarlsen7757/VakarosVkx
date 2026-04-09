namespace Vakaros.Vkx.Shared.Dtos.Boat;

public record BoatDto(int Id, string Name, string? SailNumber, string? BoatClass, string? Description, DateTimeOffset CreatedAt);
