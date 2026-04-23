namespace Vakaros.Vkx.Shared.Dtos.Shares;

public record SessionShareDto(Guid SessionId, Guid TeamId, string TeamName, string Permission, DateTimeOffset CreatedAt);
public record CreateOrUpdateShareRequest(Guid TeamId, string Permission);
