using SharedKernel.Domain;

namespace Ordering.Domain.UnitTests.Shared;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("BDT")]
    public void FromCode_SupportedCode_ReturnsCurrency(string code)
    {
        Result<Currency> result = Currency.FromCode(code);

        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Value.Code);
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("usd")]
    [InlineData("")]
    [InlineData(null)]
    public void FromCode_UnsupportedCode_ReturnsUnsupported(string? code)
    {
        Result<Currency> result = Currency.FromCode(code);

        Assert.True(result.IsFailure);
        Assert.Equal(Currency.Unsupported, result.Error);
    }

    [Fact]
    public void FromStoredCode_EmptyCode_ReturnsNone()
    {
        Assert.Equal(Currency.None, Currency.FromStoredCode(string.Empty));
    }

    [Fact]
    public void FromStoredCode_SupportedCode_ReturnsCurrency()
    {
        Assert.Equal(Currency.Usd, Currency.FromStoredCode("USD"));
    }

    [Fact]
    public void FromStoredCode_UnknownCode_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Currency.FromStoredCode("EUR"));
    }
}
