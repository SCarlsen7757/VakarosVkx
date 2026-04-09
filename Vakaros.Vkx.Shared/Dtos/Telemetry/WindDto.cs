namespace Vakaros.Vkx.Shared.Dtos.Telemetry;

public record WindDto(DateTimeOffset Time, float WindDirection, float WindSpeed);
