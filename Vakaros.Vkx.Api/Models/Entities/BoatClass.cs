namespace Vakaros.Vkx.Api.Models.Entities;

public class BoatClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double? LengthOverAll { get; set; }
    public double? Beam { get; set; }
    public double? Weight { get; set; }
    public double? BowspritLength { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Sail> Sails { get; set; } = [];
}
