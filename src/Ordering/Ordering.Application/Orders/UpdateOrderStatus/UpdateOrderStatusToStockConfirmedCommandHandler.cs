using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class UpdateOrderStatusToStockConfirmedCommandHandler : ICommandHandler<UpdateOrderStatusToStockConfirmedCommand, long>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageSession _messageSession;

    public UpdateOrderStatusToStockConfirmedCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _messageSession = messageSession;
    }

    public async Task<Result<long>> Handle(UpdateOrderStatusToStockConfirmedCommand request, CancellationToken cancellationToken)
    {

        Order? order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<long>(OrderErrors.NotFound());
        }

        var updatedOrder = Order.Update(order, order.UserId, order.TotalPrice, OrderStatus.StockConfirmed, _dateTimeProvider.UtcNow);

        _orderRepository.Update(updatedOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);


        var integrationEvent = new OrderStatusChangedToStockConfirmedIntegrationEvent(order.Id);

        await _messageSession.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}
