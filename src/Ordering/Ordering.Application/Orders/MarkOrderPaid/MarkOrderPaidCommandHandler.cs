using MassTransit;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.MarkOrderPaid;

internal sealed class MarkOrderPaidCommandHandler(
    IApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<MarkOrderPaidCommand>
{
    public async Task<Result> HandleAsync(MarkOrderPaidCommand request, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders
            .FirstOrDefaultAsync(order => order.Id == new OrderId(request.OrderId), cancellationToken);

        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound());
        }

        if (order.Status == OrderStatus.Paid)
        {
            return Result.Success();
        }

        Result paidResult = order.MarkPaid(timeProvider.GetUtcNow().UtcDateTime);

        if (paidResult.IsFailure)
        {
            return paidResult;
        }

        await publishEndpoint.Publish(new OrderStatusChangedToPaidIntegrationEvent(order.UserId, order.Id.Value), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
