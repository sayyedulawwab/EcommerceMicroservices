using System.Reflection;
using Catalog.Domain.Categories;
using Catalog.Domain.Products;
using SharedKernel.Domain;

namespace Catalog.Application.UnitTests;

internal static class TestEntities
{
    public static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly PropertyInfo IdProperty =
        typeof(Entity<long>).GetProperty(nameof(Entity<>.Id))
        ?? throw new InvalidOperationException("Entity<long>.Id not found");

    public static T WithId<T>(T entity, long id)
        where T : Entity<long>
    {
        IdProperty.SetValue(entity, id);

        return entity;
    }

    public static Product NewProduct(
        long id,
        int quantity,
        decimal price = 10m,
        Currency? currency = null,
        string? name = null,
        string description = "A product",
        long categoryId = 1) =>
        WithId(
            Product.Create(
                name ?? $"Product {id}",
                description,
                new Money(price, currency ?? Currency.Usd),
                quantity,
                categoryId,
                Now),
            id);

    public static Category NewCategory(long id, long? parentCategoryId = null) =>
        WithId(Category.Create($"Category {id}", "A category", parentCategoryId, Now), id);

    public static StockReservation NewReservation(long orderId, long productId, int quantity) =>
        StockReservation.Create(orderId, productId, quantity, Now);
}
