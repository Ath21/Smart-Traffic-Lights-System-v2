using System.Net;
using System.Net.Sockets;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Messages.Log;

namespace TrafficAnalytics.Publishers.Logs;

public class AnalyticsLogPublisher : IAnalyticsLogPublisher
{
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<AnalyticsLogPublisher> _logger;
    private readonly string _routingPattern;
    private readonly string _exchangeName;

    private const string Tag = "[PUBLISHER][LOG]";

    // ============================================================
    // Cached environment context
    // ============================================================
    private readonly string _layer;
    private readonly string _level;
    private readonly string _service;
    private readonly string _environment;
    private readonly string _hostname;
    private readonly string _containerIp;

    public AnalyticsLogPublisher(
        IPublishEndpoint publisher,
        IConfiguration configuration,
        ILogger<AnalyticsLogPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;

        _exchangeName = configuration["RabbitMQ:Exchanges:Log"] ?? "LOG.EXCHANGE";
        _routingPattern = configuration["RabbitMQ:RoutingKeys:Log:TrafficAnalytics"]
                          ?? "log.{layer}.{service}.{type}";

        // ============================================================
        // Environment-based service identity
        // ============================================================
        _layer = Environment.GetEnvironmentVariable("SERVICE_LAYER") ?? "Traffic";
        _level = Environment.GetEnvironmentVariable("SERVICE_LEVEL") ?? "Cloud";
        _service = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "Traffic Analytics";
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        _hostname = Environment.MachineName;
        _containerIp = Dns.GetHostAddresses(Dns.GetHostName())
            .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            ?.ToString() ?? "unknown";
    }

    // ============================================================
    // AUDIT LOG
    // ============================================================
    public async Task PublishAuditAsync(
        string domain,
        string messageText,
        string? category = "system",
        Dictionary<string, object>? data = null,
        string? operation = null)
    {
        var routingKey = _routingPattern
            .Replace("{layer}", _layer.ToLower())
            .Replace("{service}", _service.ToLower().Replace(' ', '-'))
            .Replace("{type}", "audit");

        var msg = new LogMessage
        {
            Layer = _layer,
            Level = _level,
            Service = _service,
            Domain = domain,
            Type = "audit",
            Category = category ?? "system",
            Message = messageText,
            Operation = operation,
            Hostname = _hostname,
            ContainerIp = _containerIp,
            Environment = _environment,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        try
        {
            await _publisher.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));
            _logger.LogInformation("{Tag} Published AUDIT log | Domain={Domain} | Message={Message}", Tag, domain, messageText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Tag} Failed to publish AUDIT log | Domain={Domain} | Error={Error}", Tag, domain, ex.Message);
        }
    }

    // ============================================================
    // ERROR LOG
    // ============================================================
    public async Task PublishErrorAsync(
        string domain,
        string messageText,
        Dictionary<string, object>? data = null,
        string? operation = null)
    {
        var routingKey = _routingPattern
            .Replace("{layer}", _layer.ToLower())
            .Replace("{service}", _service.ToLower().Replace(' ', '-'))
            .Replace("{type}", "error");

        var msg = new LogMessage
        {
            Layer = _layer,
            Level = _level,
            Service = _service,
            Domain = domain,
            Type = "error",
            Category = "Error",
            Message = messageText,
            Operation = operation,
            Hostname = _hostname,
            ContainerIp = _containerIp,
            Environment = _environment,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        try
        {
            await _publisher.Publish(msg, ctx => ctx.SetRoutingKey(routingKey));
            _logger.LogWarning("{Tag} Published ERROR log | Domain={Domain} | Message={Message}", Tag, domain, messageText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Tag} Failed to publish ERROR log | Domain={Domain} | Error={Error}", Tag, domain, ex.Message);
        }
    }
}
