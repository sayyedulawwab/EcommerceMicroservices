using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

public sealed class OrderItem : Entity<long>
{
    private OrderItem(long productId, int quantity, DateTime createdOnUtc)
    {
        ProductId = productId;
        ProductName = string.Empty;
        Price = Money.Zero();
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    private OrderItem()
    {
    }

    public long OrderId { get; } // set by EF Core from the Order relationship
    public long ProductId { get; private set; }

    // Name and price are unknown until Catalog confirms the stock; they come from Catalog, never from the client.
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public Money LineTotal => new(Price.Amount * Quantity, Price.Currency);

    public static OrderItem Create(long productId, int quantity, DateTime createdOnUtc)
    {
        return new OrderItem(productId, quantity, createdOnUtc);
    }

    internal void SetPrice(string productName, Money price)
    {
        ProductName = productName;
        Price = price;
    }
}
