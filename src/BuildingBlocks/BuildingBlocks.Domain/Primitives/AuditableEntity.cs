namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Backward-compatible alias for <see cref="BaseEntity"/>.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(Guid id)
        : base(id)
    {
    }
}
