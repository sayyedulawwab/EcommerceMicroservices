using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.DomainEvents;
using ConcurrencyException = SharedKernel.Exceptions.ConcurrencyException;
using System.Data;

namespace Ordering.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions options, IDomainEventsDispatcher domainEventsDispatcher) : DbContext(options), IApplicationDbContext
{
    public DbSet<Order> Orders => Set<Order>();

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

    private async Task PublishDomainEventsAsync()
    {
        // Pulling empties each aggregate's list, so a second SaveChangesAsync can't publish the same event twice.
        List<IDomainEvent> domainEvents =
        [
            .. ChangeTracker
                .Entries<IHasDomainEvents>()
                .SelectMany(entry => entry.Entity.PullDomainEvents()),
        ];

        await domainEventsDispatcher.DispatchAsync(domainEvents);
    }
}