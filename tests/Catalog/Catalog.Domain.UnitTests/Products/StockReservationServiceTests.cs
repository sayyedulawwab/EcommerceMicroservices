using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Domain.UnitTests.Products;

public sealed class StockReservationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Product CreateProduct(int quantity) =>
        Product.Create("Keyboard", "Mechanical keyboard", new Money(50m, Currency.Usd), quantity, CategoryId.New(), UtcNow).Value;

    [Fact]
    public void Reserve_EnoughStock_RemovesStockAndCreatesReservations()
    {
        Product product = CreateProduct(10);
        var orderId = Guid.CreateVersion7();

        Result<List<StockReservation>> result = StockReservationService.Reserve(
            orderId,
            new Dictionary<ProductId, int> { [product.Id] = 4 },
            new Dictionary<ProductId, Product> { [product.Id] = product },
            UtcNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, product.Quantity);
        StockReservation reservation = Assert.Single(result.Value);
        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(product.Id, reservation.ProductId);
        Assert.Equal(4, reservation.Quantity);
        Assert.True(reservation.IsActive);
    }

    [Fact]
    public void Reserve_InsufficientStock_ReturnsFailure()
    {
        Product product = CreateProduct(1);

        Result<List<StockReservation>> result = StockReservationService.Reserve(
            Guid.CreateVersion7(),
            new Dictionary<ProductId, int> { [product.Id] = 2 },
            new Dictionary<ProductId, Product> { [product.Id] = product },
            UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.InsufficientStock(product.Id.Value), result.Error);
        Assert.Equal(1, product.Quantity);
    }

    [Fact]
    public void Reserve_LaterLineFails_LeavesEarlierProductsUnchanged()
    {
        Product keyboard = CreateProduct(10);
        Product mouse = CreateProduct(1);

        Result<List<StockReservation>> result = StockReservationService.Reserve(
            Guid.CreateVersion7(),
            new Dictionary<ProductId, int> { [keyboard.Id] = 4, [mouse.Id] = 2 },
            new Dictionary<ProductId, Product> { [keyboard.Id] = keyboard, [mouse.Id] = mouse },
            UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(10, keyboard.Quantity);
        Assert.Equal(1, mouse.Quantity);
    }

    [Fact]
    public void Release_ActiveReservation_ReturnsStockAndClosesReservation()
    {
        Product product = CreateProduct(10);
        var products = new Dictionary<ProductId, Product> { [product.Id] = product };
        List<StockReservation> reservations = StockReservationService.Reserve(
            Guid.CreateVersion7(),
            new Dictionary<ProductId, int> { [product.Id] = 4 },
            products,
            UtcNow).Value;

        StockReservationService.Release(reservations, products, UtcNow);

        Assert.Equal(10, product.Quantity);
        Assert.Equal(StockReservationStatus.Released, reservations[0].Status);
    }
}
