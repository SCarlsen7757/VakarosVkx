namespace Vakaros.Vkx.Shared.Dtos.Teams;

public record TeamDto(Guid Id, string Name, DateTimeOffset CreatedAt, int MemberCount, string Role);
public record TeamDetailDto(Guid Id, string Name, DateTimeOffset CreatedAt, IReadOnlyList<TeamMemberDto> Members);
public record TeamMemberDto(Guid UserId, string Email, string DisplayName, string Role, DateTimeOffset JoinedAt);
public record CreateTeamRequest(string Name);
public record UpdateTeamRequest(string Name);
public record InviteMemberRequest(string Email, string Role);
public record TeamInviteDto(Guid Id, string Email, string Role, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
public record TeamInviteWithUrlDto(Guid Id, string Email, string Role, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, string AcceptUrl);
public record AcceptInviteRequest(string Token);
public record UpdateMemberRoleRequest(string Role);
