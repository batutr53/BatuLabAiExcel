using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BatuLabAiExcel.Models.Entities;

/// <summary>
/// User entity for authentication and license management
/// </summary>
[Table("Users")]
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}";

    [NotMapped]
    public License? ActiveLicense => Licenses?.FirstOrDefault(l => l.IsActive && l.ExpiresAt > DateTime.UtcNow);
}