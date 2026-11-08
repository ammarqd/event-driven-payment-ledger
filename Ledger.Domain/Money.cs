namespace Ledger.Domain;

public readonly record struct Money
{
    public long MinorUnits { get; }
    public Currency Currency { get; }

    public Money(long minorUnits, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        MinorUnits = minorUnits;
        Currency = currency;
    }

    public static Money Zero(Currency currency) => new(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add different currencies.");

        return new Money(checked(a.MinorUnits + b.MinorUnits), a.Currency);
    }
}