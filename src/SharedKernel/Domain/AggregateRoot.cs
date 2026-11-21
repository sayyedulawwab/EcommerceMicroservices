namespace SharedKernel.Domain;

/// <summary>
/// The entry point of an aggregate. Only aggregate roots record domain events: they are
/// the unit that is loaded and saved, so they are the place where "something happened".
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _pendingEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyList<IDomainEvent> PullDomainEvents()
    {
        IDomainEvent[] events = [.. _pendingEvents];
        _pendingEvents.Clear();

        return events;
    }

    protected void RecordDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _pendingEvents.Add(domainEvent);
    }
}
