namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Base entity with audit metadata. Uses Guid for Id to match existing modules.
/// </summary>
public abstract class BaseEntity : Entity
{
    public DateTimeOffset CreatedAt { get; protected set; }

    public Guid? CreatedBy { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public Guid? UpdatedBy { get; protected set; }

    protected BaseEntity()
    {
    }

    protected BaseEntity(Guid id)
        : base(id)
    {
    }

    public void SetCreated(Guid? createdBy, DateTimeOffset createdAt)
    {
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public void SetUpdated(Guid? updatedBy, DateTimeOffset updatedAt)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = updatedAt;
    }
}
