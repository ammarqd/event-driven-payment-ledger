using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Persistence;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<TransactionRow> Transactions => Set<TransactionRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountRow>(account =>
        {
            account.ToTable("accounts", table =>
                table.HasCheckConstraint("ck_accounts_currency", "currency ~ '^[A-Z]{3}$'"));

            account.Property(a => a.Id).HasColumnName("id");

            account.Property(a => a.Currency)
                .HasColumnName("currency")
                .HasColumnType("char(3)");
        });

        modelBuilder.Entity<TransactionRow>(transaction =>
        {
            transaction.ToTable("transactions");

            transaction.Property(t => t.Id).HasColumnName("id");
            transaction.Property(t => t.CreatedAt).HasColumnName("created_at");
        });
    }
}