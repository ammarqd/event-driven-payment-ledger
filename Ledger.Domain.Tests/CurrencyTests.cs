namespace Ledger.Domain.Tests;

public class CurrencyTests
{
    [Fact]
    public void FromCode_KnownCode_ReturnsThatCurrency()
    {
        var currency = Currency.FromCode("GBP");

        Assert.Equal(Currency.Gbp, currency);
        Assert.Equal(2, currency.Decimals);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void FromCode_BlankCode_Throws(string? code)
    {
        Assert.Throws<ArgumentException>(() => Currency.FromCode(code!));
    } 

    [Fact]
    public void FromCode_LowercaseCode_ReturnsThatCurrency()
    {
        Assert.Equal(Currency.Usd, Currency.FromCode("usd"));
    }

    [Fact]
    public void FromCode_JapaneseYen_HasNoDecimals()
    {
        Assert.Equal(0, Currency.FromCode("JPY").Decimals);
    }

    [Fact]
    public void FromCode_UnknownCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => Currency.FromCode("XYZ"));
    }
}