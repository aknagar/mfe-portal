using AugmentService.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AugmentService.Infrastructure.Data;

/// <summary>
/// Database context for user authorization (roles and permissions).
/// </summary>
public class UserDbContext : DbContext
{
    private readonly IOptions<InfrastructureConfig> _config;

    public UserDbContext(
        DbContextOptions<UserDbContext> options,
        IOptions<InfrastructureConfig> config)
        : base(options)
    {
        _config = config;
    }

    /// <summary>
    /// Users table.
    /// </summary>
    public DbSet<User> Users { get; set; } = default!;

    /// <summary>
    /// Roles table with JSONB permissions.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = default!;

    /// <summary>
    /// UserRoles join table for many-to-many relationship.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder
                .UseNpgsql(_config.Value.ConnectionString)
                .EnableSensitiveDataLogging(_config.Value.EnableSensitiveDataLogging);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.CreatedDate).IsRequired();
        });

        // Configure Role entity with JSONB permissions
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Name).IsUnique();
            entity.HasIndex(r => r.IsActive);
            entity.HasIndex(r => r.Rank);
            
            entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Description).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Rank).IsRequired();
            entity.Property(r => r.CreatedDate).IsRequired();

            // Configure Permissions as JSONB column
            entity.Property(r => r.Permissions)
                .HasColumnType("jsonb")
                .IsRequired();

            // Add constraint for Rank range (1-999)
            entity.ToTable(t => t.HasCheckConstraint("CHK_Roles_Rank", "\"Rank\" >= 1 AND \"Rank\" <= 999"));
        });

        // Configure UserRole join entity
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => ur.Id);
            
            // Composite unique index on UserId and RoleId to prevent duplicate assignments
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            entity.HasIndex(ur => ur.UserId);
            entity.HasIndex(ur => ur.RoleId);

            entity.Property(ur => ur.UserId).IsRequired();
            entity.Property(ur => ur.RoleId).IsRequired();
            entity.Property(ur => ur.CreatedDate).IsRequired();

            // Configure relationships
            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed initial roles using Permissions.cs definitions
        SeedRoles(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with initial role definitions from Permissions.cs.
    /// </summary>
    private void SeedRoles(ModelBuilder modelBuilder)
    {
        var roles = Core.Permissions.Roles.GetAllRoles();

        foreach (var roleDef in roles)
        {
            modelBuilder.Entity<Role>().HasData(new
            {
                Id = roleDef.Id,
                Name = roleDef.Name,
                Description = roleDef.Description,
                Permissions = roleDef.Permissions,
                Rank = roleDef.Rank,
                IsActive = roleDef.IsActive,
                CreatedDate = DateTime.UtcNow
            });
        }
    }
}
