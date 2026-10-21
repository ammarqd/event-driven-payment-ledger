using Ledger.Domain;

namespace Ledger.Api.Persistence;

public static class TransactionMapper
{
    public static TransactionRow ToRow(Transaction transaction) => new()
    {
        Id = transaction.Id,
        CreatedAt = transaction.CreatedAt,
    };

    public static EntryRow ToEntryRow(Guid transactionId, Entry entry) => new()
    {
        TransactionId = transactionId,
        AccountId = entry.AccountId,
        MinorUnits = entry.Amount.MinorUnits,
        Currency = entry.Amount.Currency,
    };
}