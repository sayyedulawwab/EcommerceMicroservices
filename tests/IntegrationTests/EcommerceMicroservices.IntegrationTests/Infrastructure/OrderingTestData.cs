using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions.Data;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace EcommerceMicroservices.IntegrationTests.Infrastructure;

/// <summary>
/// Puts orders into a given state in the Ordering database, so a test can drive one consumer or endpoint without
/// racing the real saga. The orders are stored through the service's own <see cref="IApplicationDbContext"/>.
/// </summary>
public static class OrderingTestData
{
    // Products that don't exist in Catalog: seeded orders never take part in the real saga on their own.
    public static readonly Guid KeyboardId = Guid.CreateVersion7();
    public static readonly Guid MouseId = Guid.CreateVersion7();

    public const string SeededCancellationReason = "Seeded cancellation";

    public static async Task<Guid> SeedOrderAsync(IServiceProvider ordering, Guid userId, OrderStatus status)
    {
        DateTime utcNow = DateTime.UtcNow;

        Order order = Order.Place(userId, [new OrderLine(KeyboardId, 2), new OrderLine(MouseId, 3)], utcNow).Value;

        if (status == OrderStatus.Cancelled)
        {
            Arrange(order.Cancel(SeededCancellationReason, utcNow));
        }
        else if (status != OrderStatus.Placed)
        {
            // 2 keyboards at 10 USD and 3 mice at 5 USD: total 35 USD.
            Arrange(order.ConfirmStock(
                [
                    new PricedOrderLine(KeyboardId, "Keyboard", new Money(10m, Currency.Usd)),
                    new PricedOrderLine(MouseId, "Mouse", new Money(5m, Currency.Usd))
                ],
                utcNow));

            if (status is OrderStatus.Paid or OrderStatus.Shipped or OrderStatus.Delivered)
            {
                Arrange(order.MarkPaid(utcNow));
            }

            if (status is OrderStatus.Shipped or OrderStatus.Delivered)
            {
                Arrange(order.Ship(utcNow));
            }

            if (status == OrderStatus.Delivered)
            {
                Arrange(order.Deliver(utcNow));
            }
        }

        await using AsyncServiceScope scope = ordering.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return order.Id.Value;
    }

    private static void Arrange(Result result)
    {
        Assert.True(result.IsSuccess, $"Arranging the order failed: {result.Error.Name}");
    }
}
