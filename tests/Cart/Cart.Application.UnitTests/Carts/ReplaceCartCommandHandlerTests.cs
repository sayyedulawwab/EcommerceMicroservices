using Cart.Application.Abstractions.Clock;
using Cart.Application.Carts;
using Cart.Application.Carts.ReplaceCart;
using Cart.Domain.Carts;
using Moq;
using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Application.UnitTests.Carts;

public sealed class ReplaceCartCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly ReplaceCartCommandHandler _handler;

    public ReplaceCartCommandHandlerTests()
    {
        _dateTimeProviderMock.Setup(p => p.UtcNow).Returns(CartTestData.UtcNow);
        _handler = new ReplaceCartCommandHandler(_cartRepositoryMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyItemList_RemovesCartAndReturnsEmptyCart()
    {
        var command = new ReplaceCartCommand(CartTestData.UserId, []);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value.Id);
        Assert.Equal(CartTestData.UserId, result.Value.UserId);
        Assert.Empty(result.Value.CartItems);
        Assert.Equal(0m, result.Value.TotalPriceAmount);
        _cartRepositoryMock.Verify(r => r.RemoveAsync(CartTestData.UserId, It.IsAny<CancellationToken>()), Times.Once);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Items_SavesNewCartWithMergedItems()
    {
        ShoppingCart? savedCart = null;
        _cartRepositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()))
            .Callback<ShoppingCart, CancellationToken>((cart, _) => savedCart = cart)
            .Returns(Task.CompletedTask);
        var command = new ReplaceCartCommand(
            CartTestData.UserId,
            [
                new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "USD", 2),
                new CartItemCommand(CartTestData.ProductB, "Mouse", 5m, "USD", 3),
                new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "USD", 1),
            ]);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedCart);
        Assert.Equal(CartTestData.UserId, savedCart.UserId);
        Assert.Equal(CartTestData.UtcNow, savedCart.CreatedOnUtc);
        Assert.Equal(2, savedCart.CartItems.Count);
        Assert.Equal(3, savedCart.CartItems.Single(item => item.ProductId == CartTestData.ProductA).Quantity);
        Assert.Equal(3, savedCart.CartItems.Single(item => item.ProductId == CartTestData.ProductB).Quantity);
        Assert.Equal(45m, savedCart.TotalPrice.Amount);

        Assert.Equal(savedCart.Id, result.Value.Id);
        Assert.Equal(45m, result.Value.TotalPriceAmount);
        Assert.Equal("USD", result.Value.TotalPriceCurrency);
        _cartRepositoryMock.Verify(r => r.RemoveAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MixedCurrencies_ReturnsMixedCurrenciesWithoutSaving()
    {
        var command = new ReplaceCartCommand(
            CartTestData.UserId,
            [
                new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "USD", 1),
                new CartItemCommand(CartTestData.ProductB, "Mouse", 500m, "BDT", 1),
            ]);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.MixedCurrencies, result.Error);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
