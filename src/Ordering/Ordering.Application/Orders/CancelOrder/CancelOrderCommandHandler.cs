using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.CancelOrder;

internal sealed class CancelOrderCommandHandler(
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<CancelOrderCommand>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == request.OrderId, cancellationToken);

        if (order is null || (request.RequestedByUserId is long userId && order.UserId != userId))
        {
            return Result.Failure(OrderErrors.NotFound());
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return Result.Success();
        }

        Result cancelResult = order.Cancel(request.Reason, dateTimeProvider.UtcNow);

        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        // Catalog releases whatever stock it reserved for the order (nothing, if it never reserved any).
        await publishEndpoint.Publish(new OrderCancelledIntegrationEvent(order.Id, order.CancellationReason ?? request.Reason), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
