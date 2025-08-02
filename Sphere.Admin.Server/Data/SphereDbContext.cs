using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;

namespace Sphere.Admin.Server.Data
{
    public class SphereDbContext : DbContext
    {
        public SphereDbContext(DbContextOptions<SphereDbContext> options) : base(options)
        {
        }

        // Entidades de la base de datos central
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<SphereAdmin> SphereAdmins { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Tenant
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.LicenseType).HasDefaultValue(Shared.Models.Enums.LicenseType.Trial);
                entity.Property(e => e.MaxEmployees).HasDefaultValue(10);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índices únicos
                entity.HasIndex(e => e.Subdomain)
                      .IsUnique()
                      .HasDatabaseName("IX_Tenants_Subdomain");

                entity.HasIndex(e => e.DatabaseName)
                      .IsUnique()
                      .HasDatabaseName("IX_Tenants_DatabaseName");

                entity.HasIndex(e => e.ContactEmail)
                      .IsUnique()
                      .HasDatabaseName("IX_Tenants_ContactEmail");
            });

            // Configuración de License
            modelBuilder.Entity<License>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.MaxEmployees).IsRequired();
                entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Tenant)
                      .WithOne(t => t.License)
                      .HasForeignKey<License>(e => e.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice para consultas por tenant
                entity.HasIndex(e => new { e.TenantId, e.Active })
                      .HasDatabaseName("IX_Licenses_TenantId_Active");
            });

            // Configuración de SphereAdmin
            modelBuilder.Entity<SphereAdmin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único para email
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_SphereAdmins_Email");
            });

            // Configuración de SystemConfig
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsPublic).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único para key
                entity.HasIndex(e => e.Key)
                      .IsUnique()
                      .HasDatabaseName("IX_SystemConfigs_Key");
            });

            // Seed data inicial
            SeedInitialData(modelBuilder);
        }

        private static void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Configuraciones del sistema por defecto
            modelBuilder.Entity<SystemConfig>().HasData(
                new SystemConfig
                {
                    Id = 1,
                    Key = "SystemName",
                    Value = "Sphere Time Control",
                    Description = "Nombre del sistema",
                    IsPublic = true
                },
                new SystemConfig
                {
                    Id = 2,
                    Key = "SystemVersion",
                    Value = "1.0.0",
                    Description = "Versión del sistema",
                    IsPublic = true
                },
                new SystemConfig
                {
                    Id = 3,
                    Key = "MaxTenantsPerInstance",
                    Value = "100",
                    Description = "Máximo número de tenants por instancia",
                    IsPublic = false
                },
                new SystemConfig
                {
                    Id = 4,
                    Key = "DefaultTrialDays",
                    Value = "30",
                    Description = "Días de prueba por defecto",
                    IsPublic = false
                },
                new SystemConfig
                {
                    Id = 5,
                    Key = "MaintenanceMode",
                    Value = "false",
                    Description = "Modo de mantenimiento",
                    IsPublic = true
                }
            );

            // Admin por defecto
            modelBuilder.Entity<SphereAdmin>().HasData(
                new SphereAdmin
                {
                    Id = 1,
                    Name = "Super Admin",
                    Email = "admin@spheretimecontrol.com",
                    PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj7/N8.D8eo.", // admin123
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        // Método para guardar cambios con actualización automática de timestamps
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Tenant || e.Entity is SphereAdmin || e.Entity is SystemConfig)
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added)
                {
                    if (entityEntry.Property("CreatedAt").CurrentValue == null)
                    {
                        entityEntry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    }
                }

                if (entityEntry.Property("UpdatedAt") != null)
                {
                    entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }

    // Modelos específicos para Sphere Admin (que no están en Shared.Models)
    public class SphereAdmin
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SystemConfig
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}