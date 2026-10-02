using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UserData.Settings;

namespace UserData;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=localhost,1433;Database=UserDB;User Id=sa;Password=MyPass@word;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        var settings = Microsoft.Extensions.Options.Options.Create(new UserDbSettings
        {
            ConnectionString = connectionString
        });

        return new UserDbContext(optionsBuilder.Options, settings);
    }
}
