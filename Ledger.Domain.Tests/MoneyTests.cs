namespace Ledger.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_ValidInput_StoresFields()
    {
        var money = new Money(1050, "GBP");

        Assert.Equal(1050, money.MinorUnits);
        Assert.Equal("GBP", money.Currency);
    }

    [Fact]
    public void Constructor_TwoLetterCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(100, "GB"));
    }

    [Fact]
    public void Add_ZeroPlusMoney_ReturnsMoney()
    {
        var result = Money.Zero("GBP") + new Money(500, "GBP");

        Assert.Equal(new Money(500, "GBP"), result);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => new Money(500, "GBP") + new Money(500, "USD"));
    }

    [Fact]
    public void Add_Overflow_Throws()
    {
        Assert.Throws<OverflowException>(
            () => new Money(long.MaxValue, "GBP") + new Money(1, "GBP"));
    }
}