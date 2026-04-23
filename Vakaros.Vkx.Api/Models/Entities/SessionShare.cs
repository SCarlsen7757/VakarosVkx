namespace Vakaros.Vkx.Api.Models.Entities;

public enum SharePermission
{
    Read = 0,
    Write = 1,
}

public class SessionShare
{
    public Guid SessionId { get; set; }
    public Guid TeamId { get; set; }
    public SharePermission Permission { get; set; } = SharePermission.Read;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Session Session { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
