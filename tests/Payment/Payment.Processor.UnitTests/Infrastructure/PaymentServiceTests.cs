using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Payment.Processor.Application.Abstractions;
using Payment.Processor.Infrastructure;

namespace Payment.Processor.UnitTests.Infrastructure;

public sealed class PaymentServiceTests
{
    private const long OrderId = 42;

    [Theory]
    [InlineData(1)]
    [InlineData(99.99)]
    [InlineData(1_000_000)]
    public async Task ProcessPaymentAsync_SuccessRateOne_Succeeds(double amount)
    {
        PaymentService service = CreateService(new PaymentOptions { SuccessRate = 1 });

        PaymentResult result = await service.ProcessPaymentAsync(OrderId, (decimal)amount, "USD", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SuccessRateZero_FailsAsDeclined()
    {
        PaymentService service = CreateService(new PaymentOptions { SuccessRate = 0 });

        PaymentResult result = await service.ProcessPaymentAsync(OrderId, 10m, "USD", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("declined", result.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AmountAboveLimit_FailsMentioningLimit()
    {
        PaymentService service = CreateService(new PaymentOptions { SuccessRate = 1, FailAboveAmount = 500m });

        PaymentResult result = await service.ProcessPaymentAsync(OrderId, 501m, "USD", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("limit", result.FailureReason, StringComparison.Ordinal);
        Assert.Contains("500 USD", result.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AmountEqualToLimit_Succeeds()
    {
        PaymentService service = CreateService(new PaymentOptions { SuccessRate = 1, FailAboveAmount = 500m });

        PaymentResult result = await service.ProcessPaymentAsync(OrderId, 500m, "USD", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task ProcessPaymentAsync_NonPositiveAmount_Fails(int amount)
    {
        PaymentService service = CreateService(new PaymentOptions { SuccessRate = 1 });

        PaymentResult result = await service.ProcessPaymentAsync(OrderId, amount, "USD", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("greater than zero", result.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReadsCurrentOptionsOnEveryCall()
    {
        var options = new PaymentOptions { SuccessRate = 1 };
        var optionsMonitorMock = new Mock<IOptionsMonitor<PaymentOptions>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(() => options);
        var service = new PaymentService(optionsMonitorMock.Object, NullLogger<PaymentService>.Instance);

        PaymentResult first = await service.ProcessPaymentAsync(OrderId, 10m, "USD", TestContext.Current.CancellationToken);
        options = new PaymentOptions { SuccessRate = 0 };
        PaymentResult second = await service.ProcessPaymentAsync(OrderId, 10m, "USD", TestContext.Current.CancellationToken);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
    }

    private static PaymentService CreateService(PaymentOptions options)
    {
        var optionsMonitorMock = new Mock<IOptionsMonitor<PaymentOptions>>();
        optionsMonitorMock.Setup(m => m.CurrentValue).Returns(options);

        return new PaymentService(optionsMonitorMock.Object, NullLogger<PaymentService>.Instance);
    }
}
