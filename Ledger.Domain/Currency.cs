namespace Ledger.Domain;

public sealed record Currency
{
    public string Code { get; }
    public int Decimals { get; }

    private Currency(string code, int decimals)
    {
        Code = code;
        Decimals = decimals;
    }

    public static readonly Currency Gbp = new("GBP", 2);
    public static readonly Currency Usd = new("USD", 2);
    public static readonly Currency Eur = new("EUR", 2);
    public static readonly Currency Jpy = new("JPY", 0);

    private static readonly Dictionary<string, Currency> All =
        new[] { Gbp, Usd, Eur, Jpy }
            .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);

    public static Currency FromCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be empty.", nameof(code));

        return All.TryGetValue(code, out var currency)
            ? currency
            : throw new ArgumentException($"Unsupported currency: '{code}'", nameof(code));
    }
}