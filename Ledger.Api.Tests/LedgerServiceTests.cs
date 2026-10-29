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
        var savedEntries = await check.Entries
            .Where(e => e.TransactionId == transaction.Id)
            .ToListAsync();

        Assert.Equal(2, savedEntries.Count);
    }
}