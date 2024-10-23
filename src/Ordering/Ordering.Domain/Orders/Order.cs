using Ordering.Domain.Abstractions;
using Ordering.Domain.Shared;

namespace Ordering.Domain.Orders;
public sealed class Order : Entity<long>
{
    public Order(long userID, Money totalPrice, OrderStatus status, DateTime createdOn)
    {
        UserId = userID;
        TotalPrice = totalPrice;
        Status = status;
        CreatedOnUtc = createdOn;
     
    }

    private Order()
    {
    }

    public long UserId { get; private set; }
    public Money TotalPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public List<OrderItem> OrderItems { get; private set; } = new List<OrderItem>();


    public static Order PlaceOrder(long userId, List<OrderItem> orderItems, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(userId, Money.Zero(), status, createdOnUtc);
        
        foreach (var orderItem in orderItems)
        {
            order.OrderItems.Add(orderItem);

            if (order.TotalPrice.IsZero())
            {
                order.TotalPrice = new Money(orderItem.Price.Amount, orderItem.Price.Currency); 
            }
            else
            {
                if (order.TotalPrice.Currency != orderItem.Price.Currency)
                {
                    throw new InvalidOperationException("Currencies have to be equal");
                }

                order.TotalPrice += new Money(orderItem.Price.Amount * orderItem.Quantity, orderItem.Price.Currency);
            }

            
        }

        return order;
    }


    public void AddOrderItems(List<OrderItem> orderItems)
    {
        OrderItems.AddRange(orderItems);

        foreach (var orderItem in orderItems)
        {
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
    }

    public static Order Update(Order order, long userID, Money totalPrice, OrderStatus status, DateTime updatedOn)
    {

        order.UserId = userID;
        order.TotalPrice = totalPrice;
        order.Status = status;
        order.UpdatedOnUtc = updatedOn;

        return order;
    }


}
