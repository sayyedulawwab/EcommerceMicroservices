namespace SharedKernel.Domain;

/// <summary>
/// Lets code outside the domain collect recorded events without knowing the aggregate's ID type.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Returns the recorded events and removes them, so each event is handed out once.</summary>
    IReadOnlyList<IDomainEvent> PullDomainEvents();
}
