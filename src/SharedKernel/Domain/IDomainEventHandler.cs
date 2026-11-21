namespace SharedKernel.Domain;

/// <summary>
/// The untyped view of a handler, so a dispatcher that only knows <see cref="IDomainEvent"/> can call it.
/// </summary>
public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);

    // The dispatcher resolves handlers for the event's runtime type, so this cast always succeeds.
    Task IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)domainEvent, cancellationToken);
}
