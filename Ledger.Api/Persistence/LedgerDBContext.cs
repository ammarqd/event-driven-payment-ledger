using Microsoft.EntityFrameworkCore;

namespace Ledger.Api.Persistence;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccountRow> Accounts => Set<AccountRow>();

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
    }
}