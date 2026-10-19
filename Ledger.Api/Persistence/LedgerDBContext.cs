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
    public DbSet<EntryRow> Entries => Set<EntryRow>();

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

        modelBuilder.Entity<EntryRow>(entry =>
        {
            entry.ToTable("entries", table =>
                table.HasCheckConstraint("ck_entries_currency", "currency ~ '^[A-Z]{3}$'"));

            entry.Property(e => e.Id).HasColumnName("id");
            entry.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entry.Property(e => e.AccountId).HasColumnName("account_id");
            entry.Property(e => e.MinorUnits).HasColumnName("minor_units");

            entry.Property(e => e.Currency)
                .HasColumnName("currency")
                .HasColumnType("char(3)");

            entry.HasOne<TransactionRow>()
                .WithMany()
                .HasForeignKey(e => e.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);

            entry.HasOne<AccountRow>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}