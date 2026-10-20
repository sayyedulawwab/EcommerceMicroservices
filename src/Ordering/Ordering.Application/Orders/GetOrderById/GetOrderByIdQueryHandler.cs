using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetOrderById;

internal sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderByIdQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        // Someone else's order is reported as missing so order ids can't be probed.
        if (order is null || (!request.IsAdmin && order.UserId != request.RequestingUserId))
        {
            return Result.Failure<OrderResponse>(OrderErrors.NotFound());
        }

        return OrderResponse.FromOrder(order);
    }
}
