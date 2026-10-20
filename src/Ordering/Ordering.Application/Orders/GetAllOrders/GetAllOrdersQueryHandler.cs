using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetAllOrders;

internal sealed class GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await orderRepository.GetAllAsync(cancellationToken);

        return orders.Select(OrderResponse.FromOrder).ToList();
    }
}
