namespace BuildingBlocks.Domain.Primitives;

/// <summary>
/// Backward-compatible alias for <see cref="SoftDeleteEntity"/>.
/// </summary>
public abstract class SoftDeletableEntity : SoftDeleteEntity
{
    protected SoftDeletableEntity()
    {
    }

    protected SoftDeletableEntity(Guid id)
        : base(id)
    {
    }
}
