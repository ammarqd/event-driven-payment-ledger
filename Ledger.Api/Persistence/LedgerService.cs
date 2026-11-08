using Ledger.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Persistence;

public class LedgerService
{
    private readonly LedgerDbContext _db;

    public LedgerService(LedgerDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAccountAsync(string currency, AccountType type)
    {
        var row = new AccountRow
        {
            Id = Guid.NewGuid(),
            Currency = Currency.FromCode(currency).Code,
            Type = type,
            Balance = 0,
        };

        _db.Accounts.Add(row);
        await _db.SaveChangesAsync();

        return row.Id;
    }

    public async Task<Guid> PostAsync(Transaction transaction, string idempotencyKey)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        var accountIds = transaction.Entries
            .Select(e => e.AccountId)
            .Distinct()
            .ToArray();

        var accounts = await _db.Accounts
            .FromSql($"SELECT * FROM accounts WHERE id = ANY({accountIds}) ORDER BY id FOR UPDATE")
            .ToDictionaryAsync(a => a.Id);

        var existing = await _db.IdempotencyKeys
            .SingleOrDefaultAsync(k => k.Key == idempotencyKey);

        if (existing is not null) 
            return existing.TransactionId;

        foreach (var id in accountIds)
        {
            if (!accounts.ContainsKey(id))
                throw new AccountNotFoundException(id);
        }

        foreach (var entry in transaction.Entries)
        {
            var account = accounts[entry.AccountId];
            var entryCurrency = entry.Amount.Currency.Code;

            if (entryCurrency != account.Currency)
                throw new CurrencyMismatchException(
                    entry.AccountId, account.Currency, entryCurrency);
        }

        var changes = transaction.Entries
            .GroupBy(e => e.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount.MinorUnits));

        foreach (var (id, change) in changes)
        {
            var account = accounts[id];
            if (account.Type == AccountType.System)
                continue;

            if (account.Balance + change < 0)
                throw new InsufficientFundsException(id);
        }

        foreach (var (id, change) in changes)
        {
            accounts[id].Balance += change;
        }

        _db.Transactions.Add(TransactionMapper.ToRow(transaction));
        _db.Entries.AddRange(
            transaction.Entries.Select(e => TransactionMapper.ToEntryRow(transaction.Id, e)));
        _db.IdempotencyKeys.Add(new IdempotencyKeyRow
        {
            Key = idempotencyKey,
            TransactionId = transaction.Id,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        return transaction.Id;
    }
    
    public async Task<Money> GetBalanceAsync(Guid accountId)
    {
        var accountData = await _db.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => new { a.Balance, a.Currency })
            .SingleAsync();

        return new Money(accountData.Balance, Currency.FromCode(accountData.Currency));
    }
}