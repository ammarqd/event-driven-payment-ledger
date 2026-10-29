using Ledger.Api.Persistence;
using Ledger.Domain;

namespace Ledger.Api.Tests;

public class TransactionMapperTests
{
    [Fact]
    public void ToRow_CopiesIdAndCreatedAt()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        Entry[] entries =
        [
            new(Guid.NewGuid(), new Money(-1000, "GBP")),
            new(Guid.NewGuid(), new Money(1000, "GBP")),
        ];
        var transaction = new Transaction(id, createdAt, entries);

        var row = TransactionMapper.ToRow(transaction);

        Assert.Equal(id, row.Id);
        Assert.Equal(createdAt, row.CreatedAt);
    }
}