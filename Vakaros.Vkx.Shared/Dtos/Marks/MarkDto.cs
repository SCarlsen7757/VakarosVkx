namespace Vakaros.Vkx.Shared.Dtos.Marks;

public record MarkDto(int Id, string Name, DateOnly ActiveFrom, DateOnly? ActiveUntil, double Latitude, double Longitude, string? Description);
