using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TrafficLightData;
using TrafficLightData.Repositories.Intersections;
using TrafficLightData.Repositories.Light;
using TrafficLightData.Repositories.TrafficConfig;
using TrafficLightData.Settings;
using TrafficLightCoordinatorStore.Middleware;
using TrafficLightCoordinatorStore.Publishers.Schedule;
using TrafficLightCoordinatorStore.Publishers.Logs;
using TrafficLightCoordinatorStore.Consumers;
using TrafficLightCoordinatorStore.Business.Operator;
using TrafficLightCoordinatorStore.Aggregators.Priority;
using TrafficLightCoordinatorStore.Aggregators.Analytics;
using TrafficLightCoordinatorStore.Publishers.Control;

namespace TrafficLightCoordinatorStore;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // ===============================
        // Data Layer (MSSQL - TrafficLightDB)
        // ===============================
        // Db Context
        services.Configure<TrafficLightDbSettings>(options =>
        {
            options.ConnectionString = _configuration["MSSQL:ConnectionString"];
        });
        services.AddDbContext<TrafficLightDbContext>(options =>
            options.UseSqlServer(_configuration["MSSQL:ConnectionString"]));

        // Repositories
        services.AddScoped(typeof(IIntersectionRepository), typeof(IntersectionRepository));
        services.AddScoped(typeof(ITrafficLightRepository), typeof(TrafficLightRepository));
        services.AddScoped(typeof(ITrafficConfigurationRepository), typeof(TrafficConfigurationRepository));

        // ===============================
        // Business Layer (Services)
        // ===============================
        services.AddScoped(typeof(ITrafficOperatorBusiness), typeof(TrafficOperatorBusiness));
        services.AddScoped(typeof(ITrafficModeAggregator), typeof(TrafficModeAggregator));
        services.AddScoped(typeof(IAnalyticsModeAggregator), typeof(AnalyticsModeAggregator));

        // ===============================
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(ITrafficLightSchedulePublisher), typeof(TrafficLightSchedulePublisher));
        services.AddScoped(typeof(ITrafficLightControlPublisher), typeof(TrafficLightControlPublisher));
        services.AddScoped(typeof(ICoordinatorLogPublisher), typeof(CoordinatorLogPublisher));

        // Consumers
        services.AddScoped<PriorityCountConsumer>();
        services.AddScoped<PriorityEventConsumer>();
        services.AddScoped<CongestionAnalyticsConsumer>();
        services.AddScoped<IncidentAnalyticsConsumer>();
        services.AddScoped<SummaryAnalyticsConsumer>();

        // MassTransit Setup
        services.AddTrafficCoordinatorMassTransit(_configuration);

        // ===============================
        // JWT Authentication
        // ===============================
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        // ===============================
        // CORS Policy
        // ===============================
        var allowedOrigins = _configuration["Cors:AllowedOrigins"]?.Split(",") ?? Array.Empty<string>();
        var allowedMethods = _configuration["Cors:AllowedMethods"]?.Split(",") ?? new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };
        var allowedHeaders = _configuration["Cors:AllowedHeaders"]?.Split(",") ?? new[] { "Content-Type", "Authorization" };

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .WithMethods(allowedMethods)
                      .WithHeaders(allowedHeaders);
            });
        });

        // ===============================
        // Controllers
        // ===============================
        services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.PropertyNamingPolicy = null);
        services.AddEndpointsApiExplorer();

        // ===============================
        // Swagger (API Documentation)
        // ===============================
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Traffic Light Coordinator Service",
                Version = "v3.0",
                Description = "Central coordination service managing traffic light synchronization and congestion response across all intersections."
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Insert JWT token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    public void Configure(WebApplication app, IWebHostEnvironment env)
    {
        // ===============================
        // Swagger UI
        // ===============================
        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Traffic Light Coordinator Service");
                c.DocumentTitle = "Traffic Light Coordinator Service";
            });
        }

        // ===============================
        // Core Middleware
        // ===============================
        app.UseHttpsRedirection();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();

        // ===============================
        // Endpoints
        // ===============================
        app.MapControllers();

        app.Run();
    }
}
