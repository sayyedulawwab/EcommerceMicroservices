using EcommerceMicroservices.IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Events;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace EcommerceMicroservices.IntegrationTests.Catalog;

internal enum ReservationStatus
{
    Reserved = 1,
    Released = 2,
    Committed = 3,
}

internal sealed record ReservationRow(long ProductId, int Quantity, ReservationStatus Status);

/// <summary>Shared helpers for the Catalog tests: an admin client, JSON readers, the reservations table and the stock events Catalog publishes.</summary>
internal static class CatalogTestSupport
{
    private static long _nextOrderId = DateTime.UtcNow.Ticks;

    public static HttpClient CreateAdminClient(SystemFixture system)
    {
        return SystemFixture.CreateClient(system.Catalog, TestJwt.Create(TestJwt.NewUserId(), isAdmin: true));
    }

    /// <summary>An order id no real order uses, so a published event only concerns the test that made it.</summary>
    public static long NewOrderId() => Interlocked.Increment(ref _nextOrderId);

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    public static async Task<string?> ReadErrorNameAsync(HttpResponseMessage response)
    {
        JsonElement body = await ReadJsonAsync(response);

        return body.GetProperty("name").GetString();
    }

    public static async Task<IReadOnlyList<ReservationRow>> GetReservationsAsync(SystemFixture system, long orderId)
    {
        string connectionString = system.Catalog.Services.GetRequiredService<IConfiguration>().GetConnectionString("EcommerceDB")!;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT ProductId, Quantity, Status FROM StockReservations WHERE OrderId = @orderId ORDER BY ProductId";
        command.Parameters.AddWithValue("@orderId", orderId);

        var rows = new List<ReservationRow>();

        await using SqlDataReader reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            rows.Add(new ReservationRow(reader.GetInt64(0), reader.GetInt32(1), (ReservationStatus)reader.GetInt32(2)));
        }

        return rows;
    }

    public static Task PublishOrderPlacedAsync(SystemFixture system, long orderId, params OrderStockItem[] items)
    {
        return system.Catalog.Services.GetRequiredService<IBus>().Publish(
            new OrderPlacedIntegrationEvent(orderId, TestJwt.NewUserId(), [.. items]),
            TestContext.Current.CancellationToken);
    }

    public static Task PublishOrderCancelledAsync(SystemFixture system, long orderId)
    {
        return system.Catalog.Services.GetRequiredService<IBus>().Publish(
            new OrderCancelledIntegrationEvent(orderId, "Cancelled by an integration test"),
            TestContext.Current.CancellationToken);
    }

    public static Task PublishOrderPaidAsync(SystemFixture system, long orderId)
    {
        return system.Catalog.Services.GetRequiredService<IBus>().Publish(
            new OrderStatusChangedToPaidIntegrationEvent(TestJwt.NewUserId(), orderId),
            TestContext.Current.CancellationToken);
    }
}

/// <summary>
/// Listens, on a temporary queue bound to Catalog's bus, for the stock events Catalog's order-placed consumer publishes,
/// so a test can wait until the consumer has finished with an order.
/// </summary>
internal sealed class StockEventProbe : IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, OrderItemsStockConfirmedIntegrationEvent> _confirmed = new();
    private readonly ConcurrentDictionary<long, OrderItemsStockRejectedIntegrationEvent> _rejected = new();
    private readonly ConcurrentDictionary<long, int> _answers = new();
    private HostReceiveEndpointHandle? _handle;

    private StockEventProbe()
    {
    }

    public static async Task<StockEventProbe> StartAsync(SystemFixture system)
    {
        var probe = new StockEventProbe();
        IBusControl bus = system.Catalog.Services.GetRequiredService<IBusControl>();

        probe._handle = bus.ConnectReceiveEndpoint($"catalog-tests-probe-{Guid.NewGuid():N}", endpoint =>
        {
            endpoint.Handler<OrderItemsStockConfirmedIntegrationEvent>(context =>
            {
                probe._confirmed[context.Message.OrderId] = context.Message;
                probe._answers.AddOrUpdate(context.Message.OrderId, 1, (_, count) => count + 1);

                return Task.CompletedTask;
            });

            endpoint.Handler<OrderItemsStockRejectedIntegrationEvent>(context =>
            {
                probe._rejected[context.Message.OrderId] = context.Message;
                probe._answers.AddOrUpdate(context.Message.OrderId, 1, (_, count) => count + 1);

                return Task.CompletedTask;
            });
        });

        await probe._handle.Ready;

        return probe;
    }

    public async Task<OrderItemsStockConfirmedIntegrationEvent> WaitForConfirmedAsync(long orderId)
    {
        await WaitForOutcomeAsync(orderId);

        Assert.True(_confirmed.TryGetValue(orderId, out OrderItemsStockConfirmedIntegrationEvent? confirmed), $"Order {orderId} was not confirmed. Rejected: {_rejected.GetValueOrDefault(orderId)?.Reason}");

        return confirmed;
    }

    public async Task<OrderItemsStockRejectedIntegrationEvent> WaitForRejectedAsync(long orderId)
    {
        await WaitForOutcomeAsync(orderId);

        Assert.True(_rejected.TryGetValue(orderId, out OrderItemsStockRejectedIntegrationEvent? rejected), $"Order {orderId} was not rejected.");

        return rejected;
    }

    /// <summary>Waits until Catalog has answered the order-placed event of this order <paramref name="count"/> times (redeliveries are answered every time).</summary>
    public Task<int> WaitForAnswersAsync(long orderId, int count)
    {
        return Eventually.UntilAsync(
            () => Task.FromResult(_answers.GetValueOrDefault(orderId)),
            answers => answers >= count,
            $"Catalog to answer order {orderId} {count} times");
    }

    public async ValueTask DisposeAsync()
    {
        if (_handle is not null)
        {
            await _handle.StopAsync();
        }
    }

    private Task<bool> WaitForOutcomeAsync(long orderId)
    {
        return Eventually.UntilAsync(
            () => Task.FromResult(_confirmed.ContainsKey(orderId) || _rejected.ContainsKey(orderId)),
            done => done,
            $"Catalog to answer the order-placed event of order {orderId}");
    }
}
