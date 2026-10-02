using System;
using LogData.Collections;

namespace LogData.Repositories;

public interface IBaseLogRepository<T> where T : BaseLogCollection
{
    Task InsertAsync(T log);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByCorrelationIdAsync(Guid correlationId);
    Task<IEnumerable<T>> GetByLayerAsync(string layer);
    Task<IEnumerable<T>> GetByServiceAsync(string service);
    Task<IEnumerable<T>> GetByDateRangeAsync(DateTime from, DateTime to);
}
