using Ordering.Domain.Abstractions;
using Ordering.Domain.Shared;

namespace Ordering.Domain.Orders;

public sealed class OrderItem : Entity<long>
{
    private OrderItem(long orderId, long productId, string productName, Money price, int quantity, DateTime createdOn)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
        CreatedOn = createdOn;
    }

    private OrderItem()
    {
    }

    public long OrderId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }


    public static OrderItem Create(long orderId, long productId, string productName, Money price, int quantity, DateTime createdOn)
    {
        var orderItem = new OrderItem(orderId, productId, productName, price, quantity, createdOn);

        return orderItem;
    }

}