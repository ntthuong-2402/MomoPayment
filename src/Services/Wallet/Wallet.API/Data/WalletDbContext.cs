using Microsoft.EntityFrameworkCore;
using MomoPayment.Common.Enums;
using Wallet.API.Entities;

namespace Wallet.API.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<WalletAccount> Wallets => Set<WalletAccount>();
    public DbSet<WalletLedger> Ledgers => Set<WalletLedger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WalletAccount>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.UserId).IsUnique();
            entity.HasIndex(w => w.PhoneNumber).IsUnique();
            entity.Property(w => w.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(w => w.Balance).HasPrecision(18, 2).IsRequired();
            entity.Property(w => w.Currency).HasMaxLength(10).HasDefaultValue("VND");
            entity.Property(w => w.RowVersion).IsRowVersion();

            entity.HasMany(w => w.Ledgers)
                .WithOne(l => l.Wallet)
                .HasForeignKey(l => l.WalletId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WalletLedger>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.PreviousBalance).HasPrecision(18, 2).IsRequired();
            entity.Property(l => l.NewBalance).HasPrecision(18, 2).IsRequired();
            entity.Property(l => l.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(l => l.Type).IsRequired().HasMaxLength(20);
            entity.Property(l => l.TransactionCode).IsRequired().HasMaxLength(64);
            entity.Property(l => l.Description).HasMaxLength(255);
            entity.HasIndex(l => l.TransactionCode);
        });

        // Seed initial wallets for Alice, Bob, and Charlie
        var aliceWallet = new WalletAccount
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PhoneNumber = "0901234567",
            Balance = 5000000m, // 5 triệu VND
            Currency = "VND",
            Status = WalletStatus.Active,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var bobWallet = new WalletAccount
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PhoneNumber = "0907654321",
            Balance = 2000000m, // 2 triệu VND
            Currency = "VND",
            Status = WalletStatus.Active,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var charlieWallet = new WalletAccount
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PhoneNumber = "0912345678",
            Balance = 1000000m, // 1 triệu VND
            Currency = "VND",
            Status = WalletStatus.Active,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var aliceLedger = new WalletLedger
        {
            Id = Guid.Parse("10101010-1010-1010-1010-101010101010"),
            WalletId = aliceWallet.Id,
            PreviousBalance = 0m,
            NewBalance = 5000000m,
            Amount = 5000000m,
            Type = "Credit",
            TransactionCode = "GENESIS-INIT-ALICE",
            Description = "Số dư ban đầu khi kích hoạt ví MoMo",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var bobLedger = new WalletLedger
        {
            Id = Guid.Parse("20202020-2020-2020-2020-202020202020"),
            WalletId = bobWallet.Id,
            PreviousBalance = 0m,
            NewBalance = 2000000m,
            Amount = 2000000m,
            Type = "Credit",
            TransactionCode = "GENESIS-INIT-BOB",
            Description = "Số dư ban đầu khi kích hoạt ví MoMo",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var charlieLedger = new WalletLedger
        {
            Id = Guid.Parse("30303030-3030-3030-3030-303030303030"),
            WalletId = charlieWallet.Id,
            PreviousBalance = 0m,
            NewBalance = 1000000m,
            Amount = 1000000m,
            Type = "Credit",
            TransactionCode = "GENESIS-INIT-CHARLIE",
            Description = "Số dư ban đầu khi kích hoạt ví MoMo",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        modelBuilder.Entity<WalletAccount>().HasData(aliceWallet, bobWallet, charlieWallet);
        modelBuilder.Entity<WalletLedger>().HasData(aliceLedger, bobLedger, charlieLedger);
    }
}
