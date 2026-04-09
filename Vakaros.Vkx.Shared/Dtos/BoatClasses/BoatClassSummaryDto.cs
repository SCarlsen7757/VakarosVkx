namespace Vakaros.Vkx.Shared.Dtos.BoatClasses;

/// <summary>
/// Slim summary of a boat class for embedding in boat responses.
/// All measurements are in SI units: lengths in metres, weight in kg.
/// </summary>
public record BoatClassSummaryDto(
    int Id,
    string Name,
    double? LengthOverAll,
    double? Beam,
    double? Weight,
    double? BowspritLength);
