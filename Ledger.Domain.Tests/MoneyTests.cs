namespace Ledger.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidInput_StoresFields()
    {
        var money = new Money(1050, Currency.Gbp);

        Assert.Equal(1050, money.MinorUnits);
        Assert.Equal(Currency.Gbp, money.Currency);
    }

    [Fact]
    public void Constructor_NullCurrency_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Money(100, null!));
    }

    [Fact]
    public void Add_ZeroPlusMoney_ReturnsMoney()
    {
        var result = Money.Zero(Currency.Gbp) + new Money(500, Currency.Gbp);

        Assert.Equal(new Money(500, Currency.Gbp), result);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Money(500, Currency.Gbp) + new Money(500, Currency.Usd));
    }

    [Fact]
    public void Add_Overflow_Throws()
    {
        Assert.Throws<OverflowException>(
            () => new Money(long.MaxValue, Currency.Gbp) + new Money(1, Currency.Gbp));
    }
}