using System.Text;
using LogStore.Models.Responses;
using LogData.Repositories.Search;
using LogData.Repositories.Audit;
using LogData.Collections;
using LogData.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace LogStore.Business;

public class LogBusiness : ILogBusiness
{
    private readonly ISearchLogRepository _searchRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<LogBusiness> _logger;
    private const string domain = "[BUSINESS][LOG]";

    public LogBusiness(
        ISearchLogRepository searchRepo,
        IAuditLogRepository auditRepo,
        ILogger<LogBusiness> logger)
    {
        _searchRepo = searchRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    // ============================================================
    // SEARCH METHOD
    // ============================================================
    public async Task<IEnumerable<SearchLogResponse>> SearchLogsAsync(
        string? layer,
        string? service,
        string? type,
        DateTime? from,
        DateTime? to)
    {
        _logger.LogInformation(
            "{Domain} SearchLogsAsync called with layer={Layer}, service={Service}, type={Type}, from={From}, to={To}\n",
            domain, layer, service, type, from, to);

        var results = await _searchRepo.SearchAsync(layer, service, type, from, to);
        _logger.LogInformation("{Domain} Retrieved {Count} logs from repository\n", domain, results.Count());

        var responses = new List<SearchLogResponse>();

        foreach (var obj in results)
        {
            switch (obj)
            {
                case AuditLogCollection audit:
                    responses.Add(MapAuditLog(audit));
                    break;

                case ErrorLogCollection error:
                    responses.Add(MapErrorLog(error));
                    break;

                case FailoverLogCollection failover:
                    responses.Add(MapFailoverLog(failover));
                    break;
            }
        }

        _logger.LogInformation("{Domain} Mapped {Count} results to SearchLogResponse\n", domain, responses.Count);

        // ------------------------------------------------------------
        // AUDIT: record business operation
        // ------------------------------------------------------------
        var auditEntry = new AuditLogCollection
        {
            AuditId = ObjectId.GenerateNewId().ToString(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceLayer = "Log",
            SourceLevel = "Cloud",
            SourceService = "Log",
            SourceDomain = domain + "[SEARCH]",
            Type = "Audit",
            Category = "Business",
            Message = $"Executed search query (layer={layer}, service={service}, type={type}, from={from}, to={to})",
            Operation = "SearchLogsAsync",
            Hostname = Environment.MachineName,
            ContainerIp = Environment.GetEnvironmentVariable("CONTAINER_IP") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Data = new BsonDocument
            {
                { "results_count", responses.Count },
                { "timestamp", DateTime.UtcNow }
            }
        };

        await _auditRepo.InsertAsync(auditEntry);
        _logger.LogInformation("{Domain} Stored business search audit entry ({AuditId})\n", domain, auditEntry.AuditId);

        return responses;
    }

    // ============================================================
    // EXPORT: CSV (Unified Format)
    // ============================================================
    public async Task<byte[]> ExportLogsToCsvAsync(IEnumerable<object> logs)
    {
        _logger.LogInformation("{Domain} ExportLogsToCsvAsync called with {Count} items\n", domain, logs.Count());

        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,SourceLayer,SourceLevel,SourceService,SourceDomain,Type,Category,Operation,Message,Environment");

        foreach (var log in logs)
        {
            var timestamp = log.GetType().GetProperty("Timestamp")?.GetValue(log)?.ToString() ?? "";
            var sourceLayer = log.GetType().GetProperty("SourceLayer")?.GetValue(log)?.ToString() ?? "";
            var sourceLevel = log.GetType().GetProperty("SourceLevel")?.GetValue(log)?.ToString() ?? "";
            var sourceService = log.GetType().GetProperty("SourceService")?.GetValue(log)?.ToString() ?? "";
            var sourceDomain = log.GetType().GetProperty("SourceDomain")?.GetValue(log)?.ToString() ?? "";
            var type = log.GetType().GetProperty("Type")?.GetValue(log)?.ToString() ?? "";
            var category = log.GetType().GetProperty("Category")?.GetValue(log)?.ToString() ?? "";
            var operation = log.GetType().GetProperty("Operation")?.GetValue(log)?.ToString() ?? "";
            var message = log.GetType().GetProperty("Message")?.GetValue(log)?.ToString()?.Replace(",", " ") ?? "";
            var environment = log.GetType().GetProperty("Environment")?.GetValue(log)?.ToString() ?? "";

            sb.AppendLine($"{timestamp},{sourceLayer},{sourceLevel},{sourceService},{sourceDomain},{type},{category},{operation},\"{message}\",{environment}");
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());

        // ------------------------------------------------------------
        // AUDIT: record CSV export operation
        // ------------------------------------------------------------
        var auditEntry = new AuditLogCollection
        {
            AuditId = ObjectId.GenerateNewId().ToString(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceLayer = "Log",
            SourceLevel = "Cloud",
            SourceService = "Log",
            SourceDomain = domain + "[EXPORT]",
            Type = "Audit",
            Category = "Business",
            Message = $"Exported logs to CSV with {logs.Count()} records",
            Operation = "ExportLogsToCsvAsync",
            Hostname = Environment.MachineName,
            ContainerIp = Environment.GetEnvironmentVariable("CONTAINER_IP") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "prod",
            Data = new BsonDocument
            {
                { "record_count", logs.Count() },
                { "timestamp", DateTime.UtcNow }
            }
        };

        await _auditRepo.InsertAsync(auditEntry);
        _logger.LogInformation("[BUSINESS][LOG][AUDIT] Stored CSV export audit entry ({AuditId})\n", auditEntry.AuditId);

        return await Task.FromResult(csvBytes);
    }

    // ============================================================
    // MAPPERS (1:1 with new schema)
    // ============================================================

    private static SearchLogResponse MapAuditLog(AuditLogCollection log)
    {
        return new SearchLogResponse
        {
            LogType = "Audit",
            CorrelationId = log.CorrelationId,
            Timestamp = log.Timestamp,
            Layer = log.SourceLayer ?? string.Empty,
            Level = log.SourceLevel ?? string.Empty,
            Service = log.SourceService ?? string.Empty,
            Domain = log.SourceDomain ?? string.Empty,
            Type = log.Type ?? string.Empty,
            Category = log.Category ?? string.Empty,
            Operation = log.Operation ?? string.Empty,
            Message = log.Message ?? string.Empty,
            Hostname = log.Hostname ?? string.Empty,
            ContainerIp = log.ContainerIp ?? string.Empty,
            Environment = log.Environment ?? string.Empty,
            Data = BsonExtensions.ToDictionary(log.Data)
        };
    }

    private static SearchLogResponse MapErrorLog(ErrorLogCollection log)
    {
        return new SearchLogResponse
        {
            LogType = "Error",
            CorrelationId = log.CorrelationId,
            Timestamp = log.Timestamp,
            Layer = log.SourceLayer ?? string.Empty,
            Level = log.SourceLevel ?? string.Empty,
            Service = log.SourceService ?? string.Empty,
            Domain = log.SourceDomain ?? string.Empty,
            Type = log.Type ?? string.Empty,
            Category = log.Category ?? string.Empty,
            Operation = log.Operation ?? string.Empty,
            Message = log.Message ?? string.Empty,
            Hostname = log.Hostname ?? string.Empty,
            ContainerIp = log.ContainerIp ?? string.Empty,
            Environment = log.Environment ?? string.Empty,
            Data = BsonExtensions.ToDictionary(log.Data)
        };
    }

    private static SearchLogResponse MapFailoverLog(FailoverLogCollection log)
    {
        return new SearchLogResponse
        {
            LogType = "Failover",
            CorrelationId = log.CorrelationId,
            Timestamp = log.Timestamp,
            Layer = log.SourceLayer ?? string.Empty,
            Level = log.SourceLevel ?? string.Empty,
            Service = log.SourceService ?? string.Empty,
            Domain = log.SourceDomain ?? string.Empty,
            Type = log.Type ?? string.Empty,
            Category = log.Category ?? string.Empty,
            Operation = log.Operation ?? string.Empty,
            Message = log.Message ?? string.Empty,
            Hostname = log.Hostname ?? string.Empty,
            ContainerIp = log.ContainerIp ?? string.Empty,
            Environment = log.Environment ?? string.Empty,
            Data = BsonExtensions.ToDictionary(log.Data)
        };
    }
}
