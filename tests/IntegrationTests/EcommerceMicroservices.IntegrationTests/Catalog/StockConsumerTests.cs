using EcommerceMicroservices.IntegrationTests.Infrastructure;
using SharedKernel.Events;
using System.Net;

namespace EcommerceMicroservices.IntegrationTests.Catalog;

/// <summary>
/// Catalog's order consumers (order placed, cancelled, paid), driven by publishing the integration events on the real broker.
/// What Catalog answers is read from the stock events it publishes; what it stored is read from the products API and the reservations table.
/// </summary>
[Collection(SystemCollection.Name)]
public sealed class StockConsumerTests(SystemFixture system)
{
    [Fact]
    public async Task OrderPlaced_AllItemsInStock_ReservesEachProduct_AndConfirmsWithCatalogPrices()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid first = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5, name: "First product");
        Guid second = await CatalogApi.CreateProductAsync(admin, price: 20, quantity: 8, name: "Second product");
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(first, 2), new OrderStockItem(second, 3));

        OrderItemsStockConfirmedIntegrationEvent confirmed = await probe.WaitForConfirmedAsync(orderId);
        Assert.Equal(2, confirmed.OrderItems.Count);
        Assert.Equal(new PricedOrderItem(first, "First product", 10m, "BDT", 2), Assert.Single(confirmed.OrderItems, item => item.ProductId == first));
        Assert.Equal(new PricedOrderItem(second, "Second product", 20m, "BDT", 3), Assert.Single(confirmed.OrderItems, item => item.ProductId == second));

        Assert.Equal(3, await CatalogApi.GetStockAsync(admin, first));
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, second));
        IReadOnlyList<ReservationRow> reservations = await CatalogTestSupport.GetReservationsAsync(system, orderId);
        Assert.Equal(2, reservations.Count);
        Assert.Contains(new ReservationRow(first, 2, ReservationStatus.Reserved), reservations);
        Assert.Contains(new ReservationRow(second, 3, ReservationStatus.Reserved), reservations);
    }

    [Fact]
    public async Task OrderPlaced_DuplicateProductLines_ReserveTheCombinedQuantityOnce()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 10);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 2), new OrderStockItem(productId, 3));

        OrderItemsStockConfirmedIntegrationEvent confirmed = await probe.WaitForConfirmedAsync(orderId);
        PricedOrderItem item = Assert.Single(confirmed.OrderItems);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Equal([new ReservationRow(productId, 5, ReservationStatus.Reserved)], await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_DuplicateLinesExceedingTheStockTogether_AreRejected()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 4);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 3), new OrderStockItem(productId, 3));

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.Equal([productId], rejected.RejectedProductIds);
        Assert.Equal(4, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_UnknownProduct_IsRejectedWithItsId_AndReservesNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid knownProduct = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        var unknownProduct = Guid.CreateVersion7();
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(knownProduct, 1), new OrderStockItem(unknownProduct, 1));

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.Equal([unknownProduct], rejected.RejectedProductIds);
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, knownProduct));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_InsufficientStock_RejectsEveryShortProduct_AndKeepsAllStock()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid shortOne = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 1);
        Guid shortTwo = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 2);
        Guid enough = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 9);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(
            system,
            orderId,
            new OrderStockItem(shortOne, 2),
            new OrderStockItem(shortTwo, 3),
            new OrderStockItem(enough, 4));

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.Equal(2, rejected.RejectedProductIds.Count);
        Assert.Contains(shortOne, rejected.RejectedProductIds);
        Assert.Contains(shortTwo, rejected.RejectedProductIds);
        Assert.Equal(1, await CatalogApi.GetStockAsync(admin, shortOne));
        Assert.Equal(2, await CatalogApi.GetStockAsync(admin, shortTwo));
        Assert.Equal(9, await CatalogApi.GetStockAsync(admin, enough));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_ProductsInDifferentCurrencies_AreRejected_AndReserveNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid inBdt = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5, currency: "BDT");
        Guid inUsd = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5, currency: "USD");
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(inBdt, 1), new OrderStockItem(inUsd, 1));

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.Contains("different currencies", rejected.Reason, StringComparison.Ordinal);
        Assert.Empty(rejected.RejectedProductIds);
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, inBdt));
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, inUsd));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_WithoutItems_IsRejectedWithTheValidationError()
    {
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId);

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.False(string.IsNullOrWhiteSpace(rejected.Reason));
        Assert.Empty(rejected.RejectedProductIds);
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task OrderPlaced_WithANonPositiveQuantity_IsRejected_AndKeepsTheStock(int quantity)
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, quantity));

        OrderItemsStockRejectedIntegrationEvent rejected = await probe.WaitForRejectedAsync(orderId);
        Assert.False(string.IsNullOrWhiteSpace(rejected.Reason));
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_Redelivered_ReservesOnce_AndIsAnsweredAsReservedAgain()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 10);
        Guid orderId = CatalogTestSupport.NewOrderId();

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 4));
        await probe.WaitForAnswersAsync(orderId, 1);
        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 4));
        await probe.WaitForAnswersAsync(orderId, 2);

        OrderItemsStockConfirmedIntegrationEvent confirmed = await probe.WaitForConfirmedAsync(orderId);
        Assert.Equal(4, Assert.Single(confirmed.OrderItems).Quantity);
        Assert.Equal(6, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Equal([new ReservationRow(productId, 4, ReservationStatus.Reserved)], await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderPlaced_RedeliveredAfterTheStockWasReleased_NeverReservesTwice()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 10);
        Guid orderId = CatalogTestSupport.NewOrderId();
        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 4));
        await probe.WaitForAnswersAsync(orderId, 1);
        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);
        await WaitForStatusAsync(orderId, productId, ReservationStatus.Released);

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, new OrderStockItem(productId, 4));
        await probe.WaitForAnswersAsync(orderId, 2);

        Assert.Equal(10, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Equal([new ReservationRow(productId, 4, ReservationStatus.Released)], await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderCancelled_ReleasesTheReservedStock()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid first = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid second = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 6);
        Guid orderId = await ReserveAsync((first, 2), (second, 3));
        Assert.Equal(3, await CatalogApi.GetStockAsync(admin, first));

        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);
        await WaitForStatusAsync(orderId, first, ReservationStatus.Released);

        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, first));
        Assert.Equal(6, await CatalogApi.GetStockAsync(admin, second));
        Assert.All(await CatalogTestSupport.GetReservationsAsync(system, orderId), row => Assert.Equal(ReservationStatus.Released, row.Status));
    }

    [Fact]
    public async Task OrderCancelled_Twice_ReleasesTheStockOnlyOnce()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid orderId = await ReserveAsync((productId, 2));

        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);
        await WaitForStatusAsync(orderId, productId, ReservationStatus.Released);
        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);
        await WaitForCancelQueueToDrainAsync(admin);

        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, productId));
    }

    [Fact]
    public async Task OrderCancelled_AfterTheOrderWasPaid_DoesNotReleaseTheCommittedStock()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid orderId = await ReserveAsync((productId, 2));
        await CatalogTestSupport.PublishOrderPaidAsync(system, orderId);
        await WaitForStatusAsync(orderId, productId, ReservationStatus.Committed);

        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);
        await WaitForCancelQueueToDrainAsync(admin);

        Assert.Equal(3, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Equal([new ReservationRow(productId, 2, ReservationStatus.Committed)], await CatalogTestSupport.GetReservationsAsync(system, orderId));
    }

    [Fact]
    public async Task OrderCancelled_WhenTheProductWasDeleted_StillReleasesTheReservation()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid orderId = await ReserveAsync((productId, 2));
        Assert.Equal(HttpStatusCode.OK, (await admin.DeleteAsync($"/api/v1/products/{productId}", TestContext.Current.CancellationToken)).StatusCode);

        await CatalogTestSupport.PublishOrderCancelledAsync(system, orderId);

        await WaitForStatusAsync(orderId, productId, ReservationStatus.Released);
    }

    [Fact]
    public async Task OrderCancelled_WithoutReservations_ChangesNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 1);
        Guid rejectedOrderId = CatalogTestSupport.NewOrderId();
        await CatalogTestSupport.PublishOrderPlacedAsync(system, rejectedOrderId, new OrderStockItem(productId, 5));
        await probe.WaitForRejectedAsync(rejectedOrderId);

        await CatalogTestSupport.PublishOrderCancelledAsync(system, rejectedOrderId);
        await WaitForCancelQueueToDrainAsync(admin);

        Assert.Equal(1, await CatalogApi.GetStockAsync(admin, productId));
        Assert.Empty(await CatalogTestSupport.GetReservationsAsync(system, rejectedOrderId));
    }

    [Fact]
    public async Task OrderPaid_CommitsTheReservations_SoTheyAreNotReleasedAnyMore()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid first = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid second = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid orderId = await ReserveAsync((first, 1), (second, 2));

        await CatalogTestSupport.PublishOrderPaidAsync(system, orderId);
        await WaitForStatusAsync(orderId, first, ReservationStatus.Committed);

        Assert.All(await CatalogTestSupport.GetReservationsAsync(system, orderId), row => Assert.Equal(ReservationStatus.Committed, row.Status));
        Assert.Equal(4, await CatalogApi.GetStockAsync(admin, first));
        Assert.Equal(3, await CatalogApi.GetStockAsync(admin, second));
    }

    [Fact]
    public async Task OrderPaid_WithoutActiveReservations_ChangesNothing()
    {
        using HttpClient admin = CatalogTestSupport.CreateAdminClient(system);
        Guid productId = await CatalogApi.CreateProductAsync(admin, price: 10, quantity: 5);
        Guid releasedOrderId = await ReserveAsync((productId, 2));
        await CatalogTestSupport.PublishOrderCancelledAsync(system, releasedOrderId);
        await WaitForStatusAsync(releasedOrderId, productId, ReservationStatus.Released);

        await CatalogTestSupport.PublishOrderPaidAsync(system, releasedOrderId);
        await CatalogTestSupport.PublishOrderPaidAsync(system, CatalogTestSupport.NewOrderId());
        await WaitForPaidQueueToDrainAsync(admin);

        Assert.Equal([new ReservationRow(productId, 2, ReservationStatus.Released)], await CatalogTestSupport.GetReservationsAsync(system, releasedOrderId));
        Assert.Equal(5, await CatalogApi.GetStockAsync(admin, productId));
    }

    /// <summary>Places an order for the given (productId, quantity) tuples and waits until Catalog confirmed it.</summary>
    private async Task<Guid> ReserveAsync(params (Guid ProductId, int Quantity)[] reservations)
    {
        await using StockEventProbe probe = await StockEventProbe.StartAsync(system);
        Guid orderId = CatalogTestSupport.NewOrderId();
        OrderStockItem[] items = [.. reservations.Select(reservation => new OrderStockItem(reservation.ProductId, reservation.Quantity))];

        await CatalogTestSupport.PublishOrderPlacedAsync(system, orderId, items);
        await probe.WaitForConfirmedAsync(orderId);

        return orderId;
    }

    private Task<IReadOnlyList<ReservationRow>> WaitForStatusAsync(Guid orderId, Guid productId, ReservationStatus status)
    {
        return Eventually.UntilAsync(
            () => CatalogTestSupport.GetReservationsAsync(system, orderId),
            rows => rows.Any(row => row.ProductId == productId && row.Status == status),
            $"the reservation of order {orderId} for product {productId} to be {status}");
    }

    // The cancel and paid consumers each read their own queue, so an event published afterwards that is visibly handled
    // proves the earlier ones on that queue were handled too.
    private Task WaitForCancelQueueToDrainAsync(HttpClient admin) => WaitForMarkerAsync(admin, CatalogTestSupport.PublishOrderCancelledAsync, ReservationStatus.Released);

    private Task WaitForPaidQueueToDrainAsync(HttpClient admin) => WaitForMarkerAsync(admin, CatalogTestSupport.PublishOrderPaidAsync, ReservationStatus.Committed);

    private async Task WaitForMarkerAsync(HttpClient admin, Func<SystemFixture, Guid, Task> publish, ReservationStatus expected)
    {
        Guid markerProduct = await CatalogApi.CreateProductAsync(admin, price: 1, quantity: 1);
        Guid markerOrder = await ReserveAsync((markerProduct, 1));

        await publish(system, markerOrder);
        await WaitForStatusAsync(markerOrder, markerProduct, expected);
    }
}
