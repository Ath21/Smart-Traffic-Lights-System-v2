using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficAnalyticsData.Entities;

[Table("alerts")]
public class AlertEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int AlertId { get; set; }

    [Required]
    public int IntersectionId { get; set; }

    [Required, MaxLength(100)]
    public string? Intersection { get; set; }

    [Required, MaxLength(50)]
    public string? Type { get; set; } // Incident, Congestion

    [Required]
    public int Severity { get; set; } // 1-5 scale

    [MaxLength(500)]
    public string? Message { get; set; }

    public DateTime CreatedAt { get; set; }
}
