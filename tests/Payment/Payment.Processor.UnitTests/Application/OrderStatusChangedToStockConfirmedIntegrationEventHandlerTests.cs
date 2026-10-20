using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Application.Events;
using SharedKernel.Events;

namespace Payment.Processor.UnitTests.Application;

public sealed class OrderStatusChangedToStockConfirmedIntegrationEventHandlerTests
{
    private const long OrderId = 42;
    private const long UserId = 7;
    private const decimal TotalAmount = 99.5m;
    private const string Currency = "USD";

    private readonly Mock<IPaymentService> _paymentServiceMock = new();
    private readonly Mock<ConsumeContext<OrderStatusChangedToStockConfirmedIntegrationEvent>> _contextMock = new();
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;
    private readonly OrderStatusChangedToStockConfirmedIntegrationEventHandler _consumer;

    public OrderStatusChangedToStockConfirmedIntegrationEventHandlerTests()
    {
        _contextMock
            .Setup(c => c.Message)
            .Returns(new OrderStatusChangedToStockConfirmedIntegrationEvent(OrderId, UserId, TotalAmount, Currency));
        _contextMock.Setup(c => c.CancellationToken).Returns(_cancellationToken);

        _consumer = new OrderStatusChangedToStockConfirmedIntegrationEventHandler(
            NullLogger<OrderStatusChangedToStockConfirmedIntegrationEventHandler>.Instance,
            _paymentServiceMock.Object);
    }

    [Fact]
    public async Task Consume_PaymentSucceeds_PublishesPaymentSucceeded()
    {
        SetupPaymentResult(PaymentResult.Success());

        await _consumer.Consume(_contextMock.Object);

        _paymentServiceMock.Verify(
            s => s.ProcessPaymentAsync(OrderId, TotalAmount, Currency, _cancellationToken),
            Times.Once);
        _contextMock.Verify(
            c => c.Publish(It.Is<OrderPaymentSucceededIntegrationEvent>(e => e.OrderId == OrderId), It.IsAny<CancellationToken>()),
            Times.Once);
        _contextMock.Verify(
            c => c.Publish(It.IsAny<OrderPaymentFailedIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_PaymentFails_PublishesPaymentFailedWithReason()
    {
        SetupPaymentResult(PaymentResult.Failure("The payment was declined"));

        await _consumer.Consume(_contextMock.Object);

        _contextMock.Verify(
            c => c.Publish(
                It.Is<OrderPaymentFailedIntegrationEvent>(e => e.OrderId == OrderId && e.Reason == "The payment was declined"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _contextMock.Verify(
            c => c.Publish(It.IsAny<OrderPaymentSucceededIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Consume_PaymentFailsWithoutReason_PublishesPaymentFailedWithDefaultReason()
    {
        SetupPaymentResult(new PaymentResult(false, null));

        await _consumer.Consume(_contextMock.Object);

        _contextMock.Verify(
            c => c.Publish(
                It.Is<OrderPaymentFailedIntegrationEvent>(e => e.OrderId == OrderId && e.Reason == "Payment failed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetupPaymentResult(PaymentResult result) =>
        _paymentServiceMock
            .Setup(s => s.ProcessPaymentAsync(It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
}
