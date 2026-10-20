using Ordering.Application.Abstractions.Clock;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.DeliverOrder;

internal sealed class DeliverOrderCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<DeliverOrderCommand>
{
    public async Task<Result> Handle(DeliverOrderCommand request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure(OrderErrors.NotFound());
        }

        Result result = order.Deliver(dateTimeProvider.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
