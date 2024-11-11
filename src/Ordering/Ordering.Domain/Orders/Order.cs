using Ordering.Domain.Abstractions;
using Ordering.Domain.Shared;
using System.Collections.Generic;

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


    public static Order Create(long userId, OrderStatus status, DateTime createdOnUtc)
    {
        var order = new Order(userId, Money.Zero(), status, createdOnUtc);

        return order;
    }


    public void AddOrderItem(long orderId, long productId, string productName, Money price, int quantity, DateTime createdOn)
    {

        var orderItem = OrderItem.Create(orderId, productId, productName, price, quantity, createdOn);
      
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

    public static Order Update(Order order, long userID, Money totalPrice, OrderStatus status, DateTime updatedOn)
    {

        order.UserId = userID;
        order.TotalPrice = totalPrice;
        order.Status = status;
        order.UpdatedOnUtc = updatedOn;

        return order;
    }


}
