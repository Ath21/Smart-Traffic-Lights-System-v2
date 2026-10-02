using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TrafficLightCacheData;
using TrafficLightCacheData.Repositories;
using TrafficLightCacheData.Settings;
using TrafficLightControllerStore.Aggregators.Control;
using TrafficLightControllerStore.Aggregators.Time;
using TrafficLightControllerStore.Consumers;
using TrafficLightControllerStore.Domain;
using TrafficLightControllerStore.Middleware;
using TrafficLightControllerStore.Publishers.Logs;

namespace TrafficLightControllerStore;

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
        // Traffic Light Context
        // ===============================
        services.AddSingleton(sp =>
        {
            var id = int.Parse(_configuration["TrafficLight:Id"] ?? throw new InvalidOperationException("Traffic Light Id missing"));
            var name = _configuration["TrafficLight:Name"] ?? "Unknown";
            return new TrafficLightContext(id, name);
        });

        // ===============================
        // Intersection Context
        // ===============================
        services.AddSingleton(sp =>
        {
            var id = int.Parse(_configuration["Intersection:Id"] ?? throw new InvalidOperationException("Intersection Id missing"));
            var name = _configuration["Intersection:Name"] ?? "Unknown";
            return new IntersectionContext(id, name);
        });

        // ===============================
        // Data Layer (Redis - TrafficLightCacheDB)
        // ===============================
        services.Configure<TrafficLightCacheDbSettings>(options =>
        {
            options.Host = _configuration["Redis:TrafficLight:Host"];
            options.Port = int.Parse(_configuration["Redis:TrafficLight:Port"] ?? "6379");
            options.Password = _configuration["Redis:TrafficLight:Password"];
            options.Database = int.Parse(_configuration["Redis:TrafficLight:Database"] ?? "0");

            options.KeyPrefix = new TrafficLightCacheData.Settings.KeyPrefixSettings
            {
                // --------------------------
                // Core State
                // --------------------------
                State = _configuration["Redis:TrafficLight:KeyPrefix:State"],
                CurrentPhase = _configuration["Redis:TrafficLight:KeyPrefix:CurrentPhase"],
                RemainingTime = _configuration["Redis:TrafficLight:KeyPrefix:RemainingTime"],
                Duration = _configuration["Redis:TrafficLight:KeyPrefix:Duration"],
                LastUpdate = _configuration["Redis:TrafficLight:KeyPrefix:LastUpdate"],

                // --------------------------
                // Synchronization
                // --------------------------
                CycleDuration = _configuration["Redis:TrafficLight:KeyPrefix:CycleDuration"],
                Offset = _configuration["Redis:TrafficLight:KeyPrefix:Offset"],
                LocalOffset = _configuration["Redis:TrafficLight:KeyPrefix:LocalOffset"],
                CycleProgress = _configuration["Redis:TrafficLight:KeyPrefix:CycleProgress"],

                // --------------------------
                // Configuration & Priority
                // --------------------------
                Mode = _configuration["Redis:TrafficLight:KeyPrefix:Mode"],
                Priority = _configuration["Redis:TrafficLight:KeyPrefix:Priority"],
                CachedPhases = _configuration["Redis:TrafficLight:KeyPrefix:CachedPhases"],

                // --------------------------
                // Failover & Diagnostics
                // --------------------------
                FailoverActive = _configuration["Redis:TrafficLight:KeyPrefix:FailoverActive"],
                Heartbeat = _configuration["Redis:TrafficLight:KeyPrefix:Heartbeat"],
                LastCoordinatorSync = _configuration["Redis:TrafficLight:KeyPrefix:LastCoordinatorSync"]
            };
        });

        services.AddSingleton<TrafficLightCacheDbContext>();

        services.AddScoped(typeof(ITrafficLightCacheRepository), typeof(TrafficLightCacheRepository));

        // ===============================
        // Aggregators
        // ===============================
        services.AddScoped(typeof(ITrafficLightAggregator), typeof(TrafficLightAggregator));

        // ===============================
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(ITrafficLightLogPublisher), typeof(TrafficLightLogPublisher));

        // Consumers
        services.AddScoped<TrafficLightControlConsumer>();

        // MassTransit Setup
        services.AddTrafficLightControllerMassTransit(_configuration);

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
        var allowedMethods = _configuration["Cors:AllowedMethods"]?.Split(",") ?? new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };
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
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
        services.AddEndpointsApiExplorer();

        // ===============================
        // Swagger (API Documentation)
        // ===============================
        var intersectionName = _configuration["Intersection:Name"] ?? "Unknown Intersection";
        var trafficLightName = _configuration["TrafficLight:Name"] ?? "Unknown Traffic Light";
        var trafficLightId = _configuration["TrafficLight:Id"] ?? "Unknown Traffic Light Id";

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"Traffic Light Controller – {trafficLightName.ToLower().Replace(" ", "-")}{trafficLightId} ({intersectionName})",
                Version = "v3.0",
                Description = $"Manages and monitors the state of traffic light '{trafficLightName}' at intersection '{intersectionName}'.",
                Contact = new OpenApiContact
                {
                    Name = "Vasileios Evangelos Athanasiou",
                    Email = "ice19390005@uniwa.gr",
                    Url = new Uri("https://github.com/Ath21")
                },
                License = new OpenApiLicense
                {
                    Name = "Academic License – University of West Attica",
                    Url = new Uri("https://www.uniwa.gr")
                }
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
        var intersectionName = _configuration["Intersection:Name"] ?? "Unknown Intersection";
        var trafficLightName = _configuration["TrafficLight:Name"] ?? "Unknown Traffic Light";
        var trafficLightId = _configuration["TrafficLight:Id"] ?? "Unknown Traffic Light Id";

        // ===============================
        // Swagger UI
        // ===============================
        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Traffic Light Controller – {trafficLightName.ToLower().Replace(" ", "-")}{trafficLightId} ({intersectionName})");
                c.DocumentTitle = $"Traffic Light Controller – {trafficLightName.ToLower().Replace(" ", "-")}{trafficLightId} ({intersectionName})";
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
