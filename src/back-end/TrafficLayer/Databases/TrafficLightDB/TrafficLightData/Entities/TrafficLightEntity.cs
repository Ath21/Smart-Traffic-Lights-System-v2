using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLightData.Entities;

// Updated by : Traffic Light Coordinator Service
// Read by    : Traffic Light Coordinator, Intersection Controller
[Table("traffic_lights")]
public class TrafficLightEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LightId { get; set; }

    [Required]
    public int IntersectionId { get; set; }

    [ForeignKey(nameof(IntersectionId))]
    public IntersectionEntity? Intersection { get; set; }

    [Required, MaxLength(50)]
    public string LightName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Direction { get; set; } = string.Empty;

    // NEW: Coordinates (each light pole location)
    [Column(TypeName = "decimal(10,7)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Longitude { get; set; }

    // Static flag for maintenance registry
    public bool IsOperational { get; set; } = true;
}


/*

{
  "LightId": 101,
  "IntersectionId": 2,
  "LightName": "agiou-spyridonos101",
  "Direction": "North",
  "Latitude": 37.9821201,
  "Longitude": 23.6821348,
  "IsOperational": true
}


*/