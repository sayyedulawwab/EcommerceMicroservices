using Catalog.Application.Products.AddProduct;
using Catalog.Application.Products.ReserveStock;
using FluentValidation.Results;
using SharedKernel.Events;

namespace Catalog.Application.UnitTests.Products;

public sealed class AddProductCommandValidatorTests
{
    private readonly AddProductCommandValidator _validator = new();

    private static AddProductCommand Command(string currency) =>
        new("Keyboard", "Mechanical keyboard", currency, 50m, 10, 1);

    [Theory]
    [InlineData("USD")]
    [InlineData("BDT")]
    public void Validate_SupportedCurrency_IsValid(string currency)
    {
        ValidationResult result = _validator.Validate(Command(currency));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("usd")]
    [InlineData("")]
    public void Validate_UnsupportedCurrency_IsInvalid(string currency)
    {
        ValidationResult result = _validator.Validate(Command(currency));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddProductCommand.PriceCurrency));
    }
}

public sealed class ReserveStockCommandValidatorTests
{
    private readonly ReserveStockCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidItems_IsValid()
    {
        ValidationResult result = _validator.Validate(new ReserveStockCommand(1, [new OrderStockItem(1, 2)]));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyItems_IsInvalid()
    {
        ValidationResult result = _validator.Validate(new ReserveStockCommand(1, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ReserveStockCommand.Items));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveQuantity_IsInvalid(int quantity)
    {
        ValidationResult result = _validator.Validate(
            new ReserveStockCommand(1, [new OrderStockItem(1, 2), new OrderStockItem(2, quantity)]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.EndsWith(nameof(OrderStockItem.Quantity), StringComparison.Ordinal));
    }
}
