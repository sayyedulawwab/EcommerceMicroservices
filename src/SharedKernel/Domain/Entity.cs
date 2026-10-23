namespace SharedKernel.Domain;

public abstract class Entity<TEntityId> : IEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    protected Entity(TEntityId id)
    {
        Id = id;
    }

    #pragma warning disable CS8618 // Used by EF Core to materialize the entity
    protected Entity()
    {
    }
    #pragma warning restore CS8618

    public TEntityId Id { get; init; }

    public IReadOnlyList<IDomainEvent> GetDomainEvents()
    {
        return [.. _domainEvents];
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}