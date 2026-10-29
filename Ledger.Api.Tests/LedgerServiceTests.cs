using Ledger.Api.Persistence;
using Ledger.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Tests;

public class LedgerServiceTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public LedgerServiceTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PostAsync_SavesTransactionAndEntries()
    {
        var accountA = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };
        var accountB = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(accountA, accountB);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(accountA.Id, new Money(-1000, "GBP")),
            new(accountB.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(transaction);
        }

        await using var check = _fixture.CreateContext();
        var savedCount = await check.Entries
            .CountAsync(e => e.TransactionId == transaction.Id);

        Assert.Equal(2, savedCount);
    }

    [Fact]
    public async Task PostAsync_UnknownAccount_SavesNothing()
    {
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-1000, "GBP")),
            new(Guid.NewGuid(), new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using (var db = _fixture.CreateContext())
        {
            await Assert.ThrowsAsync<DbUpdateException>(
                () => new LedgerService(db).PostAsync(transaction));
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == transaction.Id);

        Assert.Equal(0, saved);
    }
}