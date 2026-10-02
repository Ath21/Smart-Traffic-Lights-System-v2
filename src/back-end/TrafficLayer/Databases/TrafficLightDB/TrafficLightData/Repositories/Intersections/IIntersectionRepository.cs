using TrafficLightData.Entities;

namespace TrafficLightData.Repositories.Intersections;

public interface IIntersectionRepository
{
    Task<IEnumerable<IntersectionEntity>> GetAllActiveAsync();
    Task<IntersectionEntity?> GetByNameAsync(string name);
    Task InsertAsync(IntersectionEntity entity);
}
