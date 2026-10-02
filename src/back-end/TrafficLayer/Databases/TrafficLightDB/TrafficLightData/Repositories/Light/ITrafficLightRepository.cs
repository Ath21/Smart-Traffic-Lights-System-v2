using System;
using TrafficLightData.Entities;


namespace TrafficLightData.Repositories.Light;

public interface ITrafficLightRepository
{
    Task<IEnumerable<TrafficLightEntity>> GetByIntersectionAsync(int intersectionId);
    Task InsertAsync(TrafficLightEntity entity);
    Task UpdateStatusAsync(int lightId, bool isOperational);
}

