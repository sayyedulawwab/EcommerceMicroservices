namespace SharedKernel.Domain;

public record Currency
{
    public static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Bdt = new("BDT");

    // Lets Cart rebuild a cached Currency with Newtonsoft.Json, which needs a default constructor.
    private Currency()
    {
        Code = string.Empty;
    }

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; init; }

    public static Currency FromCode(string code)
    {
        if (code == None.Code)
        {
            return None;
        }

        return All.FirstOrDefault(c => c.Code == code) ?? throw new ApplicationException("The currency code is invalid");
    }

    public static bool IsSupported(string? code) => All.Any(c => c.Code == code);

    public static readonly IReadOnlyCollection<Currency> All =
    [
        Usd,
        Bdt
    ];

    public static Currency Create(string code)
    {
        var currency = new Currency(code);

        return currency;
    }
}