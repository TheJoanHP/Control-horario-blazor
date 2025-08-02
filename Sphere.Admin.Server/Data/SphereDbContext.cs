using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.Enums;

namespace Sphere.Admin.Server.Data
{
    public class SphereDbContext : DbContext
    {
        public SphereDbContext(DbContextOptions<SphereDbContext> options) : base(options) { }

        // Entidades centrales
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<SphereAdmin> SphereAdmins { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración Tenant
            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.Domain).IsUnique();
                entity.Property(e => e.Code).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            // Configuración License
            modelBuilder.Entity<License>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Tenant)
                      .WithOne(t => t.License)
                      .HasForeignKey<License>(e => e.TenantId);
                entity.Property(e => e.MonthlyPrice).HasColumnType("decimal(10,2)");
            });

            // Configuración SphereAdmin
            modelBuilder.Entity<SphereAdmin>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            // Seed data inicial
            SeedInitialData(modelBuilder);
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Super Admin inicial
            modelBuilder.Entity<SphereAdmin>().HasData(
                new SphereAdmin
                {
                    Id = 1,
                    Name = "Super Admin",
                    Email = "admin@sphere.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Tenant de demostración
            modelBuilder.Entity<Tenant>().HasData(
                new Tenant
                {
                    Id = 1,
                    Name = "Demo Company",
                    Code = "demo",
                    Description = "Empresa de demostración",
                    ContactEmail = "admin@demo.com",
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Licencia de demostración
            modelBuilder.Entity<License>().HasData(
                new License
                {
                    Id = 1,
                    TenantId = 1,
                    Type = LicenseType.Trial,
                    MaxEmployees = 50,
                    HasReports = true,
                    HasAPI = false,
                    HasMobileApp = true,
                    MonthlyPrice = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    Active = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}