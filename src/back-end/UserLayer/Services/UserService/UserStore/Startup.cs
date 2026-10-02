using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserData;
using UserData.Repositories.Usr;
using UserData.Repositories.Ses;
using UserData.Repositories.Audit;
using UserData.Settings;
using UserStore.Business.Password;
using UserStore.Business.Token;
using UserStore.Business.Usr;
using UserStore.Middleware;
using UserStore.Publishers.Logs;
using UserStore.Publishers.Notifications;
using UserStore.Business.Subscribe;

namespace UserStore;

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
        // Data Layer (MSSQL - UserDB)
        // ===============================
        // Db Context
        services.Configure<UserDbSettings>(options =>
        {
            options.ConnectionString = _configuration["MSSQL:ConnectionString"];
        });

        services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(_configuration["MSSQL:ConnectionString"]));

        // Repositories
        services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
        services.AddScoped(typeof(ISessionRepository), typeof(SessionRepository));
        services.AddScoped(typeof(IUserAuditRepository), typeof(UserAuditRepository));

        // ===============================
        // Business Layer (Services)
        // ===============================
        services.AddScoped(typeof(IPasswordHasher), typeof(PasswordHasher));
        services.AddScoped(typeof(ITokenService), typeof(TokenService));
        services.AddScoped(typeof(IUsrService), typeof(UsrService));
        services.AddScoped(typeof(IUserSubscriptionService), typeof(UserSubscriptionService));

        // ===============================
        // AutoMapper
        // ===============================
        services.AddAutoMapper(typeof(UserStoreProfile));

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
        // Message Layer (MassTransit with RabbitMQ)
        // ===============================
        // Publishers
        services.AddScoped(typeof(IUserLogPublisher), typeof(UserLogPublisher));
        services.AddScoped(typeof(IUserNotificationPublisher), typeof(UserNotificationPublisher));

        // MassTransit Setup
        services.AddUserServiceMassTransit(_configuration);

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
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "User Service",
                Version = "v3.0",
                Description = "Cloud-level user management service responsible for authentication, session tracking, and role-based access across the UNIWA STLS.",
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service");
                c.DocumentTitle = "User Service";
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
