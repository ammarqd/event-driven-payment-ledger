namespace Ledger.Api.Persistence;

using Ledger.Domain;

public class AccountRow
{
    public Guid Id { get; set; }
    public string Currency { get; set; } = "";
    public AccountType Type { get; set; }
}