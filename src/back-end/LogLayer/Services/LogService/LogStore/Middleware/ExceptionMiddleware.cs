using System.Net;
using System.Text.Json;
using MongoDB.Driver;
using RabbitMQ.Client.Exceptions;
using LogData.Collections;
using LogData.Repositories.Error;
using MongoDB.Bson;
using MassTransit;

namespace LogStore.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private const string domain = "[MIDDLEWARE][EXCEPTION]";

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogRepository errorRepo)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // =====================================================
            // Classification and Mapping
            // =====================================================
            var (statusCode, userMessage, errorType) = ex switch
            {
                UnauthorizedAccessException      => (HttpStatusCode.Unauthorized, "Unauthorized access", "AUTH_ERROR"),
                KeyNotFoundException             => (HttpStatusCode.NotFound, "Resource not found", "NOT_FOUND"),
                InvalidOperationException        => (HttpStatusCode.BadRequest, "Invalid operation", "INVALID_OPERATION"),
                ArgumentNullException            => (HttpStatusCode.BadRequest, "Missing required parameter", "ARGUMENT_NULL"),
                ArgumentException                => (HttpStatusCode.BadRequest, "Invalid parameter", "ARGUMENT_ERROR"),
                FormatException                  => (HttpStatusCode.BadRequest, "Invalid data format", "FORMAT_ERROR"),
                JsonException                    => (HttpStatusCode.BadRequest, "Invalid JSON payload", "JSON_ERROR"),
                InvalidCastException             => (HttpStatusCode.BadRequest, "Invalid type conversion", "CAST_ERROR"),
                TimeoutException                 => (HttpStatusCode.RequestTimeout, "Operation timed out", "TIMEOUT"),
                HttpRequestException             => (HttpStatusCode.BadGateway, "External service unavailable", "HTTP_REQUEST_ERROR"),
                TaskCanceledException            => (HttpStatusCode.RequestTimeout, "Operation canceled or timed out", "TASK_CANCELED"),
                RequestTimeoutException          => (HttpStatusCode.GatewayTimeout, "Message broker timeout", "BROKER_TIMEOUT"),
                BrokerUnreachableException       => (HttpStatusCode.BadGateway, "Message broker unreachable", "BROKER_UNREACHABLE"),
                OperationInterruptedException    => (HttpStatusCode.ServiceUnavailable, "Message broker interrupted operation", "BROKER_INTERRUPTED"),
                MongoAuthenticationException     => (HttpStatusCode.Unauthorized, "MongoDB authentication failed", "DB_AUTH_ERROR"),
                MongoConnectionException         => (HttpStatusCode.ServiceUnavailable, "MongoDB connection failure", "DB_CONN_ERROR"),
                MongoWriteException              => (HttpStatusCode.Conflict, "MongoDB write conflict", "DB_WRITE_ERROR"),
                MongoException                   => (HttpStatusCode.InternalServerError, "MongoDB error", "DB_ERROR"),
                _                                => (HttpStatusCode.InternalServerError, "An unexpected error occurred", "UNEXPECTED")
            };

            await HandleErrorAsync(context, errorRepo, statusCode, userMessage, errorType, ex);
        }
    }

    private async Task HandleErrorAsync(
        HttpContext context,
        IErrorLogRepository errorRepo,
        HttpStatusCode statusCode,
        string userMessage,
        string errorType,
        Exception ex)
    {
        _logger.LogError(ex, "{Domain} {ErrorType}: {UserMessage}\n", domain, errorType, userMessage);

        // ============================================================
        // Structured error payload (for MongoDB)
        // ============================================================
        var data = new BsonDocument
        {
            { "path", context.Request.Path.Value ?? "Unknown" },
            { "method", context.Request.Method },
            { "trace_id", context.TraceIdentifier },
            { "error_type", errorType },
            { "exception_type", ex.GetType().FullName ?? "Unknown" },
            { "exception_message", ex.Message },
            { "stack_trace", ex.StackTrace ?? string.Empty },
            { "inner_exception", ex.InnerException?.Message ?? string.Empty },
            { "timestamp", DateTime.UtcNow }
        };

        // ============================================================
        // Create error log document using new schema
        // ============================================================
        var errorEntry = new ErrorLogCollection
        {
            ErrorId = ObjectId.GenerateNewId().ToString(),
            CorrelationId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            SourceLayer = "Log",
            SourceLevel = "Cloud",
            SourceService = "Log Service",
            SourceDomain = "[MIDDLEWARE][EXCEPTION]",
            Type = "error",
            Category = "Middleware",
            Message = $"{userMessage}: {ex.Message}",
            Operation = context.Request.Path,
            Hostname = Environment.MachineName,
            ContainerIp = Environment.GetEnvironmentVariable("CONTAINER_IP") ?? "unknown",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
            Data = data
        };

        _logger.LogInformation("{Domain} Writing error log entry to MongoDB ({ErrorType})\n", domain, errorType);

        await errorRepo.InsertAsync(errorEntry);

        _logger.LogInformation("{Domain} Stored error log successfully with Id={ErrorId}\n", domain, errorEntry.ErrorId);

        // ============================================================
        // API Response
        // ============================================================
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var responseBody = new
        {
            error = userMessage,
            type = errorType,
            details = ex.Message,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(responseBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await context.Response.WriteAsync(json);
    }
}
