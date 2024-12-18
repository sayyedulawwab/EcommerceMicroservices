using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.Commands.PlaceOrder;
internal sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, long>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMessageSession _messageSession;

    public PlaceOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider, IMessageSession messageSession)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _messageSession = messageSession;
    }

    public async Task<Result<long>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {

        var order = Order.Create(request.userId, OrderStatus.Placed, _dateTimeProvider.UtcNow);


        foreach (var item in request.orderItems)
        {
            order.AddOrderItem(order.Id,
                item.productId,
                item.productName,
                new Money(item.priceAmount, Currency.Create(item.priceCurrency)),
                item.quantity,
                _dateTimeProvider.UtcNow);
        }

        _orderRepository.Add(order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // publish event

        var orderStockItems = request.orderItems.Select(orderItem => new OrderStockItem(orderItem.productId,
                                                                                            orderItem.productName,
                                                                                            orderItem.priceAmount,
                                                                                            orderItem.priceCurrency,
                                                                                            orderItem.quantity)
                                                       ).ToList();


        var integrationEvent = new OrderPlacedIntegrationEvent(order.Id, orderStockItems);

        await _messageSession.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}
