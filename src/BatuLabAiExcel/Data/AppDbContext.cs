using Microsoft.EntityFrameworkCore;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Data;

/// <summary>
/// Main database context for the application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
        });

        // License configuration
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            entity.HasIndex(e => e.StripeSubscriptionId);
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Licenses)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.StripePaymentIntentId).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).HasMaxLength(3);
            entity.Property(e => e.StripePaymentIntentId).HasMaxLength(100);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany(e => e.Payments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.License)
                  .WithMany()
                  .HasForeignKey(e => e.LicenseId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Enum configurations
        modelBuilder.Entity<License>()
            .Property(e => e.Type)
            .HasConversion<int>();

        modelBuilder.Entity<License>()
            .Property(e => e.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Payment>()
            .Property(e => e.Status)
            .HasConversion<int>();

        modelBuilder.Entity<Payment>()
            .Property(e => e.LicenseType)
            .HasConversion<int>();
    }
}