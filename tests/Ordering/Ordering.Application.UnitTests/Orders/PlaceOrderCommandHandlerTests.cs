using Moq;
using Ordering.Application.Orders.PlaceOrder;
using Ordering.Domain.Orders;
using SharedKernel.Domain;
using SharedKernel.Events;

namespace Ordering.Application.UnitTests.Orders;

public sealed class PlaceOrderCommandHandlerTests : OrderHandlerTestBase
{
    private const long GeneratedOrderId = 42;

    private readonly List<string> _calls = [];
    private Order? _addedOrder;

    public PlaceOrderCommandHandlerTests()
    {
        UnitOfWork
            .Setup(unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<CancellationToken, Task> operation, CancellationToken cancellationToken) =>
            {
                _calls.Add("begin");
                await operation(cancellationToken);
                _calls.Add("commit");
            });

        OrderRepository
            .Setup(repository => repository.Add(It.IsAny<Order>()))
            .Callback((Order order) =>
            {
                _calls.Add("add");
                _addedOrder = order;
            });

        // Simulates the database generating the order id on the first save.
        UnitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _calls.Add("save");

                if (_addedOrder is { Id: 0 })
                {
                    TestOrders.SetId(_addedOrder, GeneratedOrderId);
                }
            })
            .ReturnsAsync(1);

        PublishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(invocation =>
            {
                _calls.Add("publish");
                PublishedMessages.Add(invocation.Arguments[0]!);
            }))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsSavesPublishesAndSavesAgainInsideTransaction()
    {
        var command = new PlaceOrderCommand(
            TestOrders.UserId,
            [new OrderItemCommand(TestOrders.KeyboardId, 2), new OrderItemCommand(TestOrders.MouseId, 3)]);

        Result<long> result = await CreateHandler().Handle(command, CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(GeneratedOrderId, result.Value);
        Assert.Equal(["begin", "add", "save", "publish", "save", "commit"], _calls);
        UnitOfWork.Verify(
            unitOfWork => unitOfWork.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsPlacedOrderForUser()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(TestOrders.KeyboardId, 2)]);

        await CreateHandler().Handle(command, CancellationToken);

        Assert.NotNull(_addedOrder);
        Assert.Equal(TestOrders.UserId, _addedOrder.UserId);
        Assert.Equal(OrderStatus.Placed, _addedOrder.Status);
        Assert.Equal(TestOrders.UtcNow, _addedOrder.CreatedOnUtc);
        OrderRepository.Verify(repository => repository.Add(It.IsAny<Order>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesOrderPlacedEventWithMergedItems()
    {
        var command = new PlaceOrderCommand(
            TestOrders.UserId,
            [
                new OrderItemCommand(TestOrders.KeyboardId, 2),
                new OrderItemCommand(TestOrders.MouseId, 3),
                new OrderItemCommand(TestOrders.KeyboardId, 1),
            ]);

        await CreateHandler().Handle(command, CancellationToken);

        OrderPlacedIntegrationEvent placed = Assert.IsType<OrderPlacedIntegrationEvent>(Assert.Single(PublishedMessages));
        Assert.Equal(GeneratedOrderId, placed.OrderId);
        Assert.Equal(TestOrders.UserId, placed.UserId);
        Assert.Equal(2, placed.OrderStockItems.Count);
        Assert.Contains(new OrderStockItem(TestOrders.KeyboardId, 3), placed.OrderStockItems);
        Assert.Contains(new OrderStockItem(TestOrders.MouseId, 3), placed.OrderStockItems);
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyErrorWithoutTouchingRepository()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, []);

        Result<long> result = await CreateHandler().Handle(command, CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.Empty, result.Error);
        Assert.Empty(_calls);
        Assert.Empty(PublishedMessages);
        OrderRepository.Verify(repository => repository.Add(It.IsAny<Order>()), Times.Never);
        VerifySaved(Times.Never());
    }

    [Fact]
    public async Task Handle_NonPositiveQuantity_ReturnsInvalidQuantityWithoutTouchingRepository()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(TestOrders.KeyboardId, 0)]);

        Result<long> result = await CreateHandler().Handle(command, CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity(TestOrders.KeyboardId), result.Error);
        Assert.Empty(_calls);
        Assert.Empty(PublishedMessages);
    }

    private PlaceOrderCommandHandler CreateHandler()
    {
        return new PlaceOrderCommandHandler(OrderRepository.Object, UnitOfWork.Object, Clock.Object, PublishEndpoint.Object);
    }
}
