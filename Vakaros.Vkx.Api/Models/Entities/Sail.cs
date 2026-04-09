namespace Vakaros.Vkx.Api.Models.Entities;

public class Sail
{
    public int Id { get; set; }
    public int BoatClassId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Area { get; set; }

    public BoatClass BoatClass { get; set; } = null!;
}
