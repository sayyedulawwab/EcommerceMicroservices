using Catalog.Application.Products.Events;
using Catalog.Application.Products.ReserveStock;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Catalog.Application.UnitTests.Products;

public sealed class OrderPlacedIntegrationEventHandlerTests
{
    private const long OrderId = 42;

    private readonly Mock<ICommandHandler<ReserveStockCommand, ReserveStockResult>> _reserveHandler = new();
    private readonly Mock<ConsumeContext<OrderPlacedIntegrationEvent>> _context = new();
    private readonly List<object> _published = [];
    private readonly OrderPlacedIntegrationEventHandler _consumer;
    private readonly OrderPlacedIntegrationEvent _message =
        new(OrderId, 3, [new OrderStockItem(1, 2), new OrderStockItem(2, 1)]);

    public OrderPlacedIntegrationEventHandlerTests()
    {
        _context.Setup(c => c.Message).Returns(_message);
        _context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        _context
            .Setup(c => c.Publish(It.IsAny<OrderItemsStockConfirmedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderItemsStockConfirmedIntegrationEvent, CancellationToken>((message, _) => _published.Add(message))
            .Returns(Task.CompletedTask);
        _context
            .Setup(c => c.Publish(It.IsAny<OrderItemsStockRejectedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<OrderItemsStockRejectedIntegrationEvent, CancellationToken>((message, _) => _published.Add(message))
            .Returns(Task.CompletedTask);

        _consumer = new OrderPlacedIntegrationEventHandler(
            NullLogger<OrderPlacedIntegrationEventHandler>.Instance,
            _reserveHandler.Object);
    }

    private void SetupHandlerResult(Result<ReserveStockResult> result) =>
        _reserveHandler
            .Setup(h => h.Handle(It.IsAny<ReserveStockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    [Fact]
    public async Task Consume_Reserved_PublishesStockConfirmedWithPricedItems()
    {
        List<PricedOrderItem> items =
        [
            new(1, "Keyboard", 50m, "USD", 2),
            new(2, "Mouse", 20m, "USD", 1),
        ];
        SetupHandlerResult(ReserveStockResult.Reserved(items));

        await _consumer.Consume(_context.Object);

        object published = Assert.Single(_published);
        OrderItemsStockConfirmedIntegrationEvent confirmed = Assert.IsType<OrderItemsStockConfirmedIntegrationEvent>(published);
        Assert.Equal(OrderId, confirmed.OrderId);
        Assert.Equal(items, confirmed.OrderItems);
    }

    [Fact]
    public async Task Consume_PassesOrderItemsToReserveHandler()
    {
        SetupHandlerResult(ReserveStockResult.Reserved([]));

        await _consumer.Consume(_context.Object);

        _reserveHandler.Verify(
            h => h.Handle(
                It.Is<ReserveStockCommand>(c => c.OrderId == OrderId && c.Items.SequenceEqual(_message.OrderStockItems)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_Rejected_PublishesStockRejectedWithProductIds()
    {
        SetupHandlerResult(ReserveStockResult.Rejected("Out of stock", [2L]));

        await _consumer.Consume(_context.Object);

        object published = Assert.Single(_published);
        OrderItemsStockRejectedIntegrationEvent rejected = Assert.IsType<OrderItemsStockRejectedIntegrationEvent>(published);
        Assert.Equal(OrderId, rejected.OrderId);
        Assert.Equal("Out of stock", rejected.Reason);
        Assert.Equal([2L], rejected.RejectedProductIds);
    }

    [Fact]
    public async Task Consume_HandlerFailure_PublishesStockRejectedWithErrorDescription()
    {
        var error = Error.Conflict("Product.InsufficientStock", "Product 2 does not have enough stock");
        SetupHandlerResult(Result.Failure<ReserveStockResult>(error));

        await _consumer.Consume(_context.Object);

        object published = Assert.Single(_published);
        OrderItemsStockRejectedIntegrationEvent rejected = Assert.IsType<OrderItemsStockRejectedIntegrationEvent>(published);
        Assert.Equal(OrderId, rejected.OrderId);
        Assert.Equal(error.Description, rejected.Reason);
        Assert.Empty(rejected.RejectedProductIds);
    }
}
