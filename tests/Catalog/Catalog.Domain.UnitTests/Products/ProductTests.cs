using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Domain.UnitTests.Products;

public sealed class ProductTests
{
    private static readonly DateTime CreatedOn = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Product CreateProduct(int quantity) =>
        Product.Create("Keyboard", "Mechanical keyboard", new Money(50m, Currency.Usd), quantity, CategoryId.New(), CreatedOn);

    [Fact]
    public void ReserveStock_EnoughStock_DecrementsQuantity()
    {
        Product product = CreateProduct(10);

        Result result = product.ReserveStock(4);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, product.Quantity);
    }

    [Fact]
    public void ReserveStock_ExactlyAllStock_LeavesZero()
    {
        Product product = CreateProduct(3);

        Result result = product.ReserveStock(3);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, product.Quantity);
    }

    [Fact]
    public void ReserveStock_InsufficientStock_ReturnsInsufficientStockAndKeepsQuantity()
    {
        Product product = CreateProduct(2);

        Result result = product.ReserveStock(3);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock(product.Id.Value).Name, result.Error.Name);
        Assert.Equal(2, product.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ReserveStock_NonPositiveQuantity_ReturnsInvalidQuantityAndKeepsQuantity(int quantity)
    {
        Product product = CreateProduct(5);

        Result result = product.ReserveStock(quantity);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InvalidQuantity(product.Id.Value).Name, result.Error.Name);
        Assert.Equal(5, product.Quantity);
    }

    [Fact]
    public void ReleaseStock_PositiveQuantity_IncrementsQuantity()
    {
        Product product = CreateProduct(5);

        product.ReleaseStock(3);

        Assert.Equal(8, product.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void ReleaseStock_NonPositiveQuantity_KeepsQuantity(int quantity)
    {
        Product product = CreateProduct(5);

        product.ReleaseStock(quantity);

        Assert.Equal(5, product.Quantity);
    }

    [Fact]
    public void ReserveThenRelease_SameQuantity_RestoresOriginalQuantity()
    {
        Product product = CreateProduct(7);

        Result result = product.ReserveStock(5);
        product.ReleaseStock(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, product.Quantity);
    }

    [Theory]
    [InlineData(5, 1, true)]
    [InlineData(5, 5, true)]
    [InlineData(5, 6, false)]
    [InlineData(5, 0, false)]
    [InlineData(5, -1, false)]
    [InlineData(0, 1, false)]
    public void HasStock_ReturnsExpected(int stock, int requested, bool expected)
    {
        Product product = CreateProduct(stock);

        bool hasStock = product.HasStock(requested);

        Assert.Equal(expected, hasStock);
    }
}
