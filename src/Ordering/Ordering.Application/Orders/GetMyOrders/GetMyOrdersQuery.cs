using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetMyOrders;

public record GetMyOrdersQuery(long UserId) : IQuery<IReadOnlyList<OrderResponse>>;
