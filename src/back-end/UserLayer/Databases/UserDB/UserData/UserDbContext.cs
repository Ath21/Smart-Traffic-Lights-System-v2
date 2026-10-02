using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UserData.Entities;
using UserData.Settings;

namespace UserData;

public class UserDbContext : DbContext
{
    private readonly UserDbSettings _settings;

    public UserDbContext(DbContextOptions<UserDbContext> options, IOptions<UserDbSettings> settings)
        : base(options)
    {
        _settings = settings.Value;
    }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<SessionEntity> Sessions { get; set; }
    public DbSet<UserAuditEntity> UserAudits { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Relationships
        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.Sessions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserEntity>()
            .HasMany(u => u.Audits)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Default timestamps
        modelBuilder.Entity<UserEntity>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<SessionEntity>()
            .Property(s => s.LoginTime)
            .HasDefaultValueSql("GETUTCDATE()");

        modelBuilder.Entity<UserAuditEntity>()
            .Property(a => a.Timestamp)
            .HasDefaultValueSql("GETUTCDATE()");

        // Seed Users
        modelBuilder.Entity<UserEntity>().HasData(
            new UserEntity
            {
                UserId = 1,
                Username = "vathanasiou",
                Email = "ice19390005@uniwa.gr",
                PasswordHash = "10000.vFdpYw0mEjr8PGPY7dHw1w==.nJrA50wXkN3/JRyR1xz2/6Yd4Z4RrdCC8scvkoU7U9M=", // qaz123!@# 
                Role = "Admin",
                IsActive = true,
                CreatedAt = new DateTime(2024, 01, 01)
            },
            new UserEntity
            {
                UserId = 2,
                Username = "vmamalis",
                Email = "billath131908@gmail.com",
                PasswordHash = "10000.jT4SfxQOk2Jxvax8zGqx7A==.oO3A2oS1LQEMzPBI4Dnk6xNCDbDsKkQJzJ7yIh0V+eY=", // vmamalis123!@#
                Role = "TrafficOperator",
                IsActive = true,
                CreatedAt = new DateTime(2024, 01, 01)
            },
            new UserEntity
            {
                UserId = 3,
                Username = "apostolos",
                Email = "athinnovations@gmail.com",
                PasswordHash = "10000.uzBGBmVpi6oLdRIfJ8wcVQ==.Zw+yGumJf7tXvTnPQ03x9obrcvQwFZcPiC64ZXe+Pp8=", // apostolos123!@#
                Role = "User",
                IsActive = true,
                CreatedAt = new DateTime(2024, 01, 01)
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
