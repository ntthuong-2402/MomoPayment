using Identity.API.Entities;
using Microsoft.EntityFrameworkCore;
using MomoPayment.Common.Security;

namespace Identity.API.Data;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.HasIndex(u => u.PhoneNumber).IsUnique();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50);
        });

        // Seed initial MoMo test users
        var defaultPasswordHash = PasswordHasher.HashPassword("Password123@");

        var userAlice = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PhoneNumber = "0901234567",
            FullName = "Nguyễn Văn An",
            Email = "an.nguyen@example.com",
            PasswordHash = defaultPasswordHash,
            Role = "User",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var userBob = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PhoneNumber = "0907654321",
            FullName = "Trần Thị Bình",
            Email = "binh.tran@example.com",
            PasswordHash = defaultPasswordHash,
            Role = "User",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var userCharlie = new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PhoneNumber = "0912345678",
            FullName = "Lê Văn Cường",
            Email = "cuong.le@example.com",
            PasswordHash = defaultPasswordHash,
            Role = "User",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        modelBuilder.Entity<User>().HasData(userAlice, userBob, userCharlie);
    }
}
