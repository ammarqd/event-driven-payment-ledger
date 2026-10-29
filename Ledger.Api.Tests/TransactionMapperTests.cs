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

    [Fact]
    public void ToTransaction_RebuildsTransactionFromRows()
    {
        var transactionId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        var transactionRow = new TransactionRow { Id = transactionId, CreatedAt = createdAt };
        EntryRow[] entryRows =
        [
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, AccountId = accountA, MinorUnits = -1000, Currency = "GBP" },
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, AccountId = accountB, MinorUnits = 1000, Currency = "GBP" },
        ];

        var transaction = TransactionMapper.ToTransaction(transactionRow, entryRows);

        Assert.Equal(transactionId, transaction.Id);
        Assert.Equal(createdAt, transaction.CreatedAt);
        Assert.Equal(2, transaction.Entries.Count);
        Assert.Equal(new Entry(accountA, new Money(-1000, "GBP")), transaction.Entries[0]);
        Assert.Equal(new Entry(accountB, new Money(1000, "GBP")), transaction.Entries[1]);
    }

    [Fact]
    public void ToTransaction_UnbalancedRows_Throws()
    {
        var transactionId = Guid.NewGuid();
        var transactionRow = new TransactionRow { Id = transactionId, CreatedAt = DateTimeOffset.UtcNow };
        EntryRow[] entryRows =
        [
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, AccountId = Guid.NewGuid(), MinorUnits = -1000, Currency = "GBP" },
            new() { Id = Guid.NewGuid(), TransactionId = transactionId, AccountId = Guid.NewGuid(), MinorUnits = 900, Currency = "GBP" },
        ];

        Assert.Throws<ArgumentException>(
            () => TransactionMapper.ToTransaction(transactionRow, entryRows));
    }
}