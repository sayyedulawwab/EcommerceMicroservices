using Payment.Processor.Application.Abstractions;

namespace Payment.Processor.Application.Events;
internal class OrderStatusChangedToStockConfirmedIntegrationEventHandler : IHandleMessages<OrderStatusChangedToStockConfirmedIntegrationEvent>
{
    private readonly ILogger<OrderStatusChangedToStockConfirmedIntegrationEvent> _logger;
    private readonly IPaymentService _paymentService;

    public OrderStatusChangedToStockConfirmedIntegrationEventHandler(ILogger<OrderStatusChangedToStockConfirmedIntegrationEvent> logger, IPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }
    public async Task Handle(OrderStatusChangedToStockConfirmedIntegrationEvent @event, IMessageHandlerContext context)
    {

        _logger.LogInformation("Handling integration event: ({@IntegrationEvent}) with Order Id: {@orderId}", @event, @event.orderId);

        var isPaymentSuccess = await _paymentService.ProcessPaymentAsync();

        var integrationEvent = isPaymentSuccess ? (IEvent)new OrderPaymentSucceededIntegrationEvent(@event.orderId)
                                                : new OrderPaymentFailedIntegrationEvent(@event.orderId);

        await context.Publish(integrationEvent);
    }
}
