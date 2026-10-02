using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TrafficLightData.Settings;

namespace TrafficLightData;

public class TrafficLightDbContextFactory : IDesignTimeDbContextFactory<TrafficLightDbContext>
{
    public TrafficLightDbContext CreateDbContext(string[] args)
    {
        // Local connection for design-time migrations
        const string connectionString =
            "Server=localhost,1433;Database=TrafficLightDB;User Id=sa;Password=MyPass@word;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<TrafficLightDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        var settings = Microsoft.Extensions.Options.Options.Create(new TrafficLightDbSettings
        {
            ConnectionString = connectionString
        });

        return new TrafficLightDbContext(optionsBuilder.Options, settings);
    }
}
