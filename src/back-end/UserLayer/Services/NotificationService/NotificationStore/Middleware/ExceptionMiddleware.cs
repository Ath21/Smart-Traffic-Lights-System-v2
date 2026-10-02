using System.Net;
using System.Text.Json;
using System.Security.Authentication;
using Microsoft.IdentityModel.Tokens;
using MailKit.Net.Smtp;
using MailKit;
using MongoDB.Driver;
using RabbitMQ.Client.Exceptions;
using AutoMapper;
using MassTransit;
using NotificationStore.Publishers.Logs;

namespace NotificationStore.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string Domain = "[MIDDLEWARE][EXCEPTION]";

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
                AutoMapperMappingException    => (HttpStatusCode.InternalServerError, "Mapping error occurred", "MAPPING_ERROR"),

                // ============================
                // EMAIL / SMTP
                // ============================
                SmtpCommandException          => (HttpStatusCode.BadGateway, "SMTP command failed", "SMTP_COMMAND_ERROR"),
                SmtpProtocolException         => (HttpStatusCode.BadGateway, "SMTP protocol error", "SMTP_PROTOCOL_ERROR"),
                MessageNotFoundException      => (HttpStatusCode.NotFound, "Email message not found", "EMAIL_NOT_FOUND"),

                // ============================
                // DATABASE (MongoDB)
                // ============================
                MongoAuthenticationException  => (HttpStatusCode.Unauthorized, "MongoDB authentication failed", "DB_AUTH_ERROR"),
                MongoConnectionException      => (HttpStatusCode.ServiceUnavailable, "MongoDB connection failure", "DB_CONN_ERROR"),
                MongoWriteException           => (HttpStatusCode.Conflict, "MongoDB write conflict", "DB_WRITE_ERROR"),
                MongoException                => (HttpStatusCode.InternalServerError, "MongoDB error", "DB_ERROR"),

                // ============================
                // BROKER / NETWORK
                // ============================
                TimeoutException              => (HttpStatusCode.RequestTimeout, "Operation timed out", "TIMEOUT"),
                RequestTimeoutException       => (HttpStatusCode.GatewayTimeout, "RabbitMQ request timeout", "BROKER_TIMEOUT"),
                BrokerUnreachableException    => (HttpStatusCode.BadGateway, "RabbitMQ unreachable", "BROKER_UNREACHABLE"),
                OperationInterruptedException => (HttpStatusCode.ServiceUnavailable, "RabbitMQ operation interrupted", "BROKER_INTERRUPTED"),

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
        _logger.LogError(ex, "{Domain} {ErrorType} - {Message}", Domain, errorType, userMessage);

        // Correlation / trace context
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
            ["stack_trace"] = ex.StackTrace ?? string.Empty,
            ["timestamp"] = DateTime.UtcNow
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logPublisher = scope.ServiceProvider.GetRequiredService<INotificationLogPublisher>();

            await logPublisher.PublishErrorAsync(
                domain: Domain,
                messageText: $"[{errorType}] {userMessage}: {ex.Message}",
                data: metadata,
                operation: "HandleErrorAsync");

            _logger.LogInformation("{Domain} Published error log ({ErrorType}) via RabbitMQ", Domain, errorType);
        }
        catch (Exception pubEx)
        {
            _logger.LogError(pubEx, "{Domain} Failed to publish structured log to RabbitMQ", Domain);
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var responseBody = new
        {
            error = userMessage,
            details = ex.Message,
            type = errorType,
            traceId = context.TraceIdentifier,
            correlationId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(responseBody));
    }
}
