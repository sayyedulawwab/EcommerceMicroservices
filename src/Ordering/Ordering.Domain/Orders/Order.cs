using SharedKernel.Domain;

namespace Ordering.Domain.Orders;
public sealed class Order : Entity<long>
{
    public Order(long userID, Money totalPrice, OrderStatus status, DateTime createdOnUtc)
    {
        UserId = userID;
        TotalPrice = totalPrice;
        Status = status;
        CreatedOnUtc = createdOnUtc;

    }

    private Order()
    {
    }

    public long UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public List<OrderItem> OrderItems { get; private set; } = [];


    public static Order Create(long userId, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(userId, Money.Zero(), status, createdOnUtc);

        return order;
    }


    public void AddOrderItem(long OrderId, long productId, string productName, Money price, int quantity, DateTime createdOnUtc)
    {

        var orderItem = OrderItem.Create(OrderId, productId, productName, price, quantity, createdOnUtc);

        OrderItems.Add(orderItem);

        if (TotalPrice.IsZero())
        {
            TotalPrice = new Money(orderItem.Price.Amount, orderItem.Price.Currency);
        }
        else
        {
            if (TotalPrice.Currency != orderItem.Price.Currency)
            {
                throw new InvalidOperationException("Currencies have to be equal");
            }

            TotalPrice += new Money(orderItem.Price.Amount * orderItem.Quantity, orderItem.Price.Currency);
        }
    }

    public static Order Update(Order order, long userID, Money totalPrice, OrderStatus status, DateTime updatedOnUtc)
    {

        order.UserId = userID;
        order.TotalPrice = totalPrice;
        order.Status = status;
        order.UpdatedOnUtc = updatedOnUtc;

        return order;
    }


}
