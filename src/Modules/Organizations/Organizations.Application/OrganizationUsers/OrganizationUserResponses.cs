namespace Organizations.Application.OrganizationUsers;

public sealed class OrganizationUserListItemResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
}

public sealed class OrganizationUserDetailResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
}

public sealed class AssignOrganizationUserResponse
{
    public Guid Id { get; init; }
}
