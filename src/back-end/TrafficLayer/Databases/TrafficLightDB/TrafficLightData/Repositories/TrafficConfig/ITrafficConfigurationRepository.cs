using System;
using System.Text.Json;
using TrafficLightData.Entities;

namespace TrafficLightData.Repositories.TrafficConfig;

public interface ITrafficConfigurationRepository
{
    Task<TrafficConfigurationEntity?> GetLatestByIntersectionAsync(int configId);
    Task<IEnumerable<TrafficConfigurationEntity>> GetByModeAsync(string mode);
    Task InsertAsync(TrafficConfigurationEntity entity);
}