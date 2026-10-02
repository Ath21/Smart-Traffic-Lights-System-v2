using LogData;
using LogData.Collections;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LogData.Repositories.Error;

public class ErrorLogRepository : BaseLogRepository<ErrorLogCollection>, IErrorLogRepository
{
    public ErrorLogRepository(LogDbContext context, ILogger<BaseLogRepository<ErrorLogCollection>> logger)
        : base(context.ErrorLogs, logger)
    {
    }
}