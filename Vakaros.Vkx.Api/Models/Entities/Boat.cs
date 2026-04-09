namespace Vakaros.Vkx.Api.Models.Entities;

public class Boat
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SailNumber { get; set; }
    public int BoatClassId { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public BoatClass BoatClass { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = [];
}
