namespace Ordering.Application.Orders.Events;
internal record OrderItemsStockRejectedIntegrationEvent(long orderId) : IEvent;
