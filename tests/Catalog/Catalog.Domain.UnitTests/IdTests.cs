using Catalog.Domain.Categories;
using Catalog.Domain.Products;

namespace Catalog.Domain.UnitTests;

public sealed class IdTests
{
    [Fact]
    public void CategoryId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, CategoryId.New().Value.Version);
    }

    [Fact]
    public void ProductId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, ProductId.New().Value.Version);
    }

    [Fact]
    public void StockReservationId_New_CreatesVersion7Guid()
    {
        Assert.Equal(7, StockReservationId.New().Value.Version);
    }

    [Fact]
    public void Create_AssignsVersion7IdsBeforeSaving()
    {
        var category = Category.Create("Electronics", "Electronic devices", null, DateTime.UtcNow);
        Product product = Product.Create("Keyboard", "Mechanical keyboard", new SharedKernel.Domain.Money(50m, SharedKernel.Domain.Currency.Usd), 1, category.Id, DateTime.UtcNow).Value;
        var reservation = StockReservation.Create(Guid.CreateVersion7(), product.Id, 1, DateTime.UtcNow);

        Assert.Equal(7, category.Id.Value.Version);
        Assert.Equal(7, product.Id.Value.Version);
        Assert.Equal(7, reservation.Id.Value.Version);
    }
}
