using Ledger.Domain;

namespace Ledger.Api.Persistence;

public static class TransactionMapper
{
    public static TransactionRow ToRow(Transaction transaction) => new()
    {
        Id = transaction.Id,
        CreatedAt = transaction.CreatedAt,
    };
}