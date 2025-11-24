using Microsoft.EntityFrameworkCore;
using Persistence.Models;

namespace Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Camera> Cameras => Set<Camera>();
    // Add other DbSets as needed

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Camera entity
        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Protocol).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.CapabilitiesJson)
                .HasColumnType("jsonb")
                .HasColumnName("capabilities");
                
            entity.Property(e => e.ProfilesJson)
                .HasColumnType("jsonb")
                .HasColumnName("profiles");
                
            entity.Property(e => e.DeviceInfoJson)
                .HasColumnType("jsonb")
                .HasColumnName("device_info");
        });
    }
}
