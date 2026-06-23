using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    private readonly DbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public EfRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> Query() => _dbSet.AsQueryable();

    public IQueryable<TEntity> QueryReadOnly() => _dbSet.AsNoTracking();

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        _dbSet.FindAsync(new object?[] { id }, cancellationToken).AsTask();

    public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _dbSet.ToListAsync(cancellationToken);

    public Task<List<TEntity>> GetAllReadOnlyAsync(CancellationToken cancellationToken = default) =>
        _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        _dbSet.AddAsync(entity, cancellationToken).AsTask();

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(TEntity entity) => _dbSet.Update(entity);

    public void UpdateRange(IEnumerable<TEntity> entities) => _dbSet.UpdateRange(entities);

    public void Remove(TEntity entity) => _dbSet.Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);
}

public class EfRepository<TAggregateRoot> : EfRepository<TAggregateRoot, Guid>, IRepository<TAggregateRoot>
    where TAggregateRoot : AggregateRoot
{
    public EfRepository(DbContext dbContext)
        : base(dbContext)
    {
    }
}
