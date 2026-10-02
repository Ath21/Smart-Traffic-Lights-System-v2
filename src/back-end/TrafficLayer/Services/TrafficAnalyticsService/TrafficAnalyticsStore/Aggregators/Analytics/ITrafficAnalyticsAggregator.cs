using Messages.Sensor.Count;
using Messages.Sensor.Detection;
using Messages.Traffic.Analytics;

namespace TrafficAnalyticsStore.Aggregators.Analytics;

public interface ITrafficAnalyticsAggregator
{
    void AddVehicleData(VehicleCountMessage msg);
    void AddPedestrianData(PedestrianCountMessage msg);
    void AddCyclistData(CyclistCountMessage msg);

    SummaryAnalyticsMessage? TryGenerateSummary(int intersectionId);
    public double ComputeCongestion(double avgSpeed, double avgWait, int totalVehicles);
    public void AddIncidentDetection(IncidentDetectedMessage msg);
    public void AddEmergencyVehicleDetection(EmergencyVehicleDetectedMessage msg);
    public void AddPublicTransportDetection(PublicTransportDetectedMessage msg);
}
