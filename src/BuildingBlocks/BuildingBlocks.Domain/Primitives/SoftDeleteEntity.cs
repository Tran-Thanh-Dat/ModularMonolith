namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Entity with soft-delete metadata.
/// </summary>
public abstract class SoftDeleteEntity : BaseEntity
{
    public bool IsDeleted { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }

    public Guid? DeletedBy { get; protected set; }

    protected SoftDeleteEntity()
    {
    }

    protected SoftDeleteEntity(Guid id)
        : base(id)
    {
    }

    public void MarkDeleted(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        DeletedBy = deletedBy;
        DeletedAt = deletedAt;
    }

    public void Restore(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        IsDeleted = false;
        DeletedBy = null;
        DeletedAt = null;
        SetUpdated(updatedBy, updatedAt);
    }
}
