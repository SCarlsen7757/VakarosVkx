namespace Vakaros.Vkx.Shared.Dtos.Marks;

public record UpdateMarkRequest(string Name, int Year, double Latitude, double Longitude, string? Description);
