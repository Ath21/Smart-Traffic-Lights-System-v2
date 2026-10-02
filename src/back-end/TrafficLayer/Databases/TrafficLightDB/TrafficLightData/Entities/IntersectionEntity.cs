using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLightData.Entities;

// Updated by : Traffic Light Coordinator Service
// Read by    : Traffic Light Coordinator, Intersection Controller
[Table("intersections")]
public class IntersectionEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IntersectionId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    // NEW: Geo-coordinates (for mapping & analytics)
    [Column(TypeName = "decimal(10,7)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal Longitude { get; set; }

    [Required]
    public int LightCount { get; set; } = 0;
    public string MappedLightIdsJson { get; set; } = "[]";

    [Required]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Relationships
    public ICollection<TrafficLightEntity> TrafficLights { get; set; } = new List<TrafficLightEntity>();
    public ICollection<TrafficConfigurationEntity> Configurations { get; set; } = new List<TrafficConfigurationEntity>();
}


/*

{
  "IntersectionId": 2,
  "Name": "Agiou Spyridonos",
  "Location": "Agiou Spyridonos & Dimitsanas Street, UNIWA Campus",
  "Latitude": 37.9821543,
  "Longitude": 23.6821921,
  "LightCount": 3,
  "IsActive": true,
  "CreatedAt": "2025-10-08T07:00:00Z",
  "TrafficLights": [
    {
      "LightId": 101,
      "LightName": "agiou-spyridonos101",
      "CurrentState": "Green",
      "DurationSec": 40,
      "Direction": "North",
      "IsOperational": true,
      "LastUpdate": "2025-10-08T07:30:00Z"
    },
    {
      "LightId": 102,
      "LightName": "agiou-spyridonos102",
      "CurrentState": "Red",
      "DurationSec": 40,
      "Direction": "South",
      "IsOperational": true,
      "LastUpdate": "2025-10-08T07:30:00Z"
    },
    {
      "LightId": 103,
      "LightName": "agiou-spyridonos103",
      "CurrentState": "Red",
      "DurationSec": 40,
      "Direction": "West",
      "IsOperational": true,
      "LastUpdate": "2025-10-08T07:30:00Z"
    }
  ],
  "Configurations": [
    {
      "ConfigurationId": 12,
      "Mode": "Standard",
      "TimePlan": "MorningRush",
      "CycleDurationSec": 60,
      "GlobalOffsetSec": 10,
      "PhaseDurationsJson": "{\"Green\":40, \"Yellow\":5, \"Red\":15}",
      "LightOffsetsJson": "{\"101\":0, \"102\":5, \"103\":10}",
      "LastUpdated": "2025-10-08T07:00:00Z"
    }
  ]
}

*/