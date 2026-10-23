namespace Catalog.Domain.Products;

public record StockReservationId(Guid Value)
{
    public static StockReservationId New() => new(Guid.CreateVersion7());
}
