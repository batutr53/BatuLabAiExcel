using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BatuLabAiExcel.WebApi.Models.Entities;

/// <summary>
/// License entity for subscription management
/// </summary>
[Table("Licenses")]
public class License
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public LicenseType Type { get; set; }

    [Required]
    public LicenseStatus Status { get; set; }

    [Required]
    [StringLength(255)]
    public string LicenseKey { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastValidatedAt { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public string? StripeCustomerId { get; set; }

    public decimal PaidAmount { get; set; }

    public string? Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    // Removed UpdatedAt to match existing database schema

    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    [NotMapped]
    public bool IsValid => IsActive && !IsExpired && Status == LicenseStatus.Active;

    [NotMapped]
    public TimeSpan RemainingTime => IsExpired || !ExpiresAt.HasValue ? TimeSpan.Zero : ExpiresAt.Value - DateTime.UtcNow;

    [NotMapped]
    public int RemainingDays => !ExpiresAt.HasValue ? int.MaxValue : (int)Math.Ceiling(RemainingTime.TotalDays);
}

/// <summary>
/// License types available in the system
/// </summary>
public enum LicenseType
{
    Trial = 0,
    Monthly = 1,
    Yearly = 2,
    Lifetime = 3
}

/// <summary>
/// License status enumeration
/// </summary>
public enum LicenseStatus
{
    Pending = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    Suspended = 4
}