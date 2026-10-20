using FluentValidation.Results;
using Ordering.Application.Orders.PlaceOrder;

namespace Ordering.Application.UnitTests.Orders;

public sealed class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_IsValid()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(TestOrders.KeyboardId, 2)]);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MaxQuantity_IsValid()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(TestOrders.KeyboardId, 1000)]);

        ValidationResult result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyItems_IsInvalid()
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, []);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PlaceOrderCommand.OrderItems));
    }

    [Fact]
    public void Validate_TooManyItems_IsInvalid()
    {
        var items = Enumerable.Range(1, 101).Select(productId => new OrderItemCommand(productId, 1)).ToList();
        var command = new PlaceOrderCommand(TestOrders.UserId, items);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PlaceOrderCommand.OrderItems));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveUserId_IsInvalid(long userId)
    {
        var command = new PlaceOrderCommand(userId, [new OrderItemCommand(TestOrders.KeyboardId, 1)]);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PlaceOrderCommand.UserId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveProductId_IsInvalid(long productId)
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(productId, 1)]);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "OrderItems[0].ProductId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Validate_QuantityOutOfRange_IsInvalid(int quantity)
    {
        var command = new PlaceOrderCommand(TestOrders.UserId, [new OrderItemCommand(TestOrders.KeyboardId, quantity)]);

        ValidationResult result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "OrderItems[0].Quantity");
    }
}
