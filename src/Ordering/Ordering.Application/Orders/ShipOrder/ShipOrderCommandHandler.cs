using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ShipOrder;

internal sealed class ShipOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ShipOrderCommand>
{
    public async Task<Result> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == new OrderId(request.OrderId), cancellationToken);

        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound());
        }

        Result result = order.Ship(dateTimeProvider.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
