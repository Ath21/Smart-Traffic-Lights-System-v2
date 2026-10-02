using LogData;
using LogData.Collections;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LogData.Repositories.Audit;

public class AuditLogRepository : BaseLogRepository<AuditLogCollection>, IAuditLogRepository
{
    public AuditLogRepository(LogDbContext context, ILogger<BaseLogRepository<AuditLogCollection>> logger)
        : base(context.AuditLogs, logger)
    {
    }
}