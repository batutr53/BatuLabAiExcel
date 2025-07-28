using Microsoft.EntityFrameworkCore;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Data;

/// <summary>
/// Application database context
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // FullName is NotMapped, so we ignore it in EF configuration
            entity.Ignore(e => e.FullName);
            entity.Ignore(e => e.ActiveLicense);
        });

        // License configuration
        modelBuilder.Entity<License>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.LicenseKey).IsUnique();
            entity.Property(e => e.LicenseKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(255);
            
            // Ignore properties that don't exist in current database schema
            entity.Ignore(e => e.CancellationReason);
            entity.Ignore(e => e.CancelledAt);
            entity.Ignore(e => e.Notes);

            // Foreign key relationship
            entity.HasOne(l => l.User)
                  .WithMany(u => u.Licenses)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StripePaymentIntentId).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Amount).IsRequired().HasPrecision(18, 2);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.LicenseType).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();

            // Foreign key relationships
            entity.HasOne(p => p.User)
                  .WithMany(u => u.Payments)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.License)
                  .WithMany()
                  .HasForeignKey(p => p.LicenseId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}