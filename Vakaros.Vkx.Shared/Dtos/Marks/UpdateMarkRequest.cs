namespace Vakaros.Vkx.Shared.Dtos.Marks;

public record UpdateMarkRequest(string Name, DateOnly ActiveFrom, DateOnly? ActiveUntil, double Latitude, double Longitude, string? Description);
