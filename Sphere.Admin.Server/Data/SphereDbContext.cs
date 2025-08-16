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

            // *** CONFIGURACIÓN SIMPLIFICADA: SphereAdmin ***
            modelBuilder.Entity<SphereAdmin>(entity =>
            {
                entity.ToTable("SphereAdmins");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).HasDefaultValue(UserRole.SuperAdmin);
                entity.Property(e => e.Active).HasDefaultValue(true);
                
                // Mapeo de columnas
                entity.Property(e => e.LastLogin).HasColumnName("LastLoginAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

                // Índice único para email
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // *** CONFIGURACIÓN SIMPLIFICADA: SystemConfig ***
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.ToTable("SystemConfigs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.IsEditable).HasDefaultValue(true);

                // Índice único para key
                entity.HasIndex(e => e.Key).IsUnique();
            });

            // *** CONFIGURACIÓN SIMPLIFICADA: Tenant ***
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenants");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(10,2)");

                // Índices únicos básicos
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.HasIndex(e => e.ContactEmail).IsUnique();

                // Ignorar TODAS las propiedades de navegación por ahora
                entity.Ignore(e => e.License);
                entity.Ignore(e => e.Licenses);
            });

            // *** CONFIGURACIÓN SIMPLIFICADA: License ***
            modelBuilder.Entity<License>(entity =>
            {
                entity.ToTable("Licenses");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TenantId).IsRequired();
                entity.Property(e => e.LicenseType).IsRequired();
                entity.Property(e => e.MaxEmployees).IsRequired();
                entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.HasReports).HasDefaultValue(false);
                entity.Property(e => e.HasAdvancedReports).HasDefaultValue(false);
                entity.Property(e => e.HasMobileApp).HasDefaultValue(false);
                entity.Property(e => e.HasAPI).HasDefaultValue(false);
                entity.Property(e => e.HasGeolocation).HasDefaultValue(false);

                // Índices básicos
                entity.HasIndex(e => e.TenantId);
                entity.HasIndex(e => e.Active);

                // Ignorar navegación por ahora
                entity.Ignore(e => e.Tenant);

                // Relación simple con Foreign Key PERO SIN navegación
                entity.HasOne<Tenant>()
                      .WithMany()
                      .HasForeignKey(l => l.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // *** CONFIGURACIÓN DE CONVERSIONES DE ENUMS ***
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