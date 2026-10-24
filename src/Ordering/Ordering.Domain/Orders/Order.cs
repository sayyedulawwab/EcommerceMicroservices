using SharedKernel.Domain;

namespace Ordering.Domain.Orders;

/// <summary>
/// Placed → StockConfirmed → Paid → Shipped → Delivered.
/// Placed and StockConfirmed orders can be Cancelled (by the customer, a stock rejection or a failed payment).
/// </summary>
public sealed class Order : Entity<OrderId>
{
    public const int MaxCancellationReasonLength = 500;

    private readonly List<OrderItem> _orderItems = [];

    private Order(OrderId id, Guid userId, DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        TotalPrice = Money.Zero();
        Status = OrderStatus.Placed;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public IReadOnlyList<OrderItem> OrderItems => _orderItems;

    public bool CanBeCancelled => Status is OrderStatus.Placed or OrderStatus.StockConfirmed;

    public static Result<Order> Place(Guid userId, IReadOnlyCollection<OrderLine> lines, DateTime createdOnUtc)
    {
        if (lines.Count == 0)
        {
            return Result.Failure<Order>(OrderErrors.Empty);
        }

        OrderLine? invalidLine = lines.FirstOrDefault(line => line.Quantity <= 0);

        if (invalidLine is not null)
        {
            return Result.Failure<Order>(OrderErrors.InvalidQuantity(invalidLine.ProductId));
        }

        var order = new Order(OrderId.New(), userId, createdOnUtc);

        // The same product listed twice becomes one item with the combined quantity.
        foreach (IGrouping<Guid, OrderLine> productLines in lines.GroupBy(line => line.ProductId))
        {
            order._orderItems.Add(OrderItem.Create(order.Id, productLines.Key, productLines.Sum(line => line.Quantity), createdOnUtc));
        }

        return order;
    }

    public Result ConfirmStock(IReadOnlyCollection<PricedOrderLine> pricedLines, DateTime utcNow)
    {
        if (Status != OrderStatus.Placed)
        {
            return Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.StockConfirmed));
        }

        var pricesByProduct = pricedLines
            .GroupBy(line => line.ProductId)
            .ToDictionary(group => group.Key, group => group.First());

        OrderItem? unpricedItem = OrderItems.FirstOrDefault(item => !pricesByProduct.ContainsKey(item.ProductId));

        if (unpricedItem is not null)
        {
            return Result.Failure(OrderErrors.MissingPrice(unpricedItem.ProductId));
        }

        if (OrderItems.Select(item => pricesByProduct[item.ProductId].Price.Currency).Distinct().Count() > 1)
        {
            return Result.Failure(OrderErrors.MixedCurrencies);
        }

        // Sum on a copy first so a failure leaves the order untouched.
        var total = Money.Zero(pricesByProduct[OrderItems[0].ProductId].Price.Currency);

        foreach (OrderItem item in OrderItems)
        {
            PricedOrderLine pricedLine = pricesByProduct[item.ProductId];

            Result<Money> sum = total.Add(new Money(pricedLine.Price.Amount * item.Quantity, pricedLine.Price.Currency));

            if (sum.IsFailure)
            {
                return Result.Failure(OrderErrors.MixedCurrencies);
            }

            total = sum.Value;
        }

        foreach (OrderItem item in OrderItems)
        {
            PricedOrderLine pricedLine = pricesByProduct[item.ProductId];

            item.SetPrice(pricedLine.ProductName, pricedLine.Price);
        }

        TotalPrice = total;

        return MoveTo(OrderStatus.StockConfirmed, utcNow);
    }

    public Result MarkPaid(DateTime utcNow)
    {
        return Status == OrderStatus.StockConfirmed
            ? MoveTo(OrderStatus.Paid, utcNow)
            : Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Paid));
    }

    public Result Ship(DateTime utcNow)
    {
        return Status == OrderStatus.Paid
            ? MoveTo(OrderStatus.Shipped, utcNow)
            : Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Shipped));
    }

    public Result Deliver(DateTime utcNow)
    {
        return Status == OrderStatus.Shipped
            ? MoveTo(OrderStatus.Delivered, utcNow)
            : Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Delivered));
    }

    public Result Cancel(string reason, DateTime utcNow)
    {
        if (!CanBeCancelled)
        {
            return Result.Failure(OrderErrors.CannotCancel(Status));
        }

        CancellationReason = reason.Length > MaxCancellationReasonLength
            ? reason[..MaxCancellationReasonLength]
            : reason;

        return MoveTo(OrderStatus.Cancelled, utcNow);
    }

    private Result MoveTo(OrderStatus status, DateTime utcNow)
    {
        Status = status;
        UpdatedOnUtc = utcNow;

        return Result.Success();
    }
}
