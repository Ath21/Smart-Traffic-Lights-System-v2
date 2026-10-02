using System;
using LogData.Collections;
using MongoDB.Driver;

namespace LogData.Repositories.Audit;


public interface IAuditLogRepository : IBaseLogRepository<AuditLogCollection>
{
}
