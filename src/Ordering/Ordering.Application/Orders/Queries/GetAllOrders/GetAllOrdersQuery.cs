using Ordering.Application.Abstractions.Messaging;

namespace Ordering.Application.Orders.Queries.GetAllOrders;
public record GetAllOrdersQuery() : IQuery<IReadOnlyList<OrderResponse>>;
