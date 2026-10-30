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
        _db.Transactions.Add(TransactionMapper.ToRow(transaction));
        _db.Entries.AddRange(
            transaction.Entries.Select(e => TransactionMapper.ToEntryRow(transaction.Id, e)));

        await _db.SaveChangesAsync();
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