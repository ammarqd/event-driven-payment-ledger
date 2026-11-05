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
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(system.Id, new Money(-1000, "GBP")),
            new(customer.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(transaction, Guid.NewGuid().ToString());
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
                () => new LedgerService(db).PostAsync(transaction, Guid.NewGuid().ToString()));
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == transaction.Id);

        Assert.Equal(0, saved);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsBalancePerAccount()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };
        var idle = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer, idle);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(system.Id, new Money(-1000, "GBP")),
            new(customer.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

        await using var db = _fixture.CreateContext();
        var service = new LedgerService(db);
        await service.PostAsync(transaction, Guid.NewGuid().ToString());

        Assert.Equal(new Money(-1000, "GBP"), await service.GetBalanceAsync(system.Id));
        Assert.Equal(new Money(1000, "GBP"), await service.GetBalanceAsync(customer.Id));
        Assert.Equal(Money.Zero("GBP"), await service.GetBalanceAsync(idle.Id));
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
                () => new LedgerService(db).PostAsync(transaction, Guid.NewGuid().ToString()));
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
            await new LedgerService(db).PostAsync(deposit, Guid.NewGuid().ToString());
        }

        // Withdraw 600 from a balance of 500.
        Entry[] withdrawalEntries =
        [
            new(customer.Id, new Money(-600, "GBP")),
            new(system.Id, new Money(600, "GBP")),
        ];
        var withdrawal = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, withdrawalEntries);

        await using (var db = _fixture.CreateContext())
        {
            var ex = await Assert.ThrowsAsync<InsufficientFundsException>(
                () => new LedgerService(db).PostAsync(withdrawal, Guid.NewGuid().ToString()));
            Assert.Equal(customer.Id, ex.AccountId);
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.Transactions.CountAsync(t => t.Id == withdrawal.Id);

        Assert.Equal(0, saved);
    }

    [Fact]
    public async Task PostAsync_UpdatesStoredBalances()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] depositEntries =
        [
            new(system.Id, new Money(-500, "GBP")),
            new(customer.Id, new Money(500, "GBP")),
        ];
        var deposit = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, depositEntries);

        Entry[] withdrawalEntries =
        [
            new(customer.Id, new Money(-300, "GBP")),
            new(system.Id, new Money(300, "GBP")),
        ];
        var withdrawal = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, withdrawalEntries);

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(deposit, Guid.NewGuid().ToString());
        }

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(withdrawal, Guid.NewGuid().ToString());
        }

        await using var check = _fixture.CreateContext();
        var customerRow = await check.Accounts.SingleAsync(a => a.Id == customer.Id);
        var systemRow = await check.Accounts.SingleAsync(a => a.Id == system.Id);

        Assert.Equal(200, customerRow.Balance);
        Assert.Equal(-200, systemRow.Balance);

        var customerSum = await check.Entries
            .Where(e => e.AccountId == customer.Id)
            .SumAsync(e => e.MinorUnits);
        Assert.Equal(customerSum, customerRow.Balance);
    }

    [Fact]
    public async Task PostAsync_SavesIdempotencyKey()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(system.Id, new Money(-1000, "GBP")),
            new(customer.Id, new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);
        var key = Guid.NewGuid().ToString();

        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(transaction, key);
        }

        await using var check = _fixture.CreateContext();
        var saved = await check.IdempotencyKeys.SingleAsync(k => k.Key == key);

        Assert.Equal(transaction.Id, saved.TransactionId);
    }

    [Fact]
    public async Task PostAsync_SameKeyTwice_PostsOnce()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(system.Id, new Money(-1000, "GBP")),
            new(customer.Id, new Money(1000, "GBP")),
        ];
        var first = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);
        var retry = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);
        var key = Guid.NewGuid().ToString();

        Guid firstResult;
        await using (var db = _fixture.CreateContext())
        {
            firstResult = await new LedgerService(db).PostAsync(first, key);
        }

        Guid retryResult;
        await using (var db = _fixture.CreateContext())
        {
            retryResult = await new LedgerService(db).PostAsync(retry, key);
        }

        Assert.Equal(first.Id, firstResult);
        Assert.Equal(first.Id, retryResult);

        await using var check = _fixture.CreateContext();
        Assert.Equal(0, await check.Transactions.CountAsync(t => t.Id == retry.Id));
        Assert.Equal(1000, (await check.Accounts.SingleAsync(a => a.Id == customer.Id)).Balance);
    }

    [Fact]
    public async Task PostAsync_SameKeyConcurrently_PostsOnce()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] entries =
        [
            new(system.Id, new Money(-1000, "GBP")),
            new(customer.Id, new Money(1000, "GBP")),
        ];
        var key = Guid.NewGuid().ToString();

        var requests = Enumerable.Range(0, 10).Select(async _ =>
        {
            var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);
            await using var db = _fixture.CreateContext();
            return await new LedgerService(db).PostAsync(transaction, key);
        });

        var results = await Task.WhenAll(requests);

        Assert.Single(results.Distinct());

        await using var check = _fixture.CreateContext();
        Assert.Equal(1, await check.Entries.CountAsync(e => e.AccountId == customer.Id));
        Assert.Equal(1000, (await check.Accounts.SingleAsync(a => a.Id == customer.Id)).Balance);
    }

    [Fact]
    public async Task PostAsync_ParallelWithdrawals_NeverOverdraw()
    {
        var system = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP", Type = AccountType.System };
        var customer = new AccountRow { Id = Guid.NewGuid(), Currency = "GBP" };

        await using (var setup = _fixture.CreateContext())
        {
            setup.Accounts.AddRange(system, customer);
            await setup.SaveChangesAsync();
        }

        Entry[] depositEntries =
        [
            new(system.Id, new Money(-100, "GBP")),
            new(customer.Id, new Money(100, "GBP")),
        ];
        
        await using (var db = _fixture.CreateContext())
        {
            await new LedgerService(db).PostAsync(
                new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, depositEntries),
                Guid.NewGuid().ToString());
        }

        var requests = Enumerable.Range(0, 50).Select(async _ =>
        {
            Entry[] entries =
            [
                new(customer.Id, new Money(-10, "GBP")),
                new(system.Id, new Money(10, "GBP")),
            ];
            var transaction = new Transaction(Guid.NewGuid(), DateTimeOffset.UtcNow, entries);

            await using var db = _fixture.CreateContext();
            try
            {
                await new LedgerService(db).PostAsync(transaction, Guid.NewGuid().ToString());
                return true;
            }
            catch (InsufficientFundsException)
            {
                return false;
            }
        });

        var outcomes = await Task.WhenAll(requests);

        Assert.Equal(10, outcomes.Count(o => o));
        Assert.Equal(40, outcomes.Count(o => !o));

        await using var check = _fixture.CreateContext();
        var customerRow = await check.Accounts.SingleAsync(a => a.Id == customer.Id);
        Assert.Equal(0, customerRow.Balance);
    }
}