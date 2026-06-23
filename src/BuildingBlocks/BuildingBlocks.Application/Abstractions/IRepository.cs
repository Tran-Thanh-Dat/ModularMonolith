using System;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Application.Abstractions;

[Obsolete("Use IRepository<TEntity, TKey> via IUnitOfWork.Repository<TEntity, TKey>() instead.")]
public interface IRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot
{
    Task<TAggregateRoot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregateRoot entity, CancellationToken cancellationToken = default);

    void Update(TAggregateRoot entity);

    void Remove(TAggregateRoot entity);
}
