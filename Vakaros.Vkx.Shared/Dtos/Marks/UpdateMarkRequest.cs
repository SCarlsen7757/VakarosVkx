namespace Vakaros.Vkx.Shared.Dtos;

public record UpdateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
