namespace Ledger.Domain;

public sealed class Transaction
{
    public Guid Id { get; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<Entry> Entries { get; }

    public Transaction(Guid id, DateTimeOffset createdAt, IEnumerable<Entry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var list = entries.ToList();

        if (list.Count < 2)
            throw new ArgumentException("A transaction needs at least two entries", nameof(entries));

        if (list.Any(e => e.Amount.Currency is null))
            throw new ArgumentException("Every entry needs a valid amount", nameof(entries));

        foreach (var group in list.GroupBy(e => e.Amount.Currency))
        {
            var total = group.Aggregate(
                Money.Zero(group.Key),
                (sum, entry) => sum + entry.Amount);

            if (total.MinorUnits != 0)
                throw new ArgumentException(
                    $"Entries must sum to zero in {group.Key.Code}", nameof(entries));
        }

        Id = id;
        CreatedAt = createdAt;
        Entries = list.AsReadOnly();
    }
}