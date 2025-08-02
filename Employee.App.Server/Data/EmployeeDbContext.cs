using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace EmployeeApp.Server.Data
{
    public class EmployeeDbContext : DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : base(options) { }

        // Solo las entidades que necesita la app de empleados
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Shared.Models.Core.Company> Companies { get; set; }
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones esenciales para empleados
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Employee)
                      .HasForeignKey<Employee>(e => e.UserId);
                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Employees)
                      .HasForeignKey(e => e.CompanyId);
            });

            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.TimeRecords)
                      .HasForeignKey(e => e.EmployeeId);
                entity.HasIndex(e => new { e.EmployeeId, e.Timestamp });
            });

            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.VacationRequests)
                      .HasForeignKey(e => e.EmployeeId);
            });

            modelBuilder.Entity<Shared.Models.Core.Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });
        }
    }
}