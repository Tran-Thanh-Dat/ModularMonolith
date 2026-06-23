using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BuildingBlocks.Infrastructure.Persistence;

public class EfUnitOfWork<TDbContext> : IUnitOfWork
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly Dictionary<(Type Entity, Type Key), object> _repositories = new();
    private IDbContextTransaction? _currentTransaction;

    public EfUnitOfWork(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class
    {
        var key = (typeof(TEntity), typeof(TKey));
        if (_repositories.TryGetValue(key, out var repository))
        {
            return (IRepository<TEntity, TKey>)repository;
        }

        var instance = new EfRepository<TEntity, TKey>(_dbContext);
        _repositories[key] = instance;
        return instance;
    }

    public virtual Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            return;
        }

        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.CommitAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    /// <summary>
    /// Executes an action inside an explicit database transaction.
    /// The caller is responsible for calling SaveChanges within the action if needed.
    /// Do not combine with MediatR TransactionBehavior on the same command pipeline.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
