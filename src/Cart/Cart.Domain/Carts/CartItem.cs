using SharedKernel.Domain;

namespace Cart.Domain.Carts;

public sealed class CartItem : Entity<Guid>
{
    private CartItem(Guid id, Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc) : base(id)
    {
        CartId = cartId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
        CreatedOnUtc = createdOnUtc;
    }

    private CartItem()
    {
    }

    public Guid CartId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }


    public static CartItem Create(Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {
        var cartItem = new CartItem(Guid.NewGuid(), cartId, productId, productName, price, quantity, createdOnUtc);

        return cartItem;
    }

}