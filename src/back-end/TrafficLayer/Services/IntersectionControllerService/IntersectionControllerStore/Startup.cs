using IntersectionControllerStore.Middleware;
using TrafficLightCacheData;
using DetectionCacheData;
using DetectionCacheData.Settings;
using TrafficLightCacheData.Settings;
using TrafficLightCacheData.Repositories;
using DetectionCacheData.Repositories;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IntersectionControllerStore.Domain;
using IntersectionControllerStore.Publishers.Priority;
using IntersectionControllerStore.Publishers.Logs;
using IntersectionControllerStore.Consumers;
using IntersectionControllerStore.Publishers.Light;
using IntersectionControllerStore.Consumers.Light;
using IntersectionControllerStore.Aggregators.Light;
using IntersectionControllerStore.Aggregators.Priority;

namespace IntersectionControllerStore;

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
        // Intersection Context
        // ===============================
        services.AddSingleton(sp =>
        {
            var config = _configuration;

            var intersectionId = int.Parse(config["Intersection:Id"]
                ?? throw new InvalidOperationException("Intersection Id missing"));

            var intersectionName = config["Intersection:Name"] ?? "Unknown";

            // Read traffic lights
            var trafficLights = config.GetSection("TrafficLights")
                .GetChildren()
                .Select(section => new TrafficLightContext(
                    int.Parse(section["Id"] ?? throw new InvalidOperationException("TrafficLight Id missing")),
                    section["Name"] ?? "Unknown"))
                .ToList();

            return new IntersectionContext(intersectionId, intersectionName, trafficLights);
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
        services.AddScoped<ITrafficLightCacheRepository, TrafficLightCacheRepository>();

        // ===============================
        // Data Layer (Redis - DetectionCacheDB)
        // ===============================
        // Db Context
        services.Configure<DetectionCacheDbSettings>(options =>
        {
            options.Host = _configuration["Redis:Host"];
            options.Port = int.Parse(_configuration["Redis:Port"]);
            options.Password = _configuration["Redis:Password"];
            options.Database = int.Parse(_configuration["Redis:Database"]);
            options.KeyPrefix = new DetectionCacheData.Settings.KeyPrefixSettings
            {
                VehicleCount = _configuration["Redis:KeyPrefix:VehicleCount"],
                PedestrianCount = _configuration["Redis:KeyPrefix:PedestrianCount"],
                CyclistCount = _configuration["Redis:KeyPrefix:CyclistCount"],
                PublicTransportDetections = _configuration["Redis:KeyPrefix:PublicTransportDetections"],
                EmergencyVehicleDetections = _configuration["Redis:KeyPrefix:EmergencyVehicleDetections"],
                IncidentDetections = _configuration["Redis:KeyPrefix:IncidentDetections"]
            };
        });
        services.AddSingleton<DetectionCacheDbContext>();

        // Repositories
        services.AddScoped(typeof(IDetectionCacheRepository), typeof(DetectionCacheRepository));

        // ===============================
        // Business Layer (Services)
        // ===============================
        services.AddScoped(typeof(ITrafficLightAggregator), typeof(TrafficLightAggregator));
        services.AddScoped(typeof(IPriorityAggregator), typeof(PriorityAggregator));
        
        // ===============================
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(IPriorityPublisher), typeof(PriorityPublisher));
        services.AddScoped(typeof(ITrafficLightPublisher), typeof(TrafficLightPublisher));
        services.AddScoped(typeof(IIntersectionLogPublisher), typeof(IntersectionLogPublisher));

        // Consumers
        services.AddScoped<LightScheduleConsumer>();
        services.AddScoped<EmergencyVehicleDetectedConsumer>();
        services.AddScoped<IncidentDetectedConsumer>();
        services.AddScoped<PublicTransportDetectedConsumer>();
        services.AddScoped<VehicleCountConsumer>();
        services.AddScoped<PedestrianCountConsumer>();
        services.AddScoped<CyclistCountConsumer>();

        // MassTransit Setup
        services.AddIntersectionControllerMassTransit(_configuration);

        // ===============================
        // JWT (Authentication & Authorization)
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
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
        services.AddEndpointsApiExplorer();

        // ===============================
        // Swagger (API Documentation)
        // ===============================
        var intersectionName = _configuration["Intersection:Name"] ?? "Unknown Intersection";
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = $"Intersection Controller Service – {intersectionName}",
                Version = "v3.0",
                Description = $"Controls and coordinates traffic lights for intersection '{intersectionName}'.",
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

        // ===============================
        // Swagger UI
        // ===============================
        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Intersection Controller – {intersectionName}");
                c.DocumentTitle = $"Intersection Controller – {intersectionName}";
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
