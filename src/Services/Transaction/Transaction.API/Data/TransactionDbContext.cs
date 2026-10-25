using Microsoft.EntityFrameworkCore;
using Transaction.API.Entities;

namespace Transaction.API.Data;

public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TransactionCode).IsRequired().HasMaxLength(64);
            entity.HasIndex(t => t.TransactionCode).IsUnique();
            entity.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(t => t.Fee).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(t => t.NetAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(255);
            entity.Property(t => t.SenderPhone).HasMaxLength(20);
            entity.Property(t => t.ReceiverPhone).HasMaxLength(20);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(128);
            entity.Property(t => t.FailureReason).HasMaxLength(500);

            entity.HasIndex(t => t.SenderUserId);
            entity.HasIndex(t => t.ReceiverUserId);
            entity.HasIndex(t => t.IdempotencyKey);
            entity.HasIndex(t => t.CreatedAt);
        });
    }
}
