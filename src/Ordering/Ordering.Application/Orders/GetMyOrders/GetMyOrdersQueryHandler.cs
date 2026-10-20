using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetMyOrders;

internal sealed class GetMyOrdersQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetMyOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await orderRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        return orders.Select(OrderResponse.FromOrder).ToList();
    }
}
