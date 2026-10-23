using SharedKernel.Domain;

namespace Cart.Domain.Carts;

public sealed class CartItem : Entity<Guid>
{
    public const int MaxQuantity = 1000;

    private CartItem(Guid id, Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc) : base(id)
    {
        CartId = cartId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    #pragma warning disable CS8618 // Used by Newtonsoft.Json to deserialize the cart from Redis
    private CartItem()
    {
    }
    #pragma warning restore CS8618

    public Guid CartId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public Money LineTotal => new(Price.Amount * Quantity, Price.Currency);

    public static CartItem Create(Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {
        var cartItem = new CartItem(Guid.NewGuid(), cartId, productId, productName, price, quantity, createdOnUtc);

        return cartItem;
    }

    internal void Update(string productName, Money price, int quantity)
    {
        ProductName = productName;
        Price = price;
        Quantity = quantity;
    }
}
