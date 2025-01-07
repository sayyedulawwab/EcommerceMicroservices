using Ordering.Application.Abstractions.Messaging;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.Orders.GetAllOrders;
internal sealed class GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetAllOrdersQuery, IReadOnlyList<OrderResponse>>
{
    public async Task<Result<IReadOnlyList<OrderResponse>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Order> orders = await orderRepository.GetAllAsync(cancellationToken);

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
