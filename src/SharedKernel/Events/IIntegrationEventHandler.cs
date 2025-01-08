namespace SharedKernel.Events;
public interface IIntegrationEventHandler<T> : IHandleMessages<T>
    where T : IIntegrationEvent
{
}