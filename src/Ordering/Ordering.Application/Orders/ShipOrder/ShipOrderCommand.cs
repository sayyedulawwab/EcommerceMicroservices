using SharedKernel.Messaging;

namespace Ordering.Application.Orders.ShipOrder;

public record ShipOrderCommand(long OrderId) : ICommand;
