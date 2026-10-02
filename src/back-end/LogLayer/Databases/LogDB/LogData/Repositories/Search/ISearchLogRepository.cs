using System;
using LogData.Collections;

namespace LogData.Repositories.Search;

public interface ISearchLogRepository
{
    Task<IEnumerable<BaseLogCollection>> SearchAsync(
        string? layer,
        string? service,
        string? type,
        DateTime? from,
        DateTime? to);
}
