using System;
using LogData.Collections;
using MongoDB.Driver;

namespace LogData.Repositories.Error;

public interface IErrorLogRepository : IBaseLogRepository<ErrorLogCollection>
{
}