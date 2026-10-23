using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public sealed class OrderItem : Entity<OrderItemId>
{
    private OrderItem(OrderItemId id, OrderId orderId, Guid productId, int quantity, DateTime createdOnUtc)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = string.Empty;
        Price = Money.Zero();
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    public OrderId OrderId { get; }
    public Guid ProductId { get; private set; }

    // Name and price are unknown until Catalog confirms the stock; they come from Catalog, never from the client.
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public Money LineTotal => new(Price.Amount * Quantity, Price.Currency);

    public static OrderItem Create(OrderId orderId, Guid productId, int quantity, DateTime createdOnUtc)
    {
        return new OrderItem(OrderItemId.New(), orderId, productId, quantity, createdOnUtc);
    }

    internal void SetPrice(string productName, Money price)
    {
        ProductName = productName;
        Price = price;
    }
}
