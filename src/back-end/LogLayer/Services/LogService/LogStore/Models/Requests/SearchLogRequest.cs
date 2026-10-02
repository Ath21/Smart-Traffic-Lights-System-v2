using System;

namespace LogStore.Models.Requests;

public class SearchLogRequest
{
    // Optional filters
    public string? Layer { get; set; }      // e.g. "Traffic", "User", "Sensor"
    public string? Service { get; set; }    // e.g. "Intersection Controller Service"
    public string? Type { get; set; }       // e.g. "Audit", "Error", "Failover"

    // Date range
    public DateTime? From { get; set; }     // UTC range start
    public DateTime? To { get; set; }       // UTC range end
}