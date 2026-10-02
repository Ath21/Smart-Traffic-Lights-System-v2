using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MassTransit;
using MongoDB.Driver;
using RabbitMQ.Client.Exceptions;
using SensorStore.Publishers.Logs;

namespace SensorStore.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string Tag = "[MIDDLEWARE][EXCEPTION]";

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, message, errorType) = ex switch
            {
                // AUTH / SECURITY
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access", "AUTH_ERROR"),

                // CLIENT / VALIDATION
                KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found", "NOT_FOUND"),
                ArgumentNullException => (HttpStatusCode.BadRequest, "Missing required parameter", "ARGUMENT_NULL"),
                ArgumentException => (HttpStatusCode.BadRequest, "Invalid argument", "ARGUMENT_ERROR"),
                FormatException => (HttpStatusCode.BadRequest, "Invalid data format", "FORMAT_ERROR"),
                JsonException => (HttpStatusCode.BadRequest, "Invalid JSON payload", "JSON_ERROR"),

                // DATABASE (MongoDB)
                MongoAuthenticationException => (HttpStatusCode.Unauthorized, "MongoDB authentication failed", "DB_AUTH_ERROR"),
                MongoConnectionException => (HttpStatusCode.ServiceUnavailable, "MongoDB connection error", "DB_CONN_ERROR"),
                MongoWriteException => (HttpStatusCode.Conflict, "MongoDB write conflict", "DB_WRITE_ERROR"),
                MongoException => (HttpStatusCode.InternalServerError, "MongoDB general error", "DB_ERROR"),

                // BROKER / NETWORK / MASS TRANSIT
                RequestTimeoutException => (HttpStatusCode.GatewayTimeout, "RabbitMQ request timeout", "BROKER_TIMEOUT"),
                BrokerUnreachableException => (HttpStatusCode.BadGateway, "RabbitMQ unreachable", "BROKER_UNREACHABLE"),
                OperationInterruptedException => (HttpStatusCode.ServiceUnavailable, "RabbitMQ operation interrupted", "BROKER_INTERRUPTED"),
                TimeoutException => (HttpStatusCode.RequestTimeout, "Operation timed out", "TIMEOUT"),

                // FALLBACK
                _ => (HttpStatusCode.InternalServerError, "Unexpected error occurred", "UNEXPECTED")
            };

            await HandleErrorAsync(context, status, message, errorType, ex);
        }
    }

    private async Task HandleErrorAsync(
        HttpContext context,
        HttpStatusCode status,
        string userMessage,
        string errorType,
        Exception ex)
    {
        _logger.LogError(ex, "{Tag} {ErrorType} - {Message}", Tag, errorType, userMessage);

        var correlationId = context.Request.Headers.ContainsKey("X-Correlation-ID")
            ? context.Request.Headers["X-Correlation-ID"].ToString()
            : Guid.NewGuid().ToString();

        var data = new Dictionary<string, object>
        {
            ["Path"] = context.Request.Path,
            ["Method"] = context.Request.Method,
            ["TraceId"] = context.TraceIdentifier,
            ["CorrelationId"] = correlationId,
            ["ExceptionType"] = ex.GetType().FullName ?? "Unknown",
            ["ExceptionMessage"] = ex.Message,
            ["StackTrace"] = ex.StackTrace ?? string.Empty
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logPublisher = scope.ServiceProvider.GetRequiredService<ISensorLogPublisher>();

            await logPublisher.PublishErrorAsync(
                domain: "[MIDDLEWARE][EXCEPTION]",
                messageText: $"[{errorType}] {userMessage}: {ex.Message}",
                data: data,
                operation: "HandleErrorAsync");

            _logger.LogInformation("{Tag} Published error log ({ErrorType}) via RabbitMQ", Tag, errorType);
        }
        catch (Exception pubEx)
        {
            _logger.LogError(pubEx, "{Tag} Failed to publish error log to RabbitMQ", Tag);
        }

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = userMessage,
            details = ex.Message,
            type = errorType,
            traceId = context.TraceIdentifier,
            correlationId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
