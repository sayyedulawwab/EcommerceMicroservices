using Cart.Application.Abstractions.Clock;
using Cart.Application.Carts;
using Cart.Application.Carts.AddCartItem;
using Cart.Domain.Carts;
using Moq;
using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Application.UnitTests.Carts;

public sealed class AddCartItemCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly AddCartItemCommandHandler _handler;

    public AddCartItemCommandHandlerTests()
    {
        _dateTimeProviderMock.Setup(p => p.UtcNow).Returns(CartTestData.UtcNow);
        _handler = new AddCartItemCommandHandler(_cartRepositoryMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingCart_CreatesCartAndSavesIt()
    {
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);
        var command = new AddCartItemCommand(CartTestData.UserId, CartTestData.ProductA, "Keyboard", 10m, "USD", 2);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(CartTestData.UserId, result.Value.UserId);
        Assert.Equal(CartTestData.UtcNow, result.Value.CreatedOnUtc);
        CartItemResponse item = Assert.Single(result.Value.CartItems);
        Assert.Equal(CartTestData.ProductA, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20m, result.Value.TotalPriceAmount);
        Assert.Equal("USD", result.Value.TotalPriceCurrency);

        _cartRepositoryMock.Verify(
            r => r.SaveAsync(
                It.Is<ShoppingCart>(c => c.UserId == CartTestData.UserId && c.CartItems.Count == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingCart_AddsToItAndSavesIt()
    {
        ShoppingCart cart = CartTestData.CreateCartWithTwoItems();
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var command = new AddCartItemCommand(CartTestData.UserId, CartTestData.ProductA, "Keyboard", 10m, "USD", 1);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(cart.Id, result.Value.Id);
        Assert.Equal(3, result.Value.CartItems.Single(item => item.ProductId == CartTestData.ProductA).Quantity);
        Assert.Equal(45m, result.Value.TotalPriceAmount);
        Assert.Equal(CartTestData.UtcNow, result.Value.UpdatedOnUtc);
        _cartRepositoryMock.Verify(r => r.SaveAsync(cart, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DifferentCurrencyThanCart_ReturnsMixedCurrenciesWithoutSaving()
    {
        ShoppingCart cart = CartTestData.CreateCartWithTwoItems();
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var command = new AddCartItemCommand(CartTestData.UserId, 3, "Monitor", 15000m, "BDT", 1);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.MixedCurrencies, result.Error);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidQuantity_ReturnsInvalidQuantityWithoutSaving()
    {
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);
        var command = new AddCartItemCommand(CartTestData.UserId, CartTestData.ProductA, "Keyboard", 10m, "USD", 0);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.InvalidQuantity(CartTestData.ProductA), result.Error);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
