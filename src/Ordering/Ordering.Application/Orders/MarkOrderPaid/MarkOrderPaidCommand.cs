using SharedKernel.Messaging;

namespace Ordering.Application.Orders.MarkOrderPaid;

public record MarkOrderPaidCommand(long OrderId) : ICommand;
