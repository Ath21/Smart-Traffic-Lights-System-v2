using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using TrafficAnalyticsData.Entities;
using TrafficAnalyticsData.Settings;

namespace TrafficAnalyticsData;

public class TrafficAnalyticsDbContext : DbContext
{
    private readonly TrafficAnalyticsDbSettings _settings;

    public TrafficAnalyticsDbContext(DbContextOptions<TrafficAnalyticsDbContext> options, IOptions<TrafficAnalyticsDbSettings> settings)
        : base(options)
    {
        _settings = settings.Value;
    }

    public DbSet<AlertEntity> Alerts { get; set; }
    public DbSet<DailySummaryEntity> DailySummaries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AlertEntity>().ToTable("alerts");
        modelBuilder.Entity<DailySummaryEntity>().ToTable("daily_summaries");

        modelBuilder.Entity<AlertEntity>().Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        modelBuilder.Entity<DailySummaryEntity>().Property(a => a.Date).HasColumnType("date");

        base.OnModelCreating(modelBuilder);
    }

    public async Task<bool> CanConnectAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(_settings.ConnectionString);
            await conn.OpenAsync();
            return conn.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }
}
