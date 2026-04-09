namespace Vakaros.Vkx.Shared.Dtos.BoatClasses;

public record CreateBoatClassRequest(
    string Name,
    double? LengthOverAll,
    double? Beam,
    double? Weight,
    double? BowspritLength,
    List<SailRequest> Sails);
