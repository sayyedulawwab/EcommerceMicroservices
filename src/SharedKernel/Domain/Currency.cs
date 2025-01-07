namespace SharedKernel.Domain;
public record Currency
{
    public static readonly Currency None = new("");
    public static readonly Currency Usd = new("USD");
    public static readonly Currency Bdt = new("BDT");

    private Currency(string code)
    {
        Code = code;
    }

    public string Code { get; init; }

    public static Currency FromCode(string code)
    {
        return All.FirstOrDefault(c => c.Code == code) ?? throw new ApplicationException("The currency code is invalid");
    }

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