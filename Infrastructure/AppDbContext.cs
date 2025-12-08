using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Bankweave.Entities;

namespace Bankweave.Infrastructure;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<MoneyMovement> MoneyMovements => Set<MoneyMovement>();
    public DbSet<BalanceSnapshot> BalanceSnapshots => Set<BalanceSnapshot>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();
    public DbSet<WhatIfScenario> WhatIfScenarios => Set<WhatIfScenario>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Required for Identity

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasMany(u => u.FinancialAccounts)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.CategorizationRules)
                  .WithOne(r => r.User)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.WhatIfScenarios)
                  .WithOne(s => s.User)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FinancialAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CurrentBalance).HasPrecision(18, 2);
            entity.HasIndex(e => e.ExternalId);
            entity.HasIndex(e => e.RequisitionId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<MoneyMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.TransactionDate);
            entity.HasOne(e => e.Account)
                  .WithMany(a => a.Movements)
                  .HasForeignKey(e => e.FinancialAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<BalanceSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalBalance).HasPrecision(18, 2);
            entity.HasIndex(e => e.Timestamp);
        });

        builder.Entity<CategorizationRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        builder.Entity<WhatIfScenario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SavedDate);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
