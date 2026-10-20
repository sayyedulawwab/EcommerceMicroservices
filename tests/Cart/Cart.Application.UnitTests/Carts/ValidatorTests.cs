using Cart.Application.Carts.AddCartItem;
using Cart.Application.Carts.ReplaceCart;
using FluentValidation.Results;

namespace Cart.Application.UnitTests.Carts;

public sealed class ValidatorTests
{
    private readonly AddCartItemCommandValidator _addCartItemValidator = new();
    private readonly ReplaceCartCommandValidator _replaceCartValidator = new();

    [Theory]
    [InlineData("USD")]
    [InlineData("BDT")]
    public void AddCartItemValidator_ValidCommand_IsValid(string currency)
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(currency: currency));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddCartItemValidator_MaxQuantity_IsValid()
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(quantity: 1000));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("")]
    [InlineData("usd")]
    public void AddCartItemValidator_UnsupportedCurrency_IsInvalid(string currency)
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(currency: currency));

        AssertSingleErrorFor(result, nameof(AddCartItemCommand.PriceCurrency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddCartItemValidator_NonPositivePrice_IsInvalid(int price)
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(price: price));

        AssertSingleErrorFor(result, nameof(AddCartItemCommand.PriceAmount));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public void AddCartItemValidator_QuantityOutOfRange_IsInvalid(int quantity)
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(quantity: quantity));

        AssertSingleErrorFor(result, nameof(AddCartItemCommand.Quantity));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddCartItemValidator_EmptyProductName_IsInvalid(string productName)
    {
        ValidationResult result = _addCartItemValidator.Validate(CreateAddCommand(productName: productName));

        AssertSingleErrorFor(result, nameof(AddCartItemCommand.ProductName));
    }

    [Fact]
    public void ReplaceCartValidator_SameCurrency_IsValid()
    {
        var command = new ReplaceCartCommand(
            CartTestData.UserId,
            [
                new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "USD", 1),
                new CartItemCommand(CartTestData.ProductB, "Mouse", 5m, "USD", 2),
            ]);

        ValidationResult result = _replaceCartValidator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReplaceCartValidator_EmptyList_IsValid()
    {
        ValidationResult result = _replaceCartValidator.Validate(new ReplaceCartCommand(CartTestData.UserId, []));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReplaceCartValidator_MixedCurrencies_IsInvalid()
    {
        var command = new ReplaceCartCommand(
            CartTestData.UserId,
            [
                new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "USD", 1),
                new CartItemCommand(CartTestData.ProductB, "Mouse", 500m, "BDT", 2),
            ]);

        ValidationResult result = _replaceCartValidator.Validate(command);

        ValidationFailure failure = AssertSingleErrorFor(result, nameof(ReplaceCartCommand.CartItems));
        Assert.Contains("same currency", failure.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ReplaceCartValidator_InvalidItem_ReportsItemProperty()
    {
        var command = new ReplaceCartCommand(
            CartTestData.UserId,
            [new CartItemCommand(CartTestData.ProductA, "Keyboard", 10m, "EUR", 1)]);

        ValidationResult result = _replaceCartValidator.Validate(command);

        AssertSingleErrorFor(result, "CartItems[0].PriceCurrency");
    }

    [Fact]
    public void ReplaceCartValidator_MoreThanMaxItems_IsInvalid()
    {
        var items = Enumerable.Range(1, 101)
            .Select(productId => new CartItemCommand(productId, "Product", 1m, "USD", 1))
            .ToList();

        ValidationResult result = _replaceCartValidator.Validate(new ReplaceCartCommand(CartTestData.UserId, items));

        AssertSingleErrorFor(result, nameof(ReplaceCartCommand.CartItems));
    }

    private static AddCartItemCommand CreateAddCommand(
        string productName = "Keyboard",
        decimal price = 10m,
        string currency = "USD",
        int quantity = 1) =>
        new(CartTestData.UserId, CartTestData.ProductA, productName, price, currency, quantity);

    private static ValidationFailure AssertSingleErrorFor(ValidationResult result, string propertyName)
    {
        Assert.False(result.IsValid);
        ValidationFailure failure = Assert.Single(result.Errors);
        Assert.Equal(propertyName, failure.PropertyName);

        return failure;
    }
}
