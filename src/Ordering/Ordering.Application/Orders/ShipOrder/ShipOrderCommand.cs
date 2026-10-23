using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ShipOrder;

public record ShipOrderCommand(Guid OrderId) : ICommand;
