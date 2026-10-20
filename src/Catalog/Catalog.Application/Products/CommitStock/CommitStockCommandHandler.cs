using Catalog.Application.Abstractions.Clock;
using Catalog.Domain.Products;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Catalog.Application.Products.CommitStock;

/// <summary>
/// The order is paid, so its reserved stock is sold for good: commit the reservations so they can no longer be released.
/// </summary>
internal sealed class CommitStockCommandHandler(
    IStockReservationRepository stockReservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CommitStockCommand>
{
    public async Task<Result> Handle(CommitStockCommand request, CancellationToken cancellationToken)
    {
        var activeReservations = (await stockReservationRepository.GetByOrderIdAsync(request.OrderId, cancellationToken))
            .Where(reservation => reservation.IsActive)
            .ToList();

        if (activeReservations.Count == 0)
        {
            return Result.Success();
        }

        DateTime utcNow = dateTimeProvider.UtcNow;

        foreach (StockReservation reservation in activeReservations)
        {
            reservation.Commit(utcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
