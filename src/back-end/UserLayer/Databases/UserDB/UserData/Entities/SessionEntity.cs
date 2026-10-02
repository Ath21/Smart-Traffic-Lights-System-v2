using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserData.Entities;

[Table("sessions")]
public class SessionEntity
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public int SessionId { get; set; }

  [Required]
  public int UserId { get; set; }

  [ForeignKey(nameof(UserId))]
  public UserEntity? User { get; set; }

  public string? Session { get; set; }
  public DateTime LoginTime { get; set; }
  public DateTime? LogoutTime { get; set; }
  public bool IsActive { get; set; }
}
