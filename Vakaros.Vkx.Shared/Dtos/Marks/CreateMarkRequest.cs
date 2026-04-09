namespace Vakaros.Vkx.Shared.Dtos.Marks;

public record CreateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
