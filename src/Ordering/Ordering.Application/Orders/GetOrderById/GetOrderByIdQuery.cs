using SharedKernel.Messaging;

namespace Ordering.Application.Orders.GetOrderById;

/// <param name="RequestingUserId">The caller; non-admins only see their own orders.</param>
public record GetOrderByIdQuery(Guid OrderId, Guid RequestingUserId, bool IsAdmin) : IQuery<OrderResponse>;
