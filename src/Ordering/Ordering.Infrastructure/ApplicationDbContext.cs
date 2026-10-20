using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Domain;
using SharedKernel.DomainEvents;
using ConcurrencyException = SharedKernel.Exceptions.ConcurrencyException;
using System.Data;

namespace Ordering.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions options, IDomainEventsDispatcher domainEventsDispatcher) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DependencyInjection).Assembly);

        // MassTransit transactional outbox (published events) and inbox (consumed message de-duplication).
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            int result = await base.SaveChangesAsync(cancellationToken);

            await PublishDomainEventsAsync();

            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Concurrency exception occurred.", ex);
        }
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        if (Database.CurrentTransaction is not null)
        {
            await operation(cancellationToken);

            return;
        }

        await using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

        await operation(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task PublishDomainEventsAsync()
    {
        var domainEvents = ChangeTracker
            .Entries<IEntity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                IReadOnlyList<IDomainEvent> domainEvents = entity.GetDomainEvents();

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}