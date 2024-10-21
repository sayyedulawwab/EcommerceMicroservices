using Cart.Domain.Shared;
using Cart.Domain.Abstractions;

namespace Cart.Domain.Carts;

public sealed class CartItem : Entity<Guid>
{
    private CartItem(Guid id, Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOn) : base(id)
    {
        CartId = cartId;
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
        CreatedOn = createdOn;
    }

    private CartItem()
    {
    }

    public Guid CartId { get; private set; }
    public long ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public DateTime? UpdatedOn { get; private set; }


    public static CartItem Create(Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOn)
    {
        var cartItem = new CartItem(Guid.NewGuid(), cartId, productId, productName, price, quantity, createdOn);

        return cartItem;
    }

}