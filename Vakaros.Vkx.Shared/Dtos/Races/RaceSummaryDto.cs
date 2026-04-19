namespace Vakaros.Vkx.Shared.Dtos.Races;

public record RaceSummaryDto(
    string Content,
    string Model,
    DateTimeOffset GeneratedAt,
    bool IsStale);
