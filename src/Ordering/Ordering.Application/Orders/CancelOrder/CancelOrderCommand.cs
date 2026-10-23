using SharedKernel.Messaging;

namespace Ordering.Application.Orders.CancelOrder;

/// <param name="RequestedByUserId">Set when a customer cancels; the order must then belong to them. Null for system cancellations.</param>
public record CancelOrderCommand(Guid OrderId, string Reason, Guid? RequestedByUserId = null) : ICommand;
