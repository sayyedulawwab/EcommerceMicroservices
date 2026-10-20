using Cart.Domain.Carts;
using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Domain.UnitTests.Carts;

public sealed class CartTests
{
    private const long UserId = 7;
    private const long ProductA = 1;
    private const long ProductB = 2;

    private static readonly DateTime CreatedOnUtc = new(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UtcNow = new(2026, 1, 2, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_NewCart_HasUserIdAndIsEmpty()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);

        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Equal(UserId, cart.UserId);
        Assert.Equal(CreatedOnUtc, cart.CreatedOnUtc);
        Assert.Null(cart.UpdatedOnUtc);
        Assert.Empty(cart.CartItems);
        Assert.Equal(0m, cart.TotalPrice.Amount);
    }

    [Fact]
    public void AddItem_NewProduct_AddsItem()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);

        Result result = cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, UtcNow);

        Assert.True(result.IsSuccess);
        CartItem item = Assert.Single(cart.CartItems);
        Assert.Equal(cart.Id, item.CartId);
        Assert.Equal(ProductA, item.ProductId);
        Assert.Equal("Keyboard", item.ProductName);
        Assert.Equal(Usd(10m), item.Price);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(UtcNow, item.CreatedOnUtc);
    }

    [Fact]
    public void AddItem_SameProductAgain_IncreasesQuantity()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, UtcNow);

        Result result = cart.AddItem(ProductA, "Keyboard", Usd(10m), 3, UtcNow);

        Assert.True(result.IsSuccess);
        CartItem item = Assert.Single(cart.CartItems);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(Usd(50m), cart.TotalPrice);
    }

    [Fact]
    public void AddItem_SameProductBeyondTheMaximum_IsRejected()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), CartItem.MaxQuantity, UtcNow);

        Result result = cart.AddItem(ProductA, "Keyboard", Usd(10m), 1, UtcNow);

        Assert.Equal(CartErrors.InvalidQuantity(ProductA), result.Error);
        Assert.Equal(CartItem.MaxQuantity, Assert.Single(cart.CartItems).Quantity);
    }

    [Fact]
    public void AddItem_SameProductWithNewNameAndPrice_KeepsLatestNameAndPrice()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 1, UtcNow);

        cart.AddItem(ProductA, "Mechanical keyboard", Usd(12m), 1, UtcNow);

        CartItem item = Assert.Single(cart.CartItems);
        Assert.Equal("Mechanical keyboard", item.ProductName);
        Assert.Equal(Usd(12m), item.Price);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(Usd(24m), cart.TotalPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_NonPositiveQuantity_ReturnsInvalidQuantity(int quantity)
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);

        Result result = cart.AddItem(ProductA, "Keyboard", Usd(10m), quantity, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.InvalidQuantity(ProductA), result.Error);
        Assert.Empty(cart.CartItems);
        Assert.Null(cart.UpdatedOnUtc);
    }

    [Fact]
    public void AddItem_DifferentCurrencyThanExistingItems_ReturnsMixedCurrencies()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 1, UtcNow);

        Result result = cart.AddItem(ProductB, "Mouse", new Money(500m, Currency.Bdt), 1, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.MixedCurrencies, result.Error);
        Assert.Single(cart.CartItems);
        Assert.Equal(Usd(10m), cart.TotalPrice);
    }

    [Fact]
    public void AddItem_SeveralItems_TotalIsSumOfPriceTimesQuantityForEveryItem()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);

        cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, UtcNow);
        cart.AddItem(ProductB, "Mouse", Usd(5m), 3, UtcNow);

        // Regression: the first item's quantity used to be ignored (10 + 3 x 5 = 25).
        Assert.Equal(35m, cart.TotalPrice.Amount);
        Assert.Equal(Currency.Usd, cart.TotalPrice.Currency);
    }

    [Fact]
    public void AddItem_Success_SetsUpdatedOnUtc()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);

        cart.AddItem(ProductA, "Keyboard", Usd(10m), 1, UtcNow);

        Assert.Equal(UtcNow, cart.UpdatedOnUtc);
    }

    [Fact]
    public void SetItemQuantity_ExistingProduct_UpdatesQuantityAndTotal()
    {
        ShoppingCart cart = CreateCartWithTwoItems();
        DateTime later = UtcNow.AddMinutes(5);

        Result result = cart.SetItemQuantity(ProductA, 4, later);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, cart.CartItems.Single(item => item.ProductId == ProductA).Quantity);
        Assert.Equal(Usd(55m), cart.TotalPrice);
        Assert.Equal(later, cart.UpdatedOnUtc);
    }

    [Fact]
    public void SetItemQuantity_Zero_RemovesItem()
    {
        ShoppingCart cart = CreateCartWithTwoItems();

        Result result = cart.SetItemQuantity(ProductA, 0, UtcNow);

        Assert.True(result.IsSuccess);
        CartItem remaining = Assert.Single(cart.CartItems);
        Assert.Equal(ProductB, remaining.ProductId);
        Assert.Equal(Usd(15m), cart.TotalPrice);
    }

    [Fact]
    public void SetItemQuantity_UnknownProduct_ReturnsItemNotFound()
    {
        ShoppingCart cart = CreateCartWithTwoItems();

        Result result = cart.SetItemQuantity(99, 1, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound(99), result.Error);
        Assert.Equal(2, cart.CartItems.Count);
    }

    [Fact]
    public void SetItemQuantity_Negative_ReturnsInvalidQuantity()
    {
        ShoppingCart cart = CreateCartWithTwoItems();

        Result result = cart.SetItemQuantity(ProductA, -1, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.InvalidQuantity(ProductA), result.Error);
        Assert.Equal(2, cart.CartItems.Single(item => item.ProductId == ProductA).Quantity);
        Assert.Equal(Usd(35m), cart.TotalPrice);
    }

    [Fact]
    public void RemoveItem_ExistingProduct_RemovesItemAndRecalculatesTotal()
    {
        ShoppingCart cart = CreateCartWithTwoItems();
        DateTime later = UtcNow.AddMinutes(5);

        Result result = cart.RemoveItem(ProductB, later);

        Assert.True(result.IsSuccess);
        CartItem remaining = Assert.Single(cart.CartItems);
        Assert.Equal(ProductA, remaining.ProductId);
        Assert.Equal(Usd(20m), cart.TotalPrice);
        Assert.Equal(later, cart.UpdatedOnUtc);
    }

    [Fact]
    public void RemoveItem_LastItem_TotalIsZero()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, UtcNow);

        Result result = cart.RemoveItem(ProductA, UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Empty(cart.CartItems);
        Assert.Equal(0m, cart.TotalPrice.Amount);
        Assert.True(cart.TotalPrice.IsZero());
    }

    [Fact]
    public void RemoveItem_UnknownProduct_ReturnsItemNotFound()
    {
        ShoppingCart cart = CreateCartWithTwoItems();

        Result result = cart.RemoveItem(99, UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound(99), result.Error);
        Assert.Equal(2, cart.CartItems.Count);
        Assert.Equal(Usd(35m), cart.TotalPrice);
    }

    [Fact]
    public void LineTotal_IsPriceTimesQuantity()
    {
        var item = CartItem.Create(Guid.NewGuid(), ProductA, "Keyboard", Usd(2.5m), 4, UtcNow);

        Assert.Equal(Usd(10m), item.LineTotal);
    }

    private static Money Usd(decimal amount) => new(amount, Currency.Usd);

    // Keyboard 2 x 10 + Mouse 3 x 5 = 35 USD
    private static ShoppingCart CreateCartWithTwoItems()
    {
        var cart = ShoppingCart.Create(UserId, CreatedOnUtc);
        cart.AddItem(ProductA, "Keyboard", Usd(10m), 2, UtcNow);
        cart.AddItem(ProductB, "Mouse", Usd(5m), 3, UtcNow);

        return cart;
    }
}
