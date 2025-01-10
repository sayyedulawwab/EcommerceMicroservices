using MassTransit;
using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class UpdateOrderStatusToStockConfirmedCommandHandler(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IPublishEndpoint publishEndpoint)
    : ICommandHandler<UpdateOrderStatusToStockConfirmedCommand, long>
{
    public async Task<Result<long>> Handle(UpdateOrderStatusToStockConfirmedCommand request, CancellationToken cancellationToken)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<long>(OrderErrors.NotFound());
        }

        var updatedOrder = Order.Update(order, order.UserId, order.TotalPrice, OrderStatus.StockConfirmed, dateTimeProvider.UtcNow);

        orderRepository.Update(updatedOrder);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var integrationEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(order.Id);

        await publishEndpoint.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}