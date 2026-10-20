using Cart.Application.Abstractions.Clock;
using Cart.Application.Carts;
using Cart.Application.Carts.RemoveCartItem;
using Cart.Domain.Carts;
using Moq;
using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Application.UnitTests.Carts;

public sealed class RemoveCartItemCommandHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly RemoveCartItemCommandHandler _handler;

    public RemoveCartItemCommandHandlerTests()
    {
        _dateTimeProviderMock.Setup(p => p.UtcNow).Returns(CartTestData.UtcNow);
        _handler = new RemoveCartItemCommandHandler(_cartRepositoryMock.Object, _dateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_NoCart_ReturnsItemNotFound()
    {
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);
        var command = new RemoveCartItemCommand(CartTestData.UserId, CartTestData.ProductA);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound(CartTestData.ProductA), result.Error);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingItem_RemovesItAndSaves()
    {
        ShoppingCart cart = CartTestData.CreateCartWithTwoItems();
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var command = new RemoveCartItemCommand(CartTestData.UserId, CartTestData.ProductB);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        CartItemResponse remaining = Assert.Single(result.Value.CartItems);
        Assert.Equal(CartTestData.ProductA, remaining.ProductId);
        Assert.Equal(20m, result.Value.TotalPriceAmount);
        Assert.Equal(CartTestData.UtcNow, result.Value.UpdatedOnUtc);
        _cartRepositoryMock.Verify(r => r.SaveAsync(cart, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsItemNotFoundWithoutSaving()
    {
        ShoppingCart cart = CartTestData.CreateCartWithTwoItems();
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var command = new RemoveCartItemCommand(CartTestData.UserId, 99);

        Result<CartResponse> result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(CartErrors.ItemNotFound(99), result.Error);
        _cartRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ShoppingCart>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
