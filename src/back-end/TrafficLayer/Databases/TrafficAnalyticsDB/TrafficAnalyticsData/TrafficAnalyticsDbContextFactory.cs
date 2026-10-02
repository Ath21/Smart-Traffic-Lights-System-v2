using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TrafficAnalyticsData.Settings;

namespace TrafficAnalyticsData;

public class TrafficAnalyticsDbContextFactory : IDesignTimeDbContextFactory<TrafficAnalyticsDbContext>
{
    public TrafficAnalyticsDbContext CreateDbContext(string[] args)
    {
        // You can adjust this connection for local migration execution
        const string connectionString =
            "Host=localhost;Port=5432;Database=TrafficAnalyticsDB;Username=postgres;Password=postgres123";

        var optionsBuilder = new DbContextOptionsBuilder<TrafficAnalyticsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Normally you don’t have IOptions at design-time, so we manually create settings
        var settings = Microsoft.Extensions.Options.Options.Create(new TrafficAnalyticsDbSettings
        {
            ConnectionString = connectionString
        });

        return new TrafficAnalyticsDbContext(optionsBuilder.Options, settings);
    }
}
