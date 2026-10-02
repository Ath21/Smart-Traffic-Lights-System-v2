using System.Net;
using System.Text.Json;
using System.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client.Exceptions;
using MassTransit;
using TrafficLightCoordinatorStore.Publishers.Logs;

namespace TrafficLightCoordinatorStore.Middleware;

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
                UnauthorizedAccessException   => (HttpStatusCode.Unauthorized, "Unauthorized access", "AUTH_ERROR"),
                SecurityTokenException        => (HttpStatusCode.Unauthorized, "Invalid or expired token", "TOKEN_ERROR"),
                AuthenticationException       => (HttpStatusCode.Unauthorized, "Authentication failed", "AUTHENTICATION_ERROR"),

                KeyNotFoundException          => (HttpStatusCode.NotFound, "Resource not found", "NOT_FOUND"),
                InvalidOperationException     => (HttpStatusCode.BadRequest, "Invalid operation", "INVALID_OPERATION"),
                ArgumentNullException         => (HttpStatusCode.BadRequest, "Missing required parameter", "ARGUMENT_NULL"),
                ArgumentException             => (HttpStatusCode.BadRequest, "Invalid argument", "ARGUMENT_ERROR"),
                FormatException               => (HttpStatusCode.BadRequest, "Invalid data format", "FORMAT_ERROR"),
                JsonException                 => (HttpStatusCode.BadRequest, "Invalid JSON payload", "JSON_ERROR"),

                DbUpdateConcurrencyException  => (HttpStatusCode.Conflict, "Database concurrency conflict", "DB_CONCURRENCY_ERROR"),
                DbUpdateException             => (HttpStatusCode.Conflict, "Database update failed", "DB_UPDATE_ERROR"),

                RequestTimeoutException       => (HttpStatusCode.GatewayTimeout, "RabbitMQ request timeout", "BROKER_TIMEOUT"),
                BrokerUnreachableException    => (HttpStatusCode.BadGateway, "RabbitMQ unreachable", "BROKER_UNREACHABLE"),
                OperationInterruptedException => (HttpStatusCode.ServiceUnavailable, "RabbitMQ operation interrupted", "BROKER_INTERRUPTED"),
                TimeoutException              => (HttpStatusCode.RequestTimeout, "Operation timed out", "TIMEOUT"),

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
        _logger.LogError(ex, "[EXCEPTION] {ErrorType} - {Message}", errorType, userMessage);

        string correlationId = context.Request.Headers.ContainsKey("X-Correlation-ID")
            ? context.Request.Headers["X-Correlation-ID"].ToString()
            : Guid.NewGuid().ToString();

        var metadata = new Dictionary<string, object>
        {
            ["path"] = context.Request.Path,
            ["method"] = context.Request.Method,
            ["trace_id"] = context.TraceIdentifier,
            ["correlation_id"] = correlationId,
            ["exception_type"] = ex.GetType().FullName ?? "Unknown",
            ["exception_message"] = ex.Message,
            ["stack_trace"] = ex.StackTrace ?? string.Empty
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logPublisher = scope.ServiceProvider.GetRequiredService<ICoordinatorLogPublisher>();

            await logPublisher.PublishErrorAsync(
                domain: "[COORDINATOR]",
                messageText: $"[{errorType}] {userMessage}: {ex.Message}",
                data: metadata,
                operation: errorType
            );

            _logger.LogInformation("[EXCEPTION] Published error log ({ErrorType}) via RabbitMQ", errorType);
        }
        catch (Exception pubEx)
        {
            _logger.LogError(pubEx, "[EXCEPTION] Failed to publish log to RabbitMQ");
        }

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
