using Microsoft.AspNetCore.Identity;

namespace Vakaros.Vkx.Api.Models.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
