using System.Net;
using MassTransit;
using Moq;
using Ordering.Application.Abstractions.Clock;
using Ordering.Domain.Orders;
using SharedKernel.Domain;

namespace Ordering.Application.UnitTests.Orders;

public abstract class OrderHandlerTestBase
{
    protected OrderHandlerTestBase()
    {
        Clock.Setup(clock => clock.UtcNow).Returns(TestOrders.UtcNow);

        PublishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(invocation => PublishedMessages.Add(invocation.Arguments[0]!)))
            .Returns(Task.CompletedTask);

        UnitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    protected Mock<IOrderRepository> OrderRepository { get; } = new();
    protected Mock<IUnitOfWork> UnitOfWork { get; } = new();
    protected Mock<IDateTimeProvider> Clock { get; } = new();
    protected Mock<IPublishEndpoint> PublishEndpoint { get; } = new();
    protected List<object> PublishedMessages { get; } = [];

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    protected void GivenOrder(Order order)
    {
        OrderRepository
            .Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
    }

    protected void GivenNoOrder(long orderId)
    {
        OrderRepository
            .Setup(repository => repository.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
    }

    protected void VerifySaved(Times times)
    {
        UnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), times);
    }

    protected static void AssertNotFound(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.NotFound(), result.Error);
        Assert.Equal(HttpStatusCode.NotFound, result.Error.Code);
    }
}
