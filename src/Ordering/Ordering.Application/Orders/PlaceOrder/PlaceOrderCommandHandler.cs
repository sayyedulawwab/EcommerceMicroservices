using Ordering.Application.Abstractions.Clock;
using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Abstractions;
using Ordering.Domain.Orders;
using Ordering.Domain.Shared;

namespace Ordering.Application.Orders.PlaceOrder;
internal sealed class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, long>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PlaceOrderCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<long>> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {

        var orderItems = new List<OrderItem>();

        var order = Order.PlaceOrder(request.userId, orderItems, OrderStatus.Placed, _dateTimeProvider.UtcNow);

        _orderRepository.Add(order);

        await _unitOfWork.SaveChangesAsync();


        orderItems = request.orderItems
            .Select(item => OrderItem.Create(
                order.Id,  
                item.productId,
                item.productName,
                new Money(item.priceAmount, Currency.Create(item.priceCurrency)),
                item.quantity,
                _dateTimeProvider.UtcNow
            ))
            .ToList();

        // Add the order items to the Order
        order.AddOrderItems(orderItems);

        await _unitOfWork.SaveChangesAsync();

        return order.Id;
    }
}
