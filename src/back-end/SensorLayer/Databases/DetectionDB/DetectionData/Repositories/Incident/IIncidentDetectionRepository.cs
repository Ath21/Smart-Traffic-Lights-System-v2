using System;
using DetectionData.Collections.Detection;

namespace DetectionData.Repositories.Incident;

public interface IIncidentDetectionRepository
{
    Task<IEnumerable<IncidentDetectionCollection>> GetRecentIncidentsAsync(int intersectionId, int limit = 20);
    Task InsertAsync(IncidentDetectionCollection entity);
}