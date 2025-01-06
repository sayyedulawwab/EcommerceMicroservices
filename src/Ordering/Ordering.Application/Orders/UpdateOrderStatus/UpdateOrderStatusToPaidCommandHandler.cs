using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.UpdateOrderStatus;
internal sealed class UpdateOrderStatusToPaidCommandHandler : ICommandHandler<UpdateOrderStatusToPaidCommand, long>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageSession _messageSession;

    public UpdateOrderStatusToPaidCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _messageSession = messageSession;
    }

    public async Task<Result<long>> Handle(UpdateOrderStatusToPaidCommand request, CancellationToken cancellationToken)
    {

        Order? order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<long>(OrderErrors.NotFound());
        }

        var updatedOrder = Order.Update(order, order.UserId, order.TotalPrice, OrderStatus.Paid, _dateTimeProvider.UtcNow);

        _orderRepository.Update(updatedOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);


        var orderStockItems = order.OrderItems.Select(orderItem => new OrderStockItem(orderItem.ProductId,
                                                                                            orderItem.ProductName,
                                                                                            orderItem.Price.Amount,
                                                                                            orderItem.Price.Currency.Code,
                                                                                            orderItem.Quantity)
                                                       ).ToList();

        var integrationEvent = new OrderStatusChangedToPaidIntegrationEvent(order.UserId, order.Id, orderStockItems);

        await _messageSession.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}
