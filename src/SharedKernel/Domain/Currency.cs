namespace SharedKernel.Domain;

public record Currency
{
    public static readonly Error Unsupported = Error.BadRequest(
        "Currency.Unsupported",
        "The currency code is not supported");

    // The currency of an amount that has none yet (Money.Zero()). It is not a supported currency.
    public static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Bdt = new("BDT");

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; }

    public static readonly IReadOnlyCollection<Currency> All =
    [
        Usd,
        Bdt
    ];

    public static bool IsSupported(string? code) => All.Any(c => c.Code == code);

    public static Result<Currency> FromCode(string? code)
    {
        Currency? currency = All.FirstOrDefault(c => c.Code == code);

        if (currency is null)
        {
            return Result.Failure<Currency>(Unsupported);
        }

        return currency;
    }

    /// <summary>
    /// Rebuilds a currency that was stored earlier: a supported code, or the empty code of <see cref="None"/>.
    /// A stored code that is neither means the data is corrupt, so this throws instead of returning a Result.
    /// </summary>
    public static Currency FromStoredCode(string code) =>
        code == None.Code ? None : FromCode(code).Value;
}
