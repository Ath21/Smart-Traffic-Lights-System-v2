using System;
using LogData.Collections;
using MongoDB.Driver;

namespace LogData.Repositories.Failover;

public interface IFailoverLogRepository : IBaseLogRepository<FailoverLogCollection>
{
}
