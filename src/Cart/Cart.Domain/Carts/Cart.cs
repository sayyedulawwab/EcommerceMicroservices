using SharedKernel.Domain;

namespace Cart.Domain.Carts;

/// <summary>
/// A shopping cart. Names and prices here are what the client showed the user; the order uses Catalog's prices.
/// </summary>
public sealed class Cart : Entity<CartId>
{
    private readonly List<CartItem> _cartItems = [];

    public Cart(CartId id, Guid userId, Money totalPrice, DateTime createdOnUtc) : base(id)
    {
        UserId = userId;
        TotalPrice = totalPrice;
        CreatedOnUtc = createdOnUtc;

    }

    #pragma warning disable CS8618 // Used by Newtonsoft.Json to deserialize the cart from Redis
    private Cart()
    {
    }
    #pragma warning restore CS8618

    public Guid UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public IReadOnlyList<CartItem> CartItems => _cartItems;

    public static Cart Create(Guid userId, DateTime createdOnUtc)
    {
        var cart = new Cart(CartId.New(), userId, Money.Zero(), createdOnUtc);

        return cart;
    }

    /// <summary>Adds the product, or increases its quantity if it is already in the cart.</summary>
    public Result AddItem(Guid productId, string productName, Money price, int quantity, DateTime utcNow)
    {
        CartItem? existingItem = _cartItems.Find(item => item.ProductId == productId);
        int newQuantity = (existingItem?.Quantity ?? 0) + quantity;

        if (quantity <= 0 || newQuantity > CartItem.MaxQuantity)
        {
            return Result.Failure(CartErrors.InvalidQuantity(productId));
        }

        if (CartItems.Count > 0 && CartItems[0].Price.Currency != price.Currency)
        {
            return Result.Failure(CartErrors.MixedCurrencies);
        }

        if (existingItem is null)
        {
            _cartItems.Add(CartItem.Create(Id, productId, productName, price, quantity, utcNow));
        }
        else
        {
            existingItem.Update(productName, price, newQuantity);
        }

        return Changed(utcNow);
    }

    /// <summary>Sets the quantity of a product in the cart; zero removes it.</summary>
    public Result SetItemQuantity(Guid productId, int quantity, DateTime utcNow)
    {
        if (quantity < 0 || quantity > CartItem.MaxQuantity)
        {
            return Result.Failure(CartErrors.InvalidQuantity(productId));
        }

        CartItem? item = _cartItems.Find(cartItem => cartItem.ProductId == productId);

        if (item is null)
        {
            return Result.Failure(CartErrors.ItemNotFound(productId));
        }

        if (quantity == 0)
        {
            _cartItems.Remove(item);
        }
        else
        {
            item.Update(item.ProductName, item.Price, quantity);
        }

        return Changed(utcNow);
    }

    public Result RemoveItem(Guid productId, DateTime utcNow)
    {
        int removed = _cartItems.RemoveAll(item => item.ProductId == productId);

        return removed == 0
            ? Result.Failure(CartErrors.ItemNotFound(productId))
            : Changed(utcNow);
    }

    private Result Changed(DateTime utcNow)
    {
        Money total = CartItems.Count == 0 ? Money.Zero() : Money.Zero(CartItems[0].Price.Currency);

        foreach (CartItem item in CartItems)
        {
            Result<Money> sum = total.Add(item.LineTotal);

            if (sum.IsFailure)
            {
                return Result.Failure(CartErrors.MixedCurrencies);
            }

            total = sum.Value;
        }

        TotalPrice = total;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }
}
