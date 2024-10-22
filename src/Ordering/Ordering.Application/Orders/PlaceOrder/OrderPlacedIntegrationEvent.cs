namespace Ordering.Application.Orders.PlaceOrder;
internal class OrderPlacedIntegrationEvent : IEvent
{
    private OrderPlacedIntegrationEvent(long orderId, List<OrderStockItem> orderStockItems)
    {
        OrderId = orderId;
        OrderStockItems = orderStockItems;
    }

    public long OrderId { get; private set; }
    public List<OrderStockItem> OrderStockItems { get; private set; } = new List<OrderStockItem>();

    public static OrderPlacedIntegrationEvent Create(long orderId, List<OrderStockItem> orderStockItems)
    {
        var integrationEvent = new OrderPlacedIntegrationEvent(orderId, orderStockItems);

        return integrationEvent;
    }
}
