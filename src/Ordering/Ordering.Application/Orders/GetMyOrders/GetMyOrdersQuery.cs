using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetMyOrders;

public record GetMyOrdersQuery(Guid UserId) : IQuery<IReadOnlyList<OrderResponse>>;
