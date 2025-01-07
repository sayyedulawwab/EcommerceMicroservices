using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;

namespace Catalog.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(ApplicationDbContext dbContext)
    where TEntity : Entity<long>
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TEntity>().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
    }

    public void Add(TEntity entity)
    {
        DbContext.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        DbContext.Remove(entity);
    }

    public void Update(TEntity entity)
    {
        DbContext.Update(entity);
    }
}