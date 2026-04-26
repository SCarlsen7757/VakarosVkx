namespace Vakaros.Vkx.Shared.Dtos.Shares;

public record SessionShareDto(Guid SessionId, Guid TeamId, string TeamName, DateTimeOffset CreatedAt);
public record CreateShareRequest(Guid TeamId);
