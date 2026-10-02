using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrafficLightData.Entities;

// Updated by : Traffic Light Coordinator Service
// Read by    : Traffic Light Coordinator, Intersection Controller
[Table("traffic_configurations")]
public class TrafficConfigurationEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ConfigurationId { get; set; }

    [Required, MaxLength(50)]
    public string Mode { get; set; } = "Standard"; // Standard, Peak, Night, Manual, Failover
    public int CycleDurationSec { get; set; } = 60;
    public int GlobalOffsetSec { get; set; } = 0;
  // Serialized JSON field for phase durations
  public string PhaseDurationsJson { get; set; } = "{}";
    public string Purpose { get; set; } = string.Empty; // NEW: Purpose or description of the configuration
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}



/*

{
  "ConfigurationId": 12,
  "IntersectionId": 2,
  "Mode": "Standard",
  "TimePlan": "MorningRush",
  "CycleDurationSec": 60,
  "GlobalOffsetSec": 10,
  "PhaseDurationsJson": "{\"Green\":40, \"Yellow\":5, \"Red\":15}",
  "LightOffsetsJson": "{\"101\":0, \"102\":5, \"103\":10}",
  "LastUpdated": "2025-10-08T07:00:00Z"
}

{
  "ConfigurationId": 13,
  "IntersectionId": 5,
  "Mode": "Peak",
  "TimePlan": "AfternoonPeak",
  "CycleDurationSec": 75,
  "GlobalOffsetSec": 20,
  "PhaseDurationsJson": "{\"Green\":50, \"Yellow\":5, \"Red\":20}",
  "LightOffsetsJson": "{\"501\":0, \"502\":10}",
  "LastUpdated": "2025-10-08T17:00:00Z"
}

{
  "ConfigurationId": 14,
  "IntersectionId": 4,
  "Mode": "Night",
  "TimePlan": "Night",
  "CycleDurationSec": 50,
  "GlobalOffsetSec": 0,
  "PhaseDurationsJson": "{\"Green\":15, \"Yellow\":5, \"Red\":30}",
  "LightOffsetsJson": "{\"201\":0, \"202\":0}",
  "LastUpdated": "2025-10-08T23:00:00Z"
}

*/