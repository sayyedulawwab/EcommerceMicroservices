using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Shared;

public sealed class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        Result<Money> result = new Money(10m, Currency.Usd).Add(new Money(2.5m, Currency.Usd));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(12.5m, Currency.Usd), result.Value);
    }

    [Fact]
    public void Add_ToZeroOfSameCurrency_ReturnsTheAmount()
    {
        Result<Money> result = Money.Zero(Currency.Bdt).Add(new Money(7m, Currency.Bdt));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(7m, Currency.Bdt), result.Value);
    }

    [Fact]
    public void Add_DifferentCurrencies_ReturnsCurrencyMismatch()
    {
        Result<Money> result = new Money(10m, Currency.Usd).Add(new Money(10m, Currency.Bdt));

        Assert.True(result.IsFailure);
        Assert.Equal(Money.CurrencyMismatch, result.Error);
    }
}
