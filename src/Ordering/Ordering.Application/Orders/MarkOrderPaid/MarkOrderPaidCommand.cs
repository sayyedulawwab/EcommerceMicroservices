using SharedKernel.Messaging;

namespace Ordering.Application.Orders.MarkOrderPaid;

public record MarkOrderPaidCommand(Guid OrderId) : ICommand;
