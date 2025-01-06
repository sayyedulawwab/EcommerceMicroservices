using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.Orders.PlaceOrder;
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

        var order = Order.Create(request.UserId, OrderStatus.Placed, _dateTimeProvider.UtcNow);


        foreach (OrderItemCommand item in request.OrderItems)
        {
            order.AddOrderItem(order.Id,
                item.ProductId,
                item.ProductName,
                new Money(item.PriceAmount, Currency.Create(item.PriceCurrency)),
                item.Quantity,
                _dateTimeProvider.UtcNow);
        }

        _orderRepository.Add(order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // publish event

        var orderStockItems = request.OrderItems.Select(orderItem => new OrderStockItem(orderItem.ProductId,
                                                                                            orderItem.ProductName,
                                                                                            orderItem.PriceAmount,
                                                                                            orderItem.PriceCurrency,
                                                                                            orderItem.Quantity)
                                                       ).ToList();


        var integrationEvent = new OrderPlacedIntegrationEvent(order.Id, orderStockItems);

        await _messageSession.Publish(integrationEvent, cancellationToken);

        return order.Id;
    }
}
