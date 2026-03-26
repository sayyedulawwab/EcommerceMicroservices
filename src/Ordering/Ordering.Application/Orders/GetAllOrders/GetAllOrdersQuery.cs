using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetAllOrders;
public record GetAllOrdersQuery() : IQuery<IReadOnlyList<OrderResponse>>;
