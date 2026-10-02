using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserData.Entities;

[Table("user_audits")]
public class UserAuditEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AuditId { get; set; } 

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Required, MaxLength(200)]
    public string? Action { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }
}