using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Domain;

namespace SharedKernel.DomainEvents;

public sealed class DomainEventsDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventsDispatcher
{
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            // A new scope per event: its handlers get their own DbContext, not the one that just saved.
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();

            // Handlers are registered as IDomainEventHandler<TEvent> for the concrete event type.
            Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            // Every typed handler is also an untyped IDomainEventHandler, which does the cast itself.
            foreach (IDomainEventHandler handler in scope.ServiceProvider.GetServices(handlerType).OfType<IDomainEventHandler>())
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
        }
    }
}
