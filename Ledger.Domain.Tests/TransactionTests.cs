namespace Ledger.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void SingleEntry_Throws()
    {
        Entry[] entries = [new(Guid.NewGuid(), new Money(0, "GBP"))];

        Assert.Throws<ArgumentException>(
            () => new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries));
    }

    [Fact]
    public void BalancedEntries_Succeeds()
    {
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-1000, "GBP")),
            new(Guid.NewGuid(), new Money(1000, "GBP")),
        ];

        var tx = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        Assert.Equal(2, tx.Entries.Count);
    }

    [Fact]
    public void UnbalancedEntries_Throws()
    {
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-1000, "GBP")),
            new(Guid.NewGuid(), new Money(900, "GBP")),
        ];

        Assert.Throws<ArgumentException>(
            () => new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries));
    }

    [Fact]
    public void MixedCurrencies_Throws()
    {
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-500, "GBP")),
            new(Guid.NewGuid(), new Money(500, "USD")),
        ];

        Assert.Throws<ArgumentException>(
            () => new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries));
    }

    [Fact]
    public void DefaultMoney_Throws()
    {
        Entry[] entries =
        [
            new(Guid.NewGuid(), default),
            new(Guid.NewGuid(), default),
        ];

        Assert.Throws<ArgumentException>(
            () => new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries));
    }
}