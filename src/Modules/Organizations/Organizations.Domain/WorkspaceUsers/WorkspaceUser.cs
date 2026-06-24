using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Organizations.Domain.WorkspaceUsers;

public sealed class WorkspaceUser : SoftDeletableEntity
{
    private WorkspaceUser()
    {
    }

    private WorkspaceUser(
        Guid id,
        Guid tenantId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset joinedAt,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        WorkspaceId = workspaceId;
        UserId = userId;
        IsActive = true;
        JoinedAt = joinedAt;
        SetCreated(createdBy, createdAt);
    }

    public Guid TenantId { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }

    public DateTimeOffset? LeftAt { get; private set; }

    public static WorkspaceUser Create(
        Guid tenantId,
        Guid workspaceId,
        Guid userId,
        DateTimeOffset joinedAt,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.", "WorkspaceUser.InvalidTenant");
        }

        if (workspaceId == Guid.Empty)
        {
            throw new DomainException("Workspace id is required.", "WorkspaceUser.InvalidWorkspace");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.", "WorkspaceUser.InvalidUser");
        }

        return new WorkspaceUser(
            Guid.NewGuid(),
            tenantId,
            workspaceId,
            userId,
            joinedAt,
            createdAt,
            createdBy);
    }

    public void Activate(DateTimeOffset joinedAt)
    {
        if (IsActive)
        {
            throw new DomainException("Workspace membership is already active.", "WorkspaceUser.AlreadyActive");
        }

        IsActive = true;
        JoinedAt = joinedAt;
        LeftAt = null;
    }

    public void Deactivate(DateTimeOffset leftAt)
    {
        if (!IsActive)
        {
            throw new DomainException("Workspace membership is already inactive.", "WorkspaceUser.AlreadyInactive");
        }

        IsActive = false;
        LeftAt = leftAt;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Workspace membership is already removed.", "WorkspaceUser.AlreadyRemoved");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
