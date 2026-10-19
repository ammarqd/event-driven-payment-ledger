namespace Ledger.Api.Persistence;

public class EntryRow
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public long MinorUnits { get; set; }
    public string Currency { get; set; } = "";
}