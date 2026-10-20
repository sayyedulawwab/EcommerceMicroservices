using Cart.Application.Carts;
using Cart.Application.Carts.GetCartByUser;
using Cart.Domain.Carts;
using Moq;
using SharedKernel.Domain;
using ShoppingCart = Cart.Domain.Carts.Cart;

namespace Cart.Application.UnitTests.Carts;

public sealed class GetCartByUserQueryHandlerTests
{
    private readonly Mock<ICartRepository> _cartRepositoryMock = new();

    [Fact]
    public async Task Handle_NoStoredCart_ReturnsEmptyCart()
    {
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart?)null);
        var handler = new GetCartByUserQueryHandler(_cartRepositoryMock.Object);

        Result<CartResponse> result = await handler.Handle(new GetCartByUserQuery(CartTestData.UserId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Empty, result.Value.Id);
        Assert.Equal(CartTestData.UserId, result.Value.UserId);
        Assert.Empty(result.Value.CartItems);
        Assert.Equal(0m, result.Value.TotalPriceAmount);
    }

    [Fact]
    public async Task Handle_StoredCart_ReturnsMappedCart()
    {
        ShoppingCart cart = CartTestData.CreateCartWithTwoItems();
        _cartRepositoryMock
            .Setup(r => r.GetByUserId(CartTestData.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);
        var handler = new GetCartByUserQueryHandler(_cartRepositoryMock.Object);

        Result<CartResponse> result = await handler.Handle(new GetCartByUserQuery(CartTestData.UserId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(cart.Id, result.Value.Id);
        Assert.Equal(CartTestData.UserId, result.Value.UserId);
        Assert.Equal(2, result.Value.CartItems.Count);
        Assert.Equal(35m, result.Value.TotalPriceAmount);
        Assert.Equal("USD", result.Value.TotalPriceCurrency);

        CartItemResponse keyboard = result.Value.CartItems.Single(item => item.ProductId == CartTestData.ProductA);
        Assert.Equal("Keyboard", keyboard.ProductName);
        Assert.Equal(10m, keyboard.ProductPriceAmount);
        Assert.Equal("USD", keyboard.ProductPriceCurrency);
        Assert.Equal(2, keyboard.Quantity);
    }
}
