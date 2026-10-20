using Ordering.Domain.Orders;

namespace Ordering.Application.Orders;

public sealed class OrderResponse
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public List<OrderItemResponse> OrderItems { get; init; } = [];
    public decimal TotalPriceAmount { get; init; }
    public string TotalPriceCurrency { get; init; }
    public string Status { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }

    public static OrderResponse FromOrder(Order order)
    {
        return new OrderResponse
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
            CancellationReason = order.CancellationReason,
            CreatedOnUtc = order.CreatedOnUtc,
            UpdatedOnUtc = order.UpdatedOnUtc
        };
    }
}
