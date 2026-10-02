using System.Net;
using System.Text.Json;
using System.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client.Exceptions;
using MassTransit;
using TrafficAnalytics.Publishers.Logs;

namespace TrafficAnalyticsStore.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

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
            var (statusCode, userMessage, errorType) = ex switch
            {
                // ============================
                // AUTH / SECURITY
                // ============================
                UnauthorizedAccessException   => (HttpStatusCode.Unauthorized, "Unauthorized access", "AUTH_ERROR"),
                SecurityTokenException        => (HttpStatusCode.Unauthorized, "Invalid or expired token", "TOKEN_ERROR"),
                AuthenticationException       => (HttpStatusCode.Unauthorized, "Authentication failed", "AUTHENTICATION_ERROR"),

                // ============================
                // CLIENT / VALIDATION
                // ============================
                KeyNotFoundException          => (HttpStatusCode.NotFound, "Resource not found", "NOT_FOUND"),
                InvalidOperationException     => (HttpStatusCode.BadRequest, "Invalid operation", "INVALID_OPERATION"),
                ArgumentNullException         => (HttpStatusCode.BadRequest, "Missing required parameter", "ARGUMENT_NULL"),
                ArgumentException             => (HttpStatusCode.BadRequest, "Invalid argument", "ARGUMENT_ERROR"),
                FormatException               => (HttpStatusCode.BadRequest, "Invalid data format", "FORMAT_ERROR"),
                JsonException                 => (HttpStatusCode.BadRequest, "Invalid JSON payload", "JSON_ERROR"),

                // ============================
                // DATABASE (EF Core)
                // ============================
                DbUpdateConcurrencyException  => (HttpStatusCode.Conflict, "Database concurrency conflict", "DB_CONCURRENCY_ERROR"),
                DbUpdateException             => (HttpStatusCode.Conflict, "Database update failed", "DB_UPDATE_ERROR"),

                // ============================
                // BROKER / NETWORK
                // ============================
                RequestTimeoutException       => (HttpStatusCode.GatewayTimeout, "RabbitMQ request timeout", "BROKER_TIMEOUT"),
                BrokerUnreachableException    => (HttpStatusCode.BadGateway, "RabbitMQ unreachable", "BROKER_UNREACHABLE"),
                OperationInterruptedException => (HttpStatusCode.ServiceUnavailable, "RabbitMQ operation interrupted", "BROKER_INTERRUPTED"),
                TimeoutException              => (HttpStatusCode.RequestTimeout, "Operation timed out", "TIMEOUT"),

                // ============================
                // FALLBACK
                // ============================
                _                             => (HttpStatusCode.InternalServerError, "Unexpected error occurred", "UNEXPECTED")
            };

            await HandleErrorAsync(context, statusCode, userMessage, errorType, ex);
        }
    }

    private async Task HandleErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string userMessage,
        string errorType,
        Exception ex)
    {
        _logger.LogError(ex, "[MIDDLEWARE][EXCEPTION] {ErrorType} - {Message}", errorType, userMessage);

        string correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var headerId)
            ? headerId.ToString()
            : Guid.NewGuid().ToString();

        var data = new Dictionary<string, object>
        {
            ["path"] = context.Request.Path,
            ["method"] = context.Request.Method,
            ["trace_id"] = context.TraceIdentifier,
            ["correlation_id"] = correlationId,
            ["exception_type"] = ex.GetType().FullName ?? "Unknown",
            ["exception_message"] = ex.Message,
            ["stack_trace"] = ex.StackTrace ?? string.Empty
        };

        // Publish error to Log Layer via RabbitMQ
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logPublisher = scope.ServiceProvider.GetRequiredService<IAnalyticsLogPublisher>();

            await logPublisher.PublishErrorAsync(
                domain: "[MIDDLEWARE][EXCEPTION]",
                messageText: $"[{errorType}] {userMessage}: {ex.Message}",
                data: data,
                operation: "HTTP_PIPELINE");

            _logger.LogInformation("[MIDDLEWARE][EXCEPTION] Published error log ({ErrorType}) via RabbitMQ", errorType);
        }
        catch (Exception pubEx)
        {
            _logger.LogError(pubEx, "[MIDDLEWARE][EXCEPTION] Failed to publish log to RabbitMQ");
        }

        // Return HTTP response
        context.Response.StatusCode = (int)statusCode;
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
