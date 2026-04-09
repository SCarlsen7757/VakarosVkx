namespace Vakaros.Vkx.Shared.Dtos.Marks;

public record CreateMarkRequest(string Name, DateOnly ActiveFrom, DateOnly? ActiveUntil, double Latitude, double Longitude, string? Description);
