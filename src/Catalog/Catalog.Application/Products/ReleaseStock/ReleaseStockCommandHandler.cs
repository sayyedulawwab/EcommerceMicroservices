using Catalog.Application.Abstractions.Clock;
using Catalog.Application.Abstractions.Data;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReleaseStock;

internal sealed class ReleaseStockCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReleaseStockCommand>
{
    public async Task<Result> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        List<StockReservation> activeReservations = await dbContext.StockReservations
            .Where(reservation => reservation.OrderId == request.OrderId && reservation.Status == StockReservationStatus.Reserved)
            .ToListAsync(cancellationToken);

        // Nothing reserved (stock was rejected, or already released or committed): releasing again is a no-op.
        if (activeReservations.Count == 0)
        {
            return Result.Success();
        }

        var productIds = activeReservations.Select(reservation => reservation.ProductId).ToList();

        Dictionary<ProductId, Product> productsById = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        DateTime utcNow = dateTimeProvider.UtcNow;

        foreach (StockReservation reservation in activeReservations)
        {
            if (productsById.TryGetValue(reservation.ProductId, out Product? product))
            {
                product.ReleaseStock(reservation.Quantity);
            }

            reservation.Release(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
