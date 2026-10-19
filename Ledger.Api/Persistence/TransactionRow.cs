namespace Ledger.Api.Persistence;

public class TransactionRow
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}