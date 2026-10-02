using Messages.Sensor.Count;
using Messages.Sensor.Detection;
using Messages.Traffic.Analytics;
using System.Collections.Concurrent;

namespace TrafficAnalyticsStore.Aggregators.Analytics;

public class TrafficAnalyticsAggregator : ITrafficAnalyticsAggregator
{
    private class IntersectionBuffer
    {
        public int TotalVehicles;
        public int TotalPedestrians;
        public int TotalCyclists;
        public int IncidentsDetected;
        public int EmergencyVehiclesDetected;
        public int PublicTransportDetected;
        public List<double> CongestionLevels = new();
        public DateTime LastUpdate = DateTime.UtcNow;
    }

    // Thread-safe dictionary per intersection
    private readonly ConcurrentDictionary<int, IntersectionBuffer> _buffers = new();

    public void AddVehicleData(VehicleCountMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.TotalVehicles += msg.CountTotal;
        buffer.CongestionLevels.Add(ComputeCongestion(msg.AverageSpeedKmh, msg.AverageWaitTimeSec, msg.CountTotal));
        buffer.LastUpdate = DateTime.UtcNow;
    }

    public void AddPedestrianData(PedestrianCountMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.TotalPedestrians += msg.Count;
        buffer.LastUpdate = DateTime.UtcNow;
    }

    public void AddCyclistData(CyclistCountMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.TotalCyclists += msg.Count;
        buffer.LastUpdate = DateTime.UtcNow;
    }

    public SummaryAnalyticsMessage? TryGenerateSummary(int intersectionId)
    {
        if (!_buffers.TryGetValue(intersectionId, out var buffer))
            return null;

        // Optional: only generate summary if X seconds passed or threshold reached
        if ((DateTime.UtcNow - buffer.LastUpdate).TotalSeconds < 5) 
            return null;

        var avgCongestion = buffer.CongestionLevels.Any() ? buffer.CongestionLevels.Average() : 0.0;

        var summary = new SummaryAnalyticsMessage
        {
            Intersection = intersectionId.ToString(),
            VehicleCount = buffer.TotalVehicles,
            PedestrianCount = buffer.TotalPedestrians,
            CyclistCount = buffer.TotalCyclists,
            IncidentsDetected = buffer.IncidentsDetected,
            AverageCongestion = avgCongestion,
            GeneratedAt = DateTime.UtcNow
        };

        // Reset buffer after generating summary
        buffer.TotalVehicles = 0;
        buffer.TotalPedestrians = 0;
        buffer.TotalCyclists = 0;
        buffer.CongestionLevels.Clear();
        buffer.LastUpdate = DateTime.UtcNow;

        return summary;
    }

    public double ComputeCongestion(double avgSpeed, double avgWait, int totalVehicles)
    {
        double speedFactor = avgSpeed <= 10 ? 1.0 : 1.0 - (avgSpeed / 100.0);
        double waitFactor = Math.Min(avgWait / 60.0, 1.0);
        double volumeFactor = Math.Min(totalVehicles / 2000.0, 1.0);
        return Math.Clamp((speedFactor + waitFactor + volumeFactor) / 3.0, 0.0, 1.0);
    }

    public void AddIncidentDetection(IncidentDetectedMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.IncidentsDetected++;
        buffer.LastUpdate = DateTime.UtcNow;
    }

    public void AddEmergencyVehicleDetection(EmergencyVehicleDetectedMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.EmergencyVehiclesDetected++;
        buffer.LastUpdate = DateTime.UtcNow;
    }

    public void AddPublicTransportDetection(PublicTransportDetectedMessage msg)
    {
        var buffer = _buffers.GetOrAdd(msg.IntersectionId, _ => new IntersectionBuffer());
        buffer.PublicTransportDetected++;
        buffer.LastUpdate = DateTime.UtcNow;
    }
}
