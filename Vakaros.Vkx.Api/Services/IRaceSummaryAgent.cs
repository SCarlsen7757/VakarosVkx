namespace Vakaros.Vkx.Api.Services;

public interface IRaceSummaryAgent
{
    IAsyncEnumerable<string> GenerateAsync(
        RaceSummaryContext context, CancellationToken ct);
}
