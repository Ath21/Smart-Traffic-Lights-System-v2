using LogData;
using LogData.Collections;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LogData.Repositories.Failover;

public class FailoverLogRepository : BaseLogRepository<FailoverLogCollection>, IFailoverLogRepository
{
    public FailoverLogRepository(LogDbContext context, ILogger<BaseLogRepository<FailoverLogCollection>> logger)
        : base(context.FailoverLogs, logger)
    {
    }
}