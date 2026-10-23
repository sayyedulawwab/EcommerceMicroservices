using EcommerceMicroservices.IntegrationTests.Infrastructure;
using MassTransit;
using Ordering.Domain.Orders;
using SharedKernel.Events;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>
/// Ordering reacting to Catalog's stock answers, driven by publishing the integration events on the broker.
/// Orders are seeded, so Catalog is not involved; Payment still answers a stock confirmation, as it would in production.
/// </summary>
[Collection(SystemCollection.Name)]
public sealed class StockEventsTests(SystemFixture system)
{
    private const string UsdKeyboard = "Keyboard";

    [Fact]
    public async Task StockConfirmed_PlacedOrder_TakesNamesAndPricesFromCatalog_AndAsksForPayment()
    {
        Guid userId = TestJwt.NewUserId();
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.Placed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent> confirmed =
            await BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(orderId, UsdPricedItems()), TestContext.Current.CancellationToken);

        OrderStatusChangedToStockConfirmedIntegrationEvent @event = await confirmed.WaitForAsync(e => e.OrderId == orderId, "StockConfirmed to be published");
        Assert.Equal(userId, @event.UserId);
        Assert.Equal(35m, @event.TotalAmount);
        Assert.Equal("USD", @event.Currency);

        // Payment succeeds for the small total, so the order ends up paid.
        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Paid");
        Assert.Equal(35m, order.GetProperty("totalPriceAmount").GetDecimal());
        JsonElement keyboard = Assert.Single(
            order.GetProperty("orderItems").EnumerateArray(),
            item => item.GetProperty("productId").GetGuid() == OrderingTestData.KeyboardId);
        Assert.Equal(UsdKeyboard, keyboard.GetProperty("productName").GetString());
        Assert.Equal(10m, keyboard.GetProperty("productPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task StockConfirmed_ProductPricedTwice_UsesTheFirstPrice()
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Placed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        List<PricedOrderItem> items = [.. UsdPricedItems(), new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 999m, "USD", 2)];

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(orderId, items), TestContext.Current.CancellationToken);

        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Paid");
        Assert.Equal(35m, order.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task StockConfirmed_OrderCancelledMeanwhile_PublishesOrderCancelledSoTheStockIsReleased()
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Cancelled);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));
        await using BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent> confirmed =
            await BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(orderId, UsdPricedItems()), TestContext.Current.CancellationToken);

        OrderCancelledIntegrationEvent @event = await cancelled.WaitForAsync(e => e.OrderId == orderId, "OrderCancelled to be published");
        Assert.Equal(OrderingTestData.SeededCancellationReason, @event.Reason);
        Assert.Empty(await confirmed.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal("Cancelled", await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Theory]
    [InlineData(OrderStatus.StockConfirmed)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task StockConfirmed_DuplicateDelivery_ChangesNothing_AndPublishesNothing(OrderStatus status)
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));
        await using BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent> confirmed =
            await BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));
        List<PricedOrderItem> differentPrices =
        [
            new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 99m, "USD", 2),
            new PricedOrderItem(OrderingTestData.MouseId, "Mouse", 99m, "USD", 3)
        ];

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(orderId, differentPrices), TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.DoesNotContain(confirmed.Messages, e => e.OrderId == orderId);
        JsonElement order = await OrderingApi.GetOrderAsync(admin, orderId);
        Assert.Equal(status.ToString(), order.GetProperty("status").GetString());
        Assert.Equal(35m, order.GetProperty("totalPriceAmount").GetDecimal());
    }

    [Fact]
    public async Task StockConfirmed_PriceMissingForAProduct_CancelsTheOrderAndReleasesTheStock()
    {
        List<PricedOrderItem> onlyKeyboard = [new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 10m, "USD", 2)];

        await AssertCancelledAsInvalidAsync(onlyKeyboard);
    }

    [Fact]
    public async Task StockConfirmed_MixedCurrencies_CancelsTheOrderAndReleasesTheStock()
    {
        List<PricedOrderItem> mixed =
        [
            new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 10m, "USD", 2),
            new PricedOrderItem(OrderingTestData.MouseId, "Mouse", 500m, "BDT", 3)
        ];

        await AssertCancelledAsInvalidAsync(mixed);
    }

    [Fact]
    public async Task StockConfirmed_UnsupportedCurrency_CancelsTheOrderInsteadOfFailing()
    {
        List<PricedOrderItem> euros =
        [
            new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 10m, "EUR", 2),
            new PricedOrderItem(OrderingTestData.MouseId, "Mouse", 5m, "EUR", 3)
        ];

        await AssertCancelledAsInvalidAsync(euros);
    }

    [Fact]
    public async Task StockConfirmed_UnknownOrder_IsIgnored()
    {
        var missingOrderId = Guid.CreateVersion7();
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));
        await using BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent> confirmed =
            await BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(missingOrderId, UsdPricedItems()), TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == missingOrderId));
        Assert.DoesNotContain(confirmed.Messages, e => e.OrderId == missingOrderId);
    }

    [Fact]
    public async Task StockRejected_PlacedOrder_CancelsItWithTheReason_AndPublishesOrderCancelled()
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Placed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(
            new OrderItemsStockRejectedIntegrationEvent(orderId, "Not enough stock", [OrderingTestData.KeyboardId]),
            TestContext.Current.CancellationToken);

        OrderCancelledIntegrationEvent @event = await cancelled.WaitForAsync(e => e.OrderId == orderId, "OrderCancelled to be published");
        Assert.Equal("Stock rejected: Not enough stock", @event.Reason);
        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Cancelled");
        Assert.Equal("Stock rejected: Not enough stock", order.GetProperty("cancellationReason").GetString());
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task StockRejected_OrderPastCancellation_ChangesNothing(OrderStatus status)
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(
            new OrderItemsStockRejectedIntegrationEvent(orderId, "Not enough stock", [OrderingTestData.KeyboardId]),
            TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task StockRejected_UnknownOrder_IsIgnored()
    {
        var missingOrderId = Guid.CreateVersion7();
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(
            new OrderItemsStockRejectedIntegrationEvent(missingOrderId, "Not enough stock", []),
            TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == missingOrderId));
    }

    private async Task AssertCancelledAsInvalidAsync(List<PricedOrderItem> items)
    {
        Guid orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Placed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));
        await using BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent> confirmed =
            await BusProbe<OrderStatusChangedToStockConfirmedIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderItemsStockConfirmedIntegrationEvent(orderId, items), TestContext.Current.CancellationToken);

        OrderCancelledIntegrationEvent @event = await cancelled.WaitForAsync(e => e.OrderId == orderId, "OrderCancelled to be published");
        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Cancelled");
        string reason = order.GetProperty("cancellationReason").GetString()!;
        Assert.StartsWith("Stock confirmation was invalid", reason, StringComparison.Ordinal);
        Assert.Equal(reason, @event.Reason);
        Assert.DoesNotContain(confirmed.Messages, e => e.OrderId == orderId);
    }

    // 2 keyboards at 10 USD and 3 mice at 5 USD: total 35 USD.
    private static List<PricedOrderItem> UsdPricedItems()
    {
        return
        [
            new PricedOrderItem(OrderingTestData.KeyboardId, UsdKeyboard, 10m, "USD", 2),
            new PricedOrderItem(OrderingTestData.MouseId, "Mouse", 5m, "USD", 3)
        ];
    }
}
