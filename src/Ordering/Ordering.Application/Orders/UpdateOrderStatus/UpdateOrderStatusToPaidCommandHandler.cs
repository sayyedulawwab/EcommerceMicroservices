using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class UpdateOrderStatusToPaidCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IMessageSession messageSession)
    : ICommandHandler<UpdateOrderStatusToPaidCommand, long>
{
    public async Task<Result<long>> Handle(UpdateOrderStatusToPaidCommand request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<long>(OrderErrors.NotFound());
        }

        var updatedOrder = Order.Update(order, order.UserId, order.TotalPrice, OrderStatus.Paid, dateTimeProvider.UtcNow);

        orderRepository.Update(updatedOrder);

        await unitOfWork.SaveChangesAsync(cancellationToken);


        var orderStockItems = order.OrderItems.Select(orderItem => new OrderStockItem(
            orderItem.ProductId,
            orderItem.ProductName,
            orderItem.Price.Amount,
            orderItem.Price.Currency.Code,
            orderItem.Quantity)).ToList();

        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(order.UserId, order.Id, orderStockItems);

        await messageSession.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}