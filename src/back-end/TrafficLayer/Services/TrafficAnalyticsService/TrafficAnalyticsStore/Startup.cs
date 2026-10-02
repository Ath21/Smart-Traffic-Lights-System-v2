using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TrafficAnalyticsData;
using TrafficAnalyticsData.Repositories.Summary;
using TrafficAnalyticsData.Repositories.Alerts;
using TrafficAnalyticsData.Settings;
using TrafficAnalyticsStore.Middleware;
using DetectionData;
using DetectionData.Repositories.Vehicle;
using DetectionData.Repositories.Pedestrian;
using DetectionData.Repositories.Cyclist;
using DetectionData.Repositories.PublicTransport;
using DetectionData.Repositories.EmergencyVehicle;
using DetectionData.Repositories.Incident;
using TrafficAnalyticsStore.Publishers.Analytics;
using TrafficAnalyticsStore.Consumers;
using TrafficAnalyticsStore.Business.Alerts;
using TrafficAnalyticsStore.Business.DailySummary;
using TrafficAnalyticsStore.Aggregators;
using TrafficAnalyticsStore.Aggregators.Analytics;
using TrafficAnalytics.Publishers.Logs;
using TrafficAnalyticsStore.Consumers.Sensor;
using TrafficAnalyticsStore.Consumers.Detection;


namespace TrafficAnalyticsStore;

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
        // Data Layer (PostgreSQL - TrafficAnalyticsDB)
        // ===============================
        // Db Context
        services.Configure<TrafficAnalyticsDbSettings>(options =>
        {
            options.ConnectionString = _configuration["PostgreSQL:ConnectionString"];
        });
        services.AddDbContext<TrafficAnalyticsDbContext>(options =>
            options.UseNpgsql(_configuration["PostgreSQL:ConnectionString"]));

        // Repositories
        services.AddScoped(typeof(IDailySummaryRepository), typeof(DailySummaryRepository));
        services.AddScoped(typeof(IAlertRepository), typeof(AlertRepository));

        // ===============================
        // Data Layer (MongoDB - DetectionDB)
        // ===============================
        services.Configure<DetectionDbSettings>(options =>
        {
            options.ConnectionString = _configuration["Mongo:ConnectionString"];
            options.Database = _configuration["Mongo:Database"];
            options.Collections = new CollectionsSettings
            {
                VehicleCount = _configuration["Mongo:Collections:VehicleCount"],
                PedestrianCount = _configuration["Mongo:Collections:PedestrianCount"],
                CyclistCount = _configuration["Mongo:Collections:CyclistCount"],
                PublicTransport = _configuration["Mongo:Collections:PublicTransportDetections"],
                EmergencyVehicle = _configuration["Mongo:Collections:EmergencyVehicleDetections"],
                Incident = _configuration["Mongo:Collections:IncidentDetections"]
            };
        });
        services.AddSingleton<DetectionDbContext>();

        // Repositories
        services.AddScoped(typeof(IVehicleCountRepository), typeof(VehicleCountRepository));
        services.AddScoped(typeof(IPedestrianCountRepository), typeof(PedestrianCountRepository));
        services.AddScoped(typeof(ICyclistCountRepository), typeof(CyclistCountRepository));
        services.AddScoped(typeof(IEmergencyVehicleDetectionRepository), typeof(EmergencyVehicleDetectionRepository));
        services.AddScoped(typeof(IPublicTransportDetectionRepository), typeof(PublicTransportDetectionRepository));
        services.AddScoped(typeof(IIncidentDetectionRepository), typeof(IncidentDetectionRepository));

        // ===============================
        // Business Layer (Services)
        // ===============================
        services.AddScoped(typeof(IAlertBusiness), typeof(AlertBusiness));
        services.AddScoped(typeof(IDailySummaryBusiness), typeof(DailySummaryBusiness));
        services.AddScoped(typeof(ITrafficAnalyticsAggregator), typeof(TrafficAnalyticsAggregator));

        // ===============================
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(ITrafficAnalyticsPublisher), typeof(TrafficAnalyticsPublisher));
        services.AddScoped(typeof(IAnalyticsLogPublisher), typeof(AnalyticsLogPublisher));

        // Consumers
        services.AddScoped<VehicleCountConsumer>();
        services.AddScoped<PedestrianCountConsumer>();
        services.AddScoped<CyclistCountConsumer>();
        services.AddScoped<PublicTransportDetectedConsumer>();
        services.AddScoped<EmergencyVehicleDetectedConsumer>();
        services.AddScoped<IncidentDetectedConsumer>();

        // MassTransit Setup
        services.AddTrafficAnalyticsMassTransit(_configuration);

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
                Title = "Traffic Analytics Service",
                Version = "v3.0",
                Description = "Aggregates, analyzes, and publishes traffic statistics and congestion data across all intersections.",
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
        // ===============================
        // Swagger UI
        // ===============================
        if (env.IsDevelopment() || env.IsProduction())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Traffic Analytics Service");
                c.DocumentTitle = "Traffic Analytics Service";
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
