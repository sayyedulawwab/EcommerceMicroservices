using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public sealed class OrderItem : Entity<long>
{
    private OrderItem(long orderId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    private OrderItem()
    {
    }

    public long OrderId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }


    public static OrderItem Create(long orderId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {
        var orderItem = new OrderItem(orderId, productId, productName, price, quantity, createdOnUtc);

        return orderItem;
    }

}