using EcommerceMicroservices.IntegrationTests.Infrastructure;
using MassTransit;
using Ordering.Domain.Orders;
using SharedKernel.Events;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Ordering;

/// <summary>Ordering reacting to Payment's answers, driven by publishing the integration events on the broker.</summary>
[Collection(SystemCollection.Name)]
public sealed class PaymentEventsTests(SystemFixture system)
{
    [Fact]
    public async Task PaymentSucceeded_StockConfirmedOrder_MarksItPaid_AndPublishesOrderPaid()
    {
        long userId = TestJwt.NewUserId();
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, userId, OrderStatus.StockConfirmed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderStatusChangedToPaidIntegrationEvent> paid = await BusProbe<OrderStatusChangedToPaidIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentSucceededIntegrationEvent(orderId), TestContext.Current.CancellationToken);

        OrderStatusChangedToPaidIntegrationEvent @event = await paid.WaitForAsync(e => e.OrderId == orderId, "OrderPaid to be published");
        Assert.Equal(userId, @event.UserId);
        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Paid");
        Assert.NotEqual(JsonValueKind.Null, order.GetProperty("updatedOnUtc").ValueKind);
    }

    [Fact]
    public async Task PaymentSucceeded_AlreadyPaid_SucceedsWithoutPublishingAgain()
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.Paid);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderStatusChangedToPaidIntegrationEvent> paid = await BusProbe<OrderStatusChangedToPaidIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentSucceededIntegrationEvent(orderId), TestContext.Current.CancellationToken);

        Assert.Empty(await paid.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal("Paid", await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Shipped)]
    public async Task PaymentSucceeded_OrderNotAwaitingPayment_ChangesNothing_AndPublishesNothing(OrderStatus status)
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderStatusChangedToPaidIntegrationEvent> paid = await BusProbe<OrderStatusChangedToPaidIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentSucceededIntegrationEvent(orderId), TestContext.Current.CancellationToken);

        Assert.Empty(await paid.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task PaymentSucceeded_UnknownOrder_IsIgnored()
    {
        await using BusProbe<OrderStatusChangedToPaidIntegrationEvent> paid = await BusProbe<OrderStatusChangedToPaidIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentSucceededIntegrationEvent(long.MaxValue), TestContext.Current.CancellationToken);

        Assert.Empty(await paid.ExpectNoneAsync(e => e.OrderId == long.MaxValue));
    }

    [Fact]
    public async Task PaymentFailed_StockConfirmedOrder_CancelsItWithTheReason_AndPublishesOrderCancelled()
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), OrderStatus.StockConfirmed);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentFailedIntegrationEvent(orderId, "Card declined"), TestContext.Current.CancellationToken);

        OrderCancelledIntegrationEvent @event = await cancelled.WaitForAsync(e => e.OrderId == orderId, "OrderCancelled to be published");
        Assert.Equal("Payment failed: Card declined", @event.Reason);
        JsonElement order = await OrderingApi.WaitForStatusAsync(admin, orderId, "Cancelled");
        Assert.Equal("Payment failed: Card declined", order.GetProperty("cancellationReason").GetString());
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public async Task PaymentFailed_OrderPastCancellation_ChangesNothing_AndPublishesNothing(OrderStatus status)
    {
        long orderId = await OrderingTestData.SeedOrderAsync(system.Ordering.Services, TestJwt.NewUserId(), status);
        using HttpClient admin = OrderingApi.AdminClient(system);
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentFailedIntegrationEvent(orderId, "Card declined"), TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == orderId));
        Assert.Equal(status.ToString(), await OrderingApi.GetStatusAsync(admin, orderId));
    }

    [Fact]
    public async Task PaymentFailed_UnknownOrder_IsIgnored()
    {
        await using BusProbe<OrderCancelledIntegrationEvent> cancelled = await BusProbe<OrderCancelledIntegrationEvent>.StartAsync(OrderingApi.Bus(system));

        await OrderingApi.Bus(system).Publish(new OrderPaymentFailedIntegrationEvent(long.MaxValue, "Card declined"), TestContext.Current.CancellationToken);

        Assert.Empty(await cancelled.ExpectNoneAsync(e => e.OrderId == long.MaxValue));
    }
}
