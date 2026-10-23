using SharedKernel.Domain;

namespace Cart.Domain.Carts;

/// <summary>
/// A shopping cart. Names and prices here are what the client showed the user; the order uses Catalog's prices.
/// </summary>
public sealed class Cart : Entity<CartId>
{
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
    public List<CartItem> CartItems { get; private set; } = [];

    public static Cart Create(Guid userId, DateTime createdOnUtc)
    {
        var cart = new Cart(CartId.New(), userId, Money.Zero(), createdOnUtc);

        return cart;
    }

    /// <summary>Adds the product, or increases its quantity if it is already in the cart.</summary>
    public Result AddItem(Guid productId, string productName, Money price, int quantity, DateTime utcNow)
    {
        CartItem? existingItem = CartItems.Find(item => item.ProductId == productId);
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
            CartItems.Add(CartItem.Create(Id, productId, productName, price, quantity, utcNow));
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

        CartItem? item = CartItems.Find(cartItem => cartItem.ProductId == productId);

        if (item is null)
        {
            return Result.Failure(CartErrors.ItemNotFound(productId));
        }

        if (quantity == 0)
        {
            CartItems.Remove(item);
        }
        else
        {
            item.Update(item.ProductName, item.Price, quantity);
        }

        return Changed(utcNow);
    }

    public Result RemoveItem(Guid productId, DateTime utcNow)
    {
        int removed = CartItems.RemoveAll(item => item.ProductId == productId);

        return removed == 0
            ? Result.Failure(CartErrors.ItemNotFound(productId))
            : Changed(utcNow);
    }

    private Result Changed(DateTime utcNow)
    {
        TotalPrice = CartItems.Count == 0
            ? Money.Zero()
            : CartItems
                .Select(item => item.LineTotal)
                .Aggregate(Money.Zero(CartItems[0].Price.Currency), (total, lineTotal) => total + lineTotal);

        UpdatedOnUtc = utcNow;

        return Result.Success();
    }
}
