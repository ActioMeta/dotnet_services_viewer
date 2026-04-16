using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Node> Nodes { get; set; }
    public DbSet<MonitoredService> Services { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Node>()
            .OwnsOne(n => n.SshConfig);

        modelBuilder.Entity<Node>()
            .HasMany(n => n.Services)
            .WithOne()
            .HasForeignKey(s => s.NodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
