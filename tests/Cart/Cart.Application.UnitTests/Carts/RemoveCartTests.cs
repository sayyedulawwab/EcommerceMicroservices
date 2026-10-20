using Cart.Application.Carts.RemoveCart;
using Cart.Domain.Carts;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedKernel.Domain;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace Cart.Application.UnitTests.Carts;

public sealed class RemoveCartTests
{
    [Fact]
    public async Task RemoveCartCommandHandler_Handle_RemovesUsersCart()
    {
        var cartRepositoryMock = new Mock<ICartRepository>();
        var handler = new RemoveCartCommandHandler(cartRepositoryMock.Object);

        Result result = await handler.Handle(new RemoveCartCommand(CartTestData.UserId), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        cartRepositoryMock.Verify(r => r.RemoveAsync(CartTestData.UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OrderStatusChangedToPaidConsumer_Consume_SendsRemoveCartCommandForEventsUser()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var commandHandlerMock = new Mock<ICommandHandler<RemoveCartCommand>>();
        commandHandlerMock
            .Setup(h => h.Handle(It.IsAny<RemoveCartCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        var contextMock = new Mock<ConsumeContext<OrderStatusChangedToPaidIntegrationEvent>>();
        contextMock.Setup(c => c.Message).Returns(new OrderStatusChangedToPaidIntegrationEvent(CartTestData.UserId, 42));
        contextMock.Setup(c => c.CancellationToken).Returns(cancellationToken);
        var consumer = new OrderStatusChangedToPaidIntegrationEventHandler(
            NullLogger<OrderStatusChangedToPaidIntegrationEventHandler>.Instance,
            commandHandlerMock.Object);

        await consumer.Consume(contextMock.Object);

        commandHandlerMock.Verify(
            h => h.Handle(It.Is<RemoveCartCommand>(c => c.UserId == CartTestData.UserId), cancellationToken),
            Times.Once);
    }
}
