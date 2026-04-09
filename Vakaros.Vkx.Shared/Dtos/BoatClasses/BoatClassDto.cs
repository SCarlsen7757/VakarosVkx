namespace Vakaros.Vkx.Shared.Dtos.BoatClasses;

/// <summary>
/// Full boat class with its sail list.
/// All measurements are in SI units: lengths in metres, weight in kg, sail areas in m².
/// </summary>
public record BoatClassDto(
    int Id,
    string Name,
    double? LengthOverAll,
    double? Beam,
    double? Weight,
    double? BowspritLength,
    DateTimeOffset CreatedAt,
    List<SailDto> Sails);
