using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

internal sealed class StockReservationRepository(ApplicationDbContext dbContext) : Repository<StockReservation>(dbContext), IStockReservationRepository
{
    public async Task<IReadOnlyList<StockReservation>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<StockReservation>()
            .Where(reservation => reservation.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}
