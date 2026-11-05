namespace Ledger.Api.Persistence;

public class IdempotencyKeyRow
{
    public string Key { get; set; } = "";
    public Guid TransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}