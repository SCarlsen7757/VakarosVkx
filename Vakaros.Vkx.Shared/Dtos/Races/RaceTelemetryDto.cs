using Vakaros.Vkx.Shared.Dtos.Telemetry;

namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceTelemetryDto(
    List<PositionDto> Positions,
    List<WindDto> Wind,
    List<SpeedThroughWaterDto> SpeedThroughWater,
    List<DepthDto> Depth,
    List<TemperatureDto> Temperature,
    List<LoadDto> Load,
    List<ShiftAngleDto> ShiftAngles);
