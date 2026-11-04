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

    public async Task PostAsync(Transaction transaction)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();

        var accountIds = transaction.Entries
            .Select(e => e.AccountId)
            .Distinct()
            .ToArray();

        var accounts = await _db.Accounts
            .FromSql($"SELECT * FROM accounts WHERE id = ANY({accountIds}) ORDER BY id FOR UPDATE")
            .ToDictionaryAsync(a => a.Id);

        foreach (var id in accountIds)
        {
            if (!accounts.ContainsKey(id))
                throw new AccountNotFoundException(id);
        }

        foreach (var entry in transaction.Entries)
        {
            var account = accounts[entry.AccountId];
            if (entry.Amount.Currency != account.Currency)
                throw new CurrencyMismatchException(
                    entry.AccountId, account.Currency, entry.Amount.Currency);
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

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }
    
    public async Task<Money> GetBalanceAsync(Guid accountId)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .SingleAsync(a => a.Id == accountId);

        return new Money(account.Balance, account.Currency);
    }
}