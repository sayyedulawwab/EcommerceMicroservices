using Moq;
using Ordering.Application.Orders.ConfirmStock;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.UnitTests.Orders;

public sealed class ConfirmOrderStockCommandHandlerTests : OrderHandlerTestBase
{
    [Fact]
    public async Task Handle_PlacedOrder_ConfirmsStockAndPublishesTotal()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, TestOrders.UsdPricedItems()), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.StockConfirmed, order.Status);
        Assert.Equal(new Money(35m, Currency.Usd), order.TotalPrice);
        Assert.Equal(TestOrders.UtcNow, order.UpdatedOnUtc);
        OrderStatusChangedToStockConfirmedIntegrationEvent confirmed =
            Assert.IsType<OrderStatusChangedToStockConfirmedIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(new OrderStatusChangedToStockConfirmedIntegrationEvent(TestOrders.OrderId, TestOrders.UserId, 35m, "USD"), confirmed);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Handle_PlacedOrder_TakesNamesAndPricesFromCatalog()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);

        await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, TestOrders.UsdPricedItems()), CancellationToken);

        OrderItem keyboard = Assert.Single(order.OrderItems, item => item.ProductId == TestOrders.KeyboardId);
        Assert.Equal("Keyboard", keyboard.ProductName);
        Assert.Equal(new Money(10m, Currency.Usd), keyboard.Price);
    }

    [Fact]
    public async Task Handle_OrderAlreadyCancelled_PublishesOrderCancelledSoStockIsReleased()
    {
        Order order = TestOrders.In(OrderStatus.Cancelled);
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, TestOrders.UsdPricedItems()), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        OrderCancelledIntegrationEvent cancelled = Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(new OrderCancelledIntegrationEvent(TestOrders.OrderId, TestOrders.CustomerCancelReason), cancelled);
        VerifySaved(Times.Once());
    }

    [Theory]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task Handle_OrderNotPlaced_TreatsAsDuplicateAndPublishesNothing(OrderStatus status)
    {
        Order order = TestOrders.In(status);
        Money totalBefore = order.TotalPrice;
        GivenOrder(order);

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, TestOrders.UsdPricedItems()), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(status, order.Status);
        Assert.Equal(totalBefore, order.TotalPrice);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Handle_PriceMissingForProduct_CancelsOrderPublishesCancelledAndFails()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);
        List<PricedOrderItem> items = [new PricedOrderItem(TestOrders.KeyboardId, "Keyboard", 10m, "USD", 2)];

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, items), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.MissingPrice(TestOrders.MouseId), result.Error);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancellationReason);
        Assert.StartsWith("Stock confirmation was invalid", order.CancellationReason, StringComparison.Ordinal);
        OrderCancelledIntegrationEvent cancelled = Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(TestOrders.OrderId, cancelled.OrderId);
        Assert.Equal(order.CancellationReason, cancelled.Reason);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Handle_MixedCurrencies_CancelsOrderPublishesCancelledAndFails()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);
        List<PricedOrderItem> items =
        [
            new PricedOrderItem(TestOrders.KeyboardId, "Keyboard", 10m, "USD", 2),
            new PricedOrderItem(TestOrders.MouseId, "Mouse", 500m, "BDT", 3),
        ];

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, items), CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.MixedCurrencies, result.Error);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
    }

    [Fact]
    public async Task Handle_UnsupportedCurrency_CancelsOrderInsteadOfThrowing()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);
        List<PricedOrderItem> items =
        [
            new PricedOrderItem(TestOrders.KeyboardId, "Keyboard", 10m, "EUR", 2),
            new PricedOrderItem(TestOrders.MouseId, "Mouse", 5m, "EUR", 3),
        ];

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, items), CancellationToken);

        Assert.Equal(OrderErrors.UnsupportedCurrency("EUR"), result.Error);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        OrderCancelledIntegrationEvent cancelled = Assert.IsType<OrderCancelledIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(order.CancellationReason, cancelled.Reason);
        VerifySaved(Times.Once());
    }

    [Fact]
    public async Task Handle_DuplicatePricedLines_UsesTheFirstPriceInsteadOfThrowing()
    {
        Order order = TestOrders.Placed();
        GivenOrder(order);
        List<PricedOrderItem> items = [.. TestOrders.UsdPricedItems(), new PricedOrderItem(TestOrders.KeyboardId, "Keyboard", 999m, "USD", 2)];

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, items), CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(35m, Currency.Usd), order.TotalPrice);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReturnsNotFound()
    {
        GivenNoOrder(TestOrders.OrderId);

        Result result = await CreateHandler().Handle(new ConfirmOrderStockCommand(TestOrders.OrderId, TestOrders.UsdPricedItems()), CancellationToken);

        AssertNotFound(result);
        Assert.Empty(PublishedMessages);
        VerifySaved(Times.Never());
    }

    private ConfirmOrderStockCommandHandler CreateHandler()
    {
        return new ConfirmOrderStockCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object, PublishEndpoint.Object);
    }
}
