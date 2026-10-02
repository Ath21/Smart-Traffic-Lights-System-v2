using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogData.Collections;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace LogData.Repositories;

public class BaseLogRepository<T> : IBaseLogRepository<T> where T : BaseLogCollection
{
    private readonly IMongoCollection<T> _collection;
    private readonly ILogger<BaseLogRepository<T>> _logger;
    private const string domain = "[REPOSITORY][BASELOG]";

    protected BaseLogRepository(IMongoCollection<T> collection, ILogger<BaseLogRepository<T>> logger)
    {
        _collection = collection;
        _logger = logger;
    }

    // ------------------------------------------------------------
    // Insert
    // ------------------------------------------------------------
    public async Task InsertAsync(T log)
    {
        _logger.LogInformation("{domain} Inserting log with CorrelationId: {correlationId}\n", domain, log.CorrelationId);
        await _collection.InsertOneAsync(log);
    }

    // ------------------------------------------------------------
    // Basic retrievals
    // ------------------------------------------------------------
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        _logger.LogInformation("{domain} Retrieving all logs", domain);
        return await _collection.Find(Builders<T>.Filter.Empty).ToListAsync();
    }

    public async Task<T?> GetByCorrelationIdAsync(Guid correlationId)
    {
        _logger.LogInformation("{domain} Retrieving log with CorrelationId: {correlationId}\n", domain, correlationId);
        var filter = Builders<T>.Filter.Eq(l => l.CorrelationId, correlationId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    // ------------------------------------------------------------
    // Search by source metadata
    // ------------------------------------------------------------
    public async Task<IEnumerable<T>> GetByLayerAsync(string sourceLayer)
    {
        _logger.LogInformation("{domain} Retrieving logs with SourceLayer: {sourceLayer}\n", domain, sourceLayer);
        var filter = Builders<T>.Filter.Eq(l => l.SourceLayer, sourceLayer);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<T>> GetByServiceAsync(string sourceService)
    {
        _logger.LogInformation("{domain} Retrieving logs with SourceService: {sourceService}\n", domain, sourceService);
        var filter = Builders<T>.Filter.Eq(l => l.SourceService, sourceService);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<T>> GetByLevelAsync(string sourceLevel)
    {
        _logger.LogInformation("{domain} Retrieving logs with SourceLevel: {sourceLevel}\n", domain, sourceLevel);
        var filter = Builders<T>.Filter.Eq(l => l.SourceLevel, sourceLevel);
        return await _collection.Find(filter).ToListAsync();
    }

    // ------------------------------------------------------------
    // Range queries
    // ------------------------------------------------------------
    public async Task<IEnumerable<T>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        _logger.LogInformation("{domain} Retrieving logs from {from} to {to}\n", domain, from, to);
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Gte(l => l.Timestamp, from),
            Builders<T>.Filter.Lte(l => l.Timestamp, to)
        );
        return await _collection.Find(filter).ToListAsync();
    }
}
