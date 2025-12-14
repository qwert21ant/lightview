using Microsoft.EntityFrameworkCore;
using Persistence.Models;

namespace Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraMetadata> CameraMetadata => Set<CameraMetadata>();
    public DbSet<CameraProfile> CameraProfiles => Set<CameraProfile>();
    public DbSet<CameraSnapshot> CameraSnapshots => Set<CameraSnapshot>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<User> Users => Set<User>();
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
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Configure relationships
            entity.HasOne(e => e.Metadata)
                .WithOne(m => m.Camera)
                .HasForeignKey<CameraMetadata>(m => m.CameraId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.Profiles)
                .WithOne(p => p.Camera)
                .HasForeignKey(p => p.CameraId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure CameraMetadata entity
        modelBuilder.Entity<CameraMetadata>(entity =>
        {
            entity.HasKey(e => e.CameraId);
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(0); // Default to Offline
            entity.Property(e => e.LastConnectedAt).IsRequired();
            
            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.CapabilitiesJson)
                .HasColumnType("jsonb")
                .HasColumnName("capabilities");
                
            entity.Property(e => e.DeviceInfoJson)
                .HasColumnType("jsonb")
                .HasColumnName("device_info");
        });
        
        // Configure CameraProfile entity
        modelBuilder.Entity<CameraProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CameraId).IsRequired();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.IsMainStream).IsRequired();
            entity.Property(e => e.RtspUri).HasMaxLength(500);
            entity.Property(e => e.WebRtcUri).HasMaxLength(500);
            
            // Configure JSON columns for PostgreSQL
            entity.Property(e => e.VideoConfigJson)
                .HasColumnType("jsonb")
                .HasColumnName("video_config")
                .HasDefaultValue("{}");
                
            entity.Property(e => e.AudioConfigJson)
                .HasColumnType("jsonb")
                .HasColumnName("audio_config")
                .HasDefaultValue("{}");
                
            // Create index on CameraId for performance
            entity.HasIndex(e => e.CameraId);
            entity.HasIndex(e => new { e.CameraId, e.Token }).IsUnique();
        });
        
        // Configure CameraSnapshot entity
        modelBuilder.Entity<CameraSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CameraId).IsRequired();
            entity.Property(e => e.ImageData).IsRequired();
            entity.Property(e => e.ProfileToken).HasMaxLength(100);
            entity.Property(e => e.CapturedAt).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            
            // Create indexes for efficient queries
            entity.HasIndex(e => e.CameraId);
            entity.HasIndex(e => new { e.CameraId, e.CapturedAt });
            
            // Configure relationship
            entity.HasOne(e => e.Camera)
                .WithMany()
                .HasForeignKey(e => e.CameraId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure SystemSetting entity
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
            
            entity.HasIndex(e => e.Name).IsUnique();
        });
        
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            
            entity.HasIndex(e => e.Username).IsUnique();
        });
    }
}
