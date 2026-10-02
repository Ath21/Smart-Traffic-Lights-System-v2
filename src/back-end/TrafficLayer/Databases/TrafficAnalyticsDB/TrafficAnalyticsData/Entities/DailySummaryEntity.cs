using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficAnalyticsData.Entities;

[Table("daily_summaries")]
public class DailySummaryEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SummaryId { get; set; }

    [Required]
    public int IntersectionId { get; set; }

    [Required]
    public string? Intersection { get; set; } 

    [Required]
    public DateTime Date { get; set; } 

    public int TotalVehicles { get; set; }
    public int TotalPedestrians { get; set; }
    public int TotalCyclists { get; set; }
    public double AverageSpeedKmh { get; set; }
    public double AverageWaitTimeSec { get; set; }
}

