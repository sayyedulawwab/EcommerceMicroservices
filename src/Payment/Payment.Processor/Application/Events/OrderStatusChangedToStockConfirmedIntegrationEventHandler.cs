using Payment.Processor.Application.Abstractions;
using SharedKernel.Events;

namespace Payment.Processor.Application.Events;
internal sealed class OrderStatusChangedToStockConfirmedIntegrationEventHandler : IHandleMessages<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> _logger;
    private readonly IPaymentService _paymentService;

    public OrderStatusChangedToStockConfirmedIntegrationEventHandler(ILogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler> logger, IPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }
    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@OrderId}", @event, @event.OrderId);

        bool isPaymentSuccess = await _paymentService.ProcessPaymentAsync();

        IEvent integrationEvent = isPaymentSuccess ? new OrderPaymentSucceededIntegrationEvent(@event.OrderId)
                                                : new OrderPaymentFailedIntegrationEvent(@event.OrderId);

        await context.Publish(integrationEvent);
    }
}
