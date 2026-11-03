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
            .OrderBy(id => id)
            .ToList();

        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
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

        _db.Transactions.Add(TransactionMapper.ToRow(transaction));
        _db.Entries.AddRange(
            transaction.Entries.Select(e => TransactionMapper.ToEntryRow(transaction.Id, e)));

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();
    }
    
    public async Task<Money> GetBalanceAsync(Guid accountId)
    {
        var currency = await _db.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => a.Currency)
            .SingleAsync();

        var total = await _db.Entries
            .Where(e => e.AccountId == accountId)
            .SumAsync(e => e.MinorUnits);

        return new Money(total, currency);
    }

}