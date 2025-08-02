using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace Company.Admin.Server.Data
{
    public class CompanyDbContext : DbContext
    {
        public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options) { }

        // Entidades del tenant
        public DbSet<User> Users { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Shared.Models.Core.Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Name).IsRequired();
            });

            // Configuración Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithOne(u => u.Employee)
                      .HasForeignKey<Employee>(e => e.UserId);
                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Employees)
                      .HasForeignKey(e => e.CompanyId);
                entity.HasIndex(e => e.EmployeeCode).IsUnique();
            });

            // Configuración Company
            modelBuilder.Entity<Shared.Models.Core.Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });

            // Configuración TimeRecord
            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.TimeRecords)
                      .HasForeignKey(e => e.EmployeeId);
                entity.HasIndex(e => new { e.EmployeeId, e.Timestamp });
            });

            // Configuración VacationRequest
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.VacationRequests)
                      .HasForeignKey(e => e.EmployeeId);
            });

            // Configuración Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(e => e.CompanyId);
            });
        }
    }
}