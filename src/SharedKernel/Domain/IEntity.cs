namespace SharedKernel.Domain;

public interface IEntity
{
    IReadOnlyList<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
}