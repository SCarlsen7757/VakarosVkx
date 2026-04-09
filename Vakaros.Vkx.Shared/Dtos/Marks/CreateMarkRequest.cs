namespace Vakaros.Vkx.Shared.Dtos;

public record CreateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
