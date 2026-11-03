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

        var found = await _db.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync();

        foreach (var id in accountIds)
        {
            if (!found.Contains(id))
                throw new AccountNotFoundException(id);
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