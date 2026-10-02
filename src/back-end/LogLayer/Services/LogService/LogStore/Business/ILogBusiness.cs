using LogStore.Models;
using LogStore.Models.Responses;

namespace LogStore.Business;

public interface ILogBusiness
{
    Task<IEnumerable<SearchLogResponse>> SearchLogsAsync(
        string? layer,
        string? service,
        string? type,
        DateTime? from,
        DateTime? to);
    
        // NEW: Export methods
    Task<byte[]> ExportLogsToCsvAsync(IEnumerable<object> logs);
}
