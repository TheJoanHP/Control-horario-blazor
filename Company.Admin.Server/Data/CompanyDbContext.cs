using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace Company.Admin.Server.Data
{
    public class CompanyDbContext : DbContext
    {
        public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
        {
        }

        // Entidades principales
        public DbSet<Shared.Models.Core.Company> Companies { get; set; }  // ← Especificar namespace completo
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }

        // Seguimiento de tiempo
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<Break> Breaks { get; set; }
        public DbSet<Overtime> Overtimes { get; set; }

        // Vacaciones
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<VacationPolicy> VacationPolicies { get; set; }
        public DbSet<VacationBalance> VacationBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Company
            modelBuilder.Entity<Shared.Models.Core.Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Email).HasMaxLength(255);
                entity.Property(c => c.Phone).HasMaxLength(50);
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // Configuración de Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.EmployeeCode).IsUnique();

                // Relaciones
                entity.HasOne(e => e.Company)
                    .WithMany(c => c.Employees)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Employees)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
                entity.Property(d => d.Description).HasMaxLength(500);

                // Relación con Company
                entity.HasOne(d => d.Company)
                    .WithMany(c => c.Departments)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de TimeRecord
            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(tr => tr.Id);
                entity.Property(tr => tr.Location).HasMaxLength(255);
                entity.Property(tr => tr.Notes).HasMaxLength(500);

                // Relación con Employee
                entity.HasOne(tr => tr.Employee)
                    .WithMany(e => e.TimeRecords)
                    .HasForeignKey(tr => tr.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índices para consultas frecuentes
                entity.HasIndex(tr => new { tr.EmployeeId, tr.Date });
                entity.HasIndex(tr => tr.Date);
            });

            // Configuración de WorkSchedule
            modelBuilder.Entity<WorkSchedule>(entity =>
            {
                entity.HasKey(ws => ws.Id);
                entity.Property(ws => ws.Name).IsRequired().HasMaxLength(200);

                // Relación con Employee
                entity.HasOne(ws => ws.Employee)
                    .WithMany(e => e.WorkSchedules)
                    .HasForeignKey(ws => ws.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de VacationRequest
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(vr => vr.Id);
                entity.Property(vr => vr.Reason).HasMaxLength(500);
                entity.Property(vr => vr.Comments).HasMaxLength(1000);

                // Relación con Employee
                entity.HasOne(vr => vr.Employee)
                    .WithMany(e => e.VacationRequests)
                    .HasForeignKey(vr => vr.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con ApprovedBy
                entity.HasOne(vr => vr.ApprovedByEmployee)
                    .WithMany()
                    .HasForeignKey(vr => vr.ApprovedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de VacationBalance
            modelBuilder.Entity<VacationBalance>(entity =>
            {
                entity.HasKey(vb => vb.Id);

                // Relación con Employee
                entity.HasOne(vb => vb.Employee)
                    .WithMany(e => e.VacationBalances)
                    .HasForeignKey(vb => vb.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Índice único por empleado y año
                entity.HasIndex(vb => new { vb.EmployeeId, vb.Year }).IsUnique();
            });

            // Configuración de VacationPolicy
            modelBuilder.Entity<VacationPolicy>(entity =>
            {
                entity.HasKey(vp => vp.Id);
                entity.Property(vp => vp.Name).IsRequired().HasMaxLength(200);
                entity.Property(vp => vp.Description).HasMaxLength(1000);

                // Relación con Company
                entity.HasOne(vp => vp.Company)
                    .WithMany(c => c.VacationPolicies)
                    .HasForeignKey(vp => vp.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de valores por defecto
            ConfigureDefaultValues(modelBuilder);
        }

        private void ConfigureDefaultValues(ModelBuilder modelBuilder)
        {
            // Valores por defecto para fechas
            modelBuilder.Entity<Employee>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Department>()
                .Property(d => d.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<TimeRecord>()
                .Property(tr => tr.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<VacationRequest>()
                .Property(vr => vr.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Valores por defecto para campos booleanos
            modelBuilder.Entity<Employee>()
                .Property(e => e.Active)
                .HasDefaultValue(true);

            modelBuilder.Entity<Department>()
                .Property(d => d.Active)
                .HasDefaultValue(true);

            modelBuilder.Entity<Shared.Models.Core.Company>()
                .Property(c => c.Active)
                .HasDefaultValue(true);
        }
    }
}