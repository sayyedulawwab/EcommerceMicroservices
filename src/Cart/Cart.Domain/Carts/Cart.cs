using Cart.Domain.Shared;
using Cart.Domain.Abstractions;

namespace Cart.Domain.Carts;
public sealed class Cart : Entity<Guid>
{
    public Cart(Guid id, long userId, Money totalPrice, DateTime createdOn) : base(id)
    {
        UserId = userId;
        TotalPrice = totalPrice;
        CreatedOnUtc = createdOn;

    }

    private Cart()
    {
    }

    public long UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public List<CartItem> CartItems { get; private set; } = new List<CartItem>();


    public static Cart Create(long userId, List<CartItem> cartItems, DateTime createdOnUtc)
    {
        var cart = new Cart(Guid.NewGuid(), userId, Money.Zero(), createdOnUtc);

        foreach (var cartItem in cartItems)
        {
            cart.CartItems.Add(cartItem);

            if (cart.TotalPrice.IsZero())
            {
                cart.TotalPrice = new Money(cartItem.Price.Amount, cartItem.Price.Currency);
            }
            else
            {
                if (cart.TotalPrice.Currency != cartItem.Price.Currency)
                {
                    throw new InvalidOperationException("Currencies have to be equal");
                }

                cart.TotalPrice += new Money(cartItem.Price.Amount * cartItem.Quantity, cartItem.Price.Currency);
            }


        }

        return cart;
    }


    public void AddCartItems(List<CartItem> cartItems)
    {
        CartItems.AddRange(cartItems);

        foreach (var cartItem in cartItems)
        {
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


}
