using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.ReleaseStock;

internal sealed class ReleaseStockCommandHandler(
    IProductRepository productRepository,
    IStockReservationRepository stockReservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReleaseStockCommand>
{
    public async Task<Result> Handle(ReleaseStockCommand request, CancellationToken cancellationToken)
    {
        var activeReservations = (await stockReservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken))
            .Where(reservation => reservation.IsActive)
            .ToList();

        // Nothing reserved (stock was rejected, or already released or committed): releasing again is a no-op.
        if (activeReservations.Count == 0)
        {
            return Result.Success();
        }

        IReadOnlyList<Product> products = await productRepository.GetByIdsAsync(
            activeReservations.Select(reservation => reservation.ProductId).ToList(),
            cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        DateTime utcNow = dateTimeProvider.UtcNow;

        foreach (StockReservation reservation in activeReservations)
        {
            if (productsById.TryGetValue(reservation.ProductId, out Product? product))
            {
                product.ReleaseStock(reservation.Quantity);
            }

            reservation.Release(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
