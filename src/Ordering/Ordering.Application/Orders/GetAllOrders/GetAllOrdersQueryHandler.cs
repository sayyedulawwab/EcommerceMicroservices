using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.Orders.GetAllOrders;
internal sealed class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await _orderRepository.GetAllAsync(cancellationToken);

        IEnumerable<OrderResponse> ordersResponse = orders.Select(order => new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                ProductPriceAmount = oi.Price.Amount,
                ProductPriceCurrency = oi.Price.Currency.Code,
                Quantity = oi.Quantity,
                CreatedOnUtc = oi.CreatedOnUtc
            }).ToList(),
            TotalPriceAmount = order.TotalPrice.Amount,
            TotalPriceCurrency = order.TotalPrice.Currency.Code,
            Status = order.Status.ToString(),
            CreatedOnUtc = order.CreatedOnUtc
        });

        return ordersResponse.ToList();
    }
}
