namespace Vakaros.Vkx.Api.Auth;

public static class AuthConstants
{
    public const string PatScheme = "PAT";
    public const string PatPrefix = "vkx_";

    /// <summary>
    /// Synthetic user used in single-user mode.
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-7000-8000-000000000001");
    public const string SystemUserEmail = "system@vakaros.local";

    public const string SessionAccessPolicy = "SessionAccess";
    public const string SessionWritePolicy = "SessionWrite";

    public const string CsrfHeaderName = "X-CSRF-Token";
    public const string CsrfCookieName = "vkx.csrf";

    public const string AdminRole = "Admin";
    public const string UserRole = "User";
}
