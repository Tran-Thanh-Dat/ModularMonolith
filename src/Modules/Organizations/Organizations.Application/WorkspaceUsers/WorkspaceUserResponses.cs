namespace Organizations.Application.WorkspaceUsers;

public sealed class WorkspaceUserListItemResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
}

public sealed class WorkspaceUserDetailResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

public sealed class AssignWorkspaceUserResponse
{
    public Guid Id { get; init; }
}
