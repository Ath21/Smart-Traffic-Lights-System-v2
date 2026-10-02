using System.Text;
using DetectionData;
using DetectionStore.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using DetectionCacheData;
using DetectionData.Repositories.EmergencyVehicle;
using DetectionData.Repositories.PublicTransport;
using DetectionData.Repositories.Incident;
using DetectionStore.Business;
using DetectionStore.Publishers.Logs;
using DetectionStore.Publishers.Event;
using DetectionStore.Workers;
using DetectionData.Repositories.Vehicle;
using DetectionData.Repositories.Pedestrian;
using DetectionData.Repositories.Cyclist;
using DetectionCacheData.Repositories;
using DetectionStore.Domain;
using DetectionCacheData.Settings;

namespace DetectionStore;

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
            var id = int.Parse(_configuration["Intersection:Id"] ?? throw new InvalidOperationException("Intersection Id missing"));
            var name = _configuration["Intersection:Name"] ?? "Unknown";
            return new IntersectionContext(id, name);
        });

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
        // Data Layer (Redis - DetectionCacheDB)
        // ===============================
        // Db Context
        services.Configure<DetectionCacheDbSettings>(options =>
        {
            options.Host = _configuration["Redis:Host"];
            options.Port = int.Parse(_configuration["Redis:Port"]);
            options.Password = _configuration["Redis:Password"];
            options.Database = int.Parse(_configuration["Redis:Database"]);
            options.KeyPrefix = new KeyPrefixSettings
            {
                VehicleCount = _configuration["Redis:Sensor:KeyPrefix:VehicleCount"],
                PedestrianCount = _configuration["Redis:Sensor:KeyPrefix:PedestrianCount"],
                CyclistCount = _configuration["Redis:Sensor:KeyPrefix:CyclistCount"],
                PublicTransportDetections = _configuration["Redis:Detection:KeyPrefix:PublicTransportDetections"],
                EmergencyVehicleDetections = _configuration["Redis:Detection:KeyPrefix:EmergencyVehicleDetections"],
                IncidentDetections = _configuration["Redis:Detection:KeyPrefix:IncidentDetections"]
            };
        });
        services.AddSingleton<DetectionCacheDbContext>();

        // Repositories
        services.AddScoped(typeof(IDetectionCacheRepository), typeof(DetectionCacheRepository));

        // ===============================
        // Business Layer (Services)
        // ===============================
        services.AddScoped(typeof(IDetectionBusiness), typeof(DetectionBusiness));

        // ===============================
        // AutoMapper (object-object mapping)
        // ===============================
        services.AddAutoMapper(typeof(DetectionStoreProfile));

        // ===============================
        // Workers (Virtual Detectors)
        // ===============================
        services.AddHostedService<DetectionWorker>();

        // ===============================
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(IDetectionEventPublisher), typeof(DetectionEventPublisher));
        services.AddScoped(typeof(IDetectionLogPublisher), typeof(DetectionLogPublisher));

        // MassTransit Setup
        services.AddDetectionServiceMassTransit(_configuration);

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
                Title = $"Detection Service – {intersectionName}",
                Version = "v3.0",
                Description = $"Virtual detectors that publish detection data (emergency vehicles, public transport, incidents) for intersection '{intersectionName}'.",
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Detection Service – {intersectionName}");
                c.DocumentTitle = $"Detection Service – {intersectionName}";
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
