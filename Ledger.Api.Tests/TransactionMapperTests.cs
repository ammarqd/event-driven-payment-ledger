using Ledger.Api.Persistence;
using Ledger.Domain;

namespace Ledger.Api.Tests;

public class TransactionMapperTests
{
    [Fact]
    public void ToRow_CopiesIdAndCreatedAt()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-1000, "GBP")),
            new(Guid.NewGuid(), new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(id, createdAt, entries);

        var row = TransactionMapper.ToRow(transaction);

        Assert.Equal(id, row.Id);
        Assert.Equal(createdAt, row.CreatedAt);
    }

    [Fact]
    public void ToEntryRow_FlattensMoneyIntoTwoColumns()
    {
        var transactionId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var entry = new Entry(accountId, new Money(-1000, "GBP"));

        var row = TransactionMapper.ToEntryRow(transactionId, entry);

        Assert.Equal(transactionId, row.TransactionId);
        Assert.Equal(accountId, row.AccountId);
        Assert.Equal(-1000, row.MinorUnits);
        Assert.Equal("GBP", row.Currency);
    }

    [Fact]
    public void ToEntry_JoinsColumnsBackIntoMoney()
    {
        var accountId = Guid.NewGuid();
        var row = new EntryRow
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            AccountId = accountId,
            MinorUnits = -1000,
            Currency = "GBP",
        };

        var entry = TransactionMapper.ToEntry(row);

        Assert.Equal(new Entry(accountId, new Money(-1000, "GBP")), entry);
    }
}