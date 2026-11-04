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
        var accountA = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
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
            await Assert.ThrowsAsync<AccountNotFoundException>(
                () => new LedgerService(db).PostAsync(transaction));
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == transaction.Id);

        Assert.Equal(0, saved);
    }

    [Fact]
    public async Task GetBalanceAsync_SumsEntriesPerAccount()
    {
        var accountA = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var accountB = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };
        var accountC = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(accountA, accountB, accountC);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(accountA.Id, new Money(-1000, "GBP")),
            new(accountB.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using var db = _fixture.CreateContext();
        var service = new LedgerService(db);
        await service.PostAsync(transaction);

        Assert.Equal(new Money(-1000, "GBP"), await service.GetBalanceAsync(accountA.Id));
        Assert.Equal(new Money(1000, "GBP"), await service.GetBalanceAsync(accountB.Id));
        Assert.Equal(Money.Zero("GBP"), await service.GetBalanceAsync(accountC.Id));
    }

    [Fact]
    public async Task PostAsync_CurrencyMismatch_SavesNothing()
    {
        var accountA = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };
        var accountB = new AccountRow { Id = Guid.NewGuid(), Currency = "USD" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(accountA, accountB);
            await setup.SaveChangesAsync();
        }

        // Balanced in GBP, so the domain accepts it, but accountB holds USD.
        Entry[] entries =
        [
            new(accountA.Id, new Money(-1000, "GBP")),
            new(accountB.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using (var db = _fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<CurrencyMismatchException>(
                () => new LedgerService(db).PostAsync(transaction));
            Assert.Equal(accountB.Id, ex.AccountId);
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == transaction.Id);

        Assert.Equal(0, saved);
    }

    [Fact]
    public async Task PostAsync_InsufficientFunds_SavesNothing()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        // Deposit 500 so the customer has a real balance.
        Entry[] depositEntries =
        [
            new(system.Id, new Money(-500, "GBP")),
            new(customer.Id, new Money(500, "GBP")),
        ];
        var deposit = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, depositEntries);

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(deposit);
        }

        // Withdraw 600 from a balance of 500.
        Entry[] withdrawEntries =
        [
            new(customer.Id, new Money(-600, "GBP")),
            new(system.Id, new Money(600, "GBP")),
        ];
        var withdrawal = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, withdrawEntries);

        await using (var db = _fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InsufficientFundsException>(
                () => new LedgerService(db).PostAsync(withdrawal));
            Assert.Equal(customer.Id, ex.AccountId);
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == withdrawal.Id);

        Assert.Equal(0, saved);
    }
}