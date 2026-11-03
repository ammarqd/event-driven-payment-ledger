namespace Ledger.Domain;

public class InsufficientFundsException : Exception
{
    public Guid AccountId { get; }

    public InsufficientFundsException(Guid accountId)
        : base($"Account {accountId} does not have enough funds.")
    {
        AccountId = accountId;
    }
}