using SharedKernel.Domain;

namespace Cart.Domain.Carts;
public sealed class Cart : Entity<Guid>
{
    public Cart(Guid id, long userId, Money totalPrice, DateTime createdOnUtc) : base(id)
    {
        UserId = userId;
        TotalPrice = totalPrice;
        CreatedOnUtc = createdOnUtc;

    }

    private Cart()
    {
    }

    public long UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public List<CartItem> CartItems { get; private set; } = [];


    public static Cart Create(long userId, DateTime createdOnUtc)
    {
        var cart = new Cart(Guid.NewGuid(), userId, Money.Zero(), createdOnUtc);

        return cart;
    }

    public void AddCartItem(Guid cartId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {
        var cartItem = CartItem.Create(cartId, productId, productName, price, quantity, createdOnUtc);

        CartItems.Add(cartItem);

        if (TotalPrice.IsZero())
        {
            TotalPrice = new Money(cartItem.Price.Amount, cartItem.Price.Currency);
        }
        else
        {
            if (TotalPrice.Currency != cartItem.Price.Currency)
            {
                throw new InvalidOperationException("Currencies have to be equal");
            }

            TotalPrice += new Money(cartItem.Price.Amount * cartItem.Quantity, cartItem.Price.Currency);
        }
    }
}