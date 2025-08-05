using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.Enums;

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
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.LicenseType).HasDefaultValue(LicenseType.Trial);
                entity.Property(e => e.MaxEmployees).HasDefaultValue(10);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índices únicos
                entity.HasIndex(e => e.Code)
                      .IsUnique()
                      .HasDatabaseName("IX_Tenants_Code");

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
                entity.Property(e => e.LicenseType).IsRequired();
                entity.Property(e => e.MaxEmployees).IsRequired();
                entity.Property(e => e.MonthlyPrice).HasPrecision(10, 2);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

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
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).HasDefaultValue(UserRole.SuperAdmin);
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
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único para key
                entity.HasIndex(e => e.Key)
                      .IsUnique()
                      .HasDatabaseName("IX_SystemConfigs_Key");
            });

            // Configuración de conversiones de enums
            modelBuilder.Entity<Tenant>()
                .Property(e => e.LicenseType)
                .HasConversion<int>();

            modelBuilder.Entity<License>()
                .Property(e => e.LicenseType)
                .HasConversion<int>();

            modelBuilder.Entity<SphereAdmin>()
                .Property(e => e.Role)
                .HasConversion<int>();
        }
    }
}