
using Identity.Application.Abstractions.Data;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Domain;
using SharedKernel.DomainEvents;
using SharedKernel.Exceptions;
using System.Data;

namespace Identity.Infrastructure;

public sealed class ApplicationDbContext(DbContextOptions options, IDomainEventsDispatcher domainEventsDispatcher) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DependencyInjection).Assembly);

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