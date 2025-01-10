using MassTransit;

namespace SharedKernel.Events;
public interface IIntegrationEventHandler<in TIntegrationEvent> : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : IntegrationEvent
{
}