using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TrafficLightData.Entities;
using TrafficLightData.Settings;

namespace TrafficLightData;

public class TrafficLightDbContext : DbContext
{
    private readonly TrafficLightDbSettings _settings;

    public TrafficLightDbContext(DbContextOptions<TrafficLightDbContext> options, IOptions<TrafficLightDbSettings> settings)
        : base(options)
    {
        _settings = settings.Value;
    }

    public DbSet<IntersectionEntity> Intersections { get; set; }
    public DbSet<TrafficLightEntity> TrafficLights { get; set; }
    public DbSet<TrafficConfigurationEntity> TrafficConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // === RELATIONSHIPS ===
        modelBuilder.Entity<IntersectionEntity>()
            .HasMany(i => i.TrafficLights)
            .WithOne(t => t.Intersection)
            .HasForeignKey(t => t.IntersectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // === COLUMN CONFIG ===
        modelBuilder.Entity<IntersectionEntity>()
            .Property(i => i.Latitude).HasColumnType("decimal(10,7)");
        modelBuilder.Entity<IntersectionEntity>()
            .Property(i => i.Longitude).HasColumnType("decimal(10,7)");
        modelBuilder.Entity<TrafficLightEntity>()
            .Property(l => l.Latitude).HasColumnType("decimal(10,7)");
        modelBuilder.Entity<TrafficLightEntity>()
            .Property(l => l.Longitude).HasColumnType("decimal(10,7)");

        modelBuilder.Entity<IntersectionEntity>()
            .Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
        modelBuilder.Entity<TrafficConfigurationEntity>()
            .Property(c => c.LastUpdated)
            .HasDefaultValueSql("GETUTCDATE()");

        // === SEED DATA ===
        var seedCreatedAt = new DateTime(2025, 10, 8, 7, 0, 0, DateTimeKind.Utc);

        // --- Intersections ---
        modelBuilder.Entity<IntersectionEntity>().HasData(
            new IntersectionEntity { IntersectionId = 1, Name = "Agiou Spyridonos", Location = "Agiou Spyridonos & Dimitsanas Street", Latitude = 38.004677m, Longitude = 23.676086m, LightCount = 2, MappedLightIdsJson = "[101, 102]", IsActive = true, CreatedAt = seedCreatedAt },
            new IntersectionEntity { IntersectionId = 2, Name = "Anatoliki Pyli", Location = "Eastern Gate", Latitude = 38.003558m, Longitude = 23.678042m, LightCount = 2, MappedLightIdsJson = "[201, 202]", IsActive = true, CreatedAt = seedCreatedAt },
            new IntersectionEntity { IntersectionId = 3, Name = "Dytiki Pyli", Location = "Western Gate", Latitude = 38.002644m, Longitude = 23.674499m, LightCount = 3, MappedLightIdsJson = "[301, 302, 303]", IsActive = true, CreatedAt = seedCreatedAt },
            new IntersectionEntity { IntersectionId = 4, Name = "Ekklisia", Location = "Church Intersection", Latitude = 38.001580m, Longitude = 23.673638m, LightCount = 3, MappedLightIdsJson = "[401, 402, 403]", IsActive = true, CreatedAt = seedCreatedAt },
            new IntersectionEntity { IntersectionId = 5, Name = "Kentriki Pyli", Location = "Central Gate", Latitude = 38.004456m, Longitude = 23.676483m, LightCount = 2, MappedLightIdsJson = "[501, 502]", IsActive = true, CreatedAt = seedCreatedAt }
        );

        // --- Traffic Lights ---
        modelBuilder.Entity<TrafficLightEntity>().HasData(
            // [1] Agiou Spyridonos
            new TrafficLightEntity { LightId = 101, IntersectionId = 1, LightName = "agiou-spyridonos101", Direction = "Agiou Spyridonos", Latitude = 38.004685m, Longitude = 23.676139m, IsOperational = true },
            new TrafficLightEntity { LightId = 102, IntersectionId = 1, LightName = "dimitsanas102", Direction = "Dimitsanas", Latitude = 38.004640m, Longitude = 23.676094m, IsOperational = true },

            // [2] Anatoliki Pyli
            new TrafficLightEntity { LightId = 201, IntersectionId = 2, LightName = "anatoliki-pyli201", Direction = "Anatoliki Pyli", Latitude = 38.003549m, Longitude = 23.677997m, IsOperational = true },
            new TrafficLightEntity { LightId = 202, IntersectionId = 2, LightName = "agiou-spyridonos202", Direction = "Agiou Spyridonos", Latitude = 38.003570m, Longitude = 23.678093m, IsOperational = true },

            // [3] Dytiki Pyli
            new TrafficLightEntity { LightId = 301, IntersectionId = 3, LightName = "dytiki-pyli301", Direction = "Dytiki Pyli", Latitude = 38.002648m, Longitude = 23.674531m, IsOperational = true },
            new TrafficLightEntity { LightId = 302, IntersectionId = 3, LightName = "dimitsanas-north302", Direction = "Dimitsanas North", Latitude = 38.002696m, Longitude = 23.674498m, IsOperational = true },
            new TrafficLightEntity { LightId = 303, IntersectionId = 3, LightName = "dimitsanas-south303", Direction = "Dimitsanas South", Latitude = 38.002606m, Longitude = 23.674487m, IsOperational = true },

            // [4] Ekklisia
            new TrafficLightEntity { LightId = 401, IntersectionId = 4, LightName = "dimitsanas401", Direction = "Dimitsanas", Latitude = 38.001626m, Longitude = 23.673627m, IsOperational = true },
            new TrafficLightEntity { LightId = 402, IntersectionId = 4, LightName = "edessis402", Direction = "Edessis", Latitude = 38.001583m, Longitude = 23.673566m, IsOperational = true },
            new TrafficLightEntity { LightId = 403, IntersectionId = 4, LightName = "korytsas403", Direction = "Korytsas", Latitude = 38.001596m, Longitude = 23.673686m, IsOperational = true },

            // [5] Kentriki Pyli
            new TrafficLightEntity { LightId = 501, IntersectionId = 5, LightName = "kentriki-pyli501", Direction = "Kentriki Pyli", Latitude = 38.004447m, Longitude = 23.676453m, IsOperational = true },
            new TrafficLightEntity { LightId = 502, IntersectionId = 5, LightName = "agiou-spyridonos502", Direction = "Agiou Spyridonos", Latitude = 38.004467m, Longitude = 23.676528m, IsOperational = true }
        );

        // --- Traffic Configurations ---
        modelBuilder.Entity<TrafficConfigurationEntity>().HasData(
            new TrafficConfigurationEntity
            {
                ConfigurationId = 1,
                Mode = "Standard",
                CycleDurationSec = 60,
                GlobalOffsetSec = 10,
                PhaseDurationsJson = "{\"Green\":40, \"Yellow\":5, \"Red\":15}",
                Purpose = "Balanced baseline cycle. 40 s green handles moderate mixed traffic.",
                LastUpdated = new DateTime(2025,10,8,7,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 2,
                Mode = "Peak",
                CycleDurationSec = 75,
                GlobalOffsetSec = 20,
                PhaseDurationsJson = "{\"Green\":50, \"Yellow\":5, \"Red\":20}",
                Purpose = "Longer green for heavy traffic periods.",
                LastUpdated = new DateTime(2025,10,8,17,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 3,
                Mode = "Night",
                CycleDurationSec = 50,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":15, \"Yellow\":5, \"Red\":30}",
                Purpose = "Short green, long red for minimal traffic.",
                LastUpdated = new DateTime(2025,10,8,23,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 4,
                Mode = "Emergency",
                CycleDurationSec = 30,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":25, \"Yellow\":3, \"Red\":2}",
                Purpose = "Immediate priority for emergency vehicles.",
                LastUpdated = new DateTime(2025,10,8,10,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 5,
                Mode = "PublicTransport",
                CycleDurationSec = 65,
                GlobalOffsetSec = 10,
                PhaseDurationsJson = "{\"Green\":45, \"Yellow\":5, \"Red\":15}",
                Purpose = "Extended green for bus approach.",
                LastUpdated = new DateTime(2025,10,8,9,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 6,
                Mode = "Pedestrian",
                CycleDurationSec = 40,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":20, \"Yellow\":5, \"Red\":15}",
                Purpose = "Half-cycle pedestrian focus.",
                LastUpdated = new DateTime(2025,10,9,17,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 7,
                Mode = "Cyclist",
                CycleDurationSec = 50,
                GlobalOffsetSec = 5,
                PhaseDurationsJson = "{\"Green\":30, \"Yellow\":5, \"Red\":15}",
                Purpose = "Cycle optimized for bicycle flow.",
                LastUpdated = new DateTime(2025,10,9,8,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 8,
                Mode = "Incident",
                CycleDurationSec = 20,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":0, \"Yellow\":0, \"Red\":20}",
                Purpose = "Lock red in case of crash or obstruction.",
                LastUpdated = new DateTime(2025,10,9,18,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 9,
                Mode = "Manual",
                CycleDurationSec = 60,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":20, \"Yellow\":5, \"Red\":35}",
                Purpose = "Operator control mode.",
                LastUpdated = new DateTime(2025,10,10,12,0,0,DateTimeKind.Utc)
            },
            new TrafficConfigurationEntity
            {
                ConfigurationId = 10,
                Mode = "Failover",
                CycleDurationSec = 10,
                GlobalOffsetSec = 0,
                PhaseDurationsJson = "{\"Green\":2, \"Yellow\":3, \"Red\":5}",
                Purpose = "Flashing yellow fallback mode.",
                LastUpdated = new DateTime(2025,10,10,12,5,0,DateTimeKind.Utc)
            }
        );

        base.OnModelCreating(modelBuilder);
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            return await Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
