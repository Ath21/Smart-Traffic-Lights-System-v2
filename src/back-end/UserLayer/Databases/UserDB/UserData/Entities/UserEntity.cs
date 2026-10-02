using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserData.Entities;

[Table("users")]
public class UserEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public string? Username { get; set; }

    [Required, MaxLength(100)]
    public string? Email { get; set; }

    [Required]
    public string? PasswordHash { get; set; }

    [MaxLength(20)]
    public string? Role { get; set; } // = "User", "Admin", "TrafficOperator";

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<SessionEntity>? Sessions { get; set; } 
    public ICollection<UserAuditEntity>? Audits { get; set; }
}
