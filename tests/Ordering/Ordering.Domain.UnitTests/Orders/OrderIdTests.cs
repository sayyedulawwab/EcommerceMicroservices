using Ordering.Domain.Orders;

namespace Ordering.Domain.UnitTests.Orders;

public sealed class OrderIdTests
{
    [Fact]
    public void OrderId_New_CreatesVersion7Guid()
    {
        var id = OrderId.New();

        Assert.Equal(7, id.Value.Version);
    }

    [Fact]
    public void OrderItemId_New_CreatesVersion7Guid()
    {
        var id = OrderItemId.New();

        Assert.Equal(7, id.Value.Version);
    }

    [Fact]
    public void Place_AssignsVersion7IdsBeforeSaving()
    {
        Order order = OrderTestData.PlacedOrder();

        Assert.Equal(7, order.Id.Value.Version);
        Assert.All(order.OrderItems, item =>
        {
            Assert.Equal(7, item.Id.Value.Version);
            Assert.Equal(order.Id, item.OrderId);
        });
    }
}
