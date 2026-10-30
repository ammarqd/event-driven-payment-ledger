namespace Ledger.Api.Persistence;

public class AccountRow
{
    public Guid Id { get; set; }
    public string Currency { get; set; } = "";
    public bool AllowNegative { get; set; }
}