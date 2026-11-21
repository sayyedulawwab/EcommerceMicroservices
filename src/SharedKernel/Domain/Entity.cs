namespace SharedKernel.Domain;

/// <summary>
/// Anything with an identity of its own. Child entities such as order lines derive from this directly.
/// </summary>
public abstract class Entity<TId>
{
    protected Entity(TId id)
    {
        Id = id;
    }

#pragma warning disable CS8618 // EF Core sets Id after calling this constructor.
    protected Entity()
    {
    }
#pragma warning restore CS8618

    public TId Id { get; init; }
}
