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

            // *** CONFIGURACIÓN: TENANT ***
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("Tenants");
                entity.HasKey(e => e.Id);
                
                // Propiedades básicas
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Subdomain).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ContactEmail).IsRequired().HasMaxLength(255);
                entity.Property(e => e.DatabaseName).IsRequired().HasMaxLength(200);
                
                // Configuración de propiedades decimales
                entity.Property(e => e.MonthlyPrice)
                    .HasColumnType("decimal(10,2)")
                    .HasDefaultValue(0.00m);
                
                // Configuración de fechas
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Índices únicos
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Subdomain).IsUnique();
                entity.HasIndex(e => e.ContactEmail).IsUnique();
                
                // *** IGNORAR COLECCIÓN LICENSES PARA EVITAR CONFLICTOS ***
                entity.Ignore(e => e.Licenses);
            });

            // *** CONFIGURACIÓN: LICENSE ***
            modelBuilder.Entity<License>(entity =>
            {
                entity.ToTable("Licenses");
                entity.HasKey(e => e.Id);
                
                // Propiedades básicas
                entity.Property(e => e.TenantId).IsRequired();
                entity.Property(e => e.LicenseType).IsRequired();
                entity.Property(e => e.MaxEmployees).IsRequired();
                
                // Configuración de precios
                entity.Property(e => e.MonthlyPrice)
                    .HasColumnType("decimal(10,2)")
                    .IsRequired();
                
                // Configuración de fechas
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Valores por defecto para características
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.HasReports).HasDefaultValue(false);
                entity.Property(e => e.HasAdvancedReports).HasDefaultValue(false);
                entity.Property(e => e.HasMobileApp).HasDefaultValue(false);
                entity.Property(e => e.HasAPI).HasDefaultValue(false);
                entity.Property(e => e.HasGeolocation).HasDefaultValue(false);
                
                // *** RELACIÓN CON TENANT (N:1) ***
                entity.HasOne(l => l.Tenant)
                    .WithOne(t => t.License)
                    .HasForeignKey<License>(l => l.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // *** CONFIGURACIÓN: SPHERE ADMIN ***
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
                
                // Mapeo de columnas con nombres específicos
                entity.Property(e => e.LastLogin).HasColumnName("LastLoginAt");
                entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt");
                entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");

                // Índice único para email
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // *** CONFIGURACIÓN: SYSTEM CONFIG ***
            modelBuilder.Entity<SystemConfig>(entity =>
            {
                entity.ToTable("SystemConfigs");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.IsEditable).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                
                // Índice único para la clave
                entity.HasIndex(e => e.Key).IsUnique();
            });
        }
    }
}