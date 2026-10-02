using System;
using DetectionData.Collections.Detection;

namespace DetectionData.Repositories.PublicTransport;

public interface IPublicTransportDetectionRepository
{
    Task<IEnumerable<PublicTransportDetectionCollection>> GetRecentPublicTransportsAsync(int intersectionId, int limit = 50);
    Task InsertAsync(PublicTransportDetectionCollection entity);
}