namespace Ledger.Domain;

public class CurrencyMismatchException : Exception
{
    public Guid AccountId { get; }

    public CurrencyMismatchException(Guid accountId, string accountCurrency, string entryCurrency)
        : base($"Account {accountId} holds {accountCurrency}, but the entry is in {entryCurrency}.")
    {
        AccountId = accountId;
    }
}