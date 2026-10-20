namespace Catalog.Domain.Products;

public interface IStockReservationRepository
{
    Task<IReadOnlyList<StockReservation>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
    void Add(StockReservation reservation);
}
