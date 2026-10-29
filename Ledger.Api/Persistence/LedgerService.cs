using Ledger.Domain;

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
}