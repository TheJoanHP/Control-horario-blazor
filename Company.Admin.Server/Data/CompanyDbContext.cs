using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;
using Shared.Models.Enums;

namespace Company.Admin.Server.Data
{
    public class CompanyDbContext : DbContext
    {
        public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options)
        {
        }

        // Entidades principales
        public DbSet<Shared.Models.Core.Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<User> Users { get; set; }

        // Entidades de tiempo
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<Break> Breaks { get; set; }
        public DbSet<Overtime> Overtimes { get; set; }

        // Entidades de vacaciones
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<VacationPolicy> VacationPolicies { get; set; }
        public DbSet<VacationBalance> VacationBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Company
            modelBuilder.Entity<Shared.Models.Core.Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único para email
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Companies_Email");
            });

            // Configuración de Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).HasDefaultValue(UserRole.Employee);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único para email
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email");
            });

            // Configuración de Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Position).HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithOne(u => u.Employee)
                      .HasForeignKey<Employee>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Employees)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Índices únicos
                entity.HasIndex(e => new { e.CompanyId, e.EmployeeCode })
                      .IsUnique()
                      .HasDatabaseName("IX_Employees_CompanyId_EmployeeCode");
            });

            // Configuración de TimeRecord
            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Time).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.TimeRecords)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices para consultas frecuentes
                entity.HasIndex(e => new { e.EmployeeId, e.Date })
                      .HasDatabaseName("IX_TimeRecords_EmployeeId_Date");

                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_TimeRecords_Timestamp");
            });

            // Configuración de WorkSchedule
            modelBuilder.Entity<WorkSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Company)
                      .WithMany(c => c.WorkSchedules)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Break
            modelBuilder.Entity<Break>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.WorkSchedule)
                      .WithMany(ws => ws.Breaks)
                      .HasForeignKey(e => e.WorkScheduleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de VacationRequest
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.TotalDays).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue(VacationStatus.Pending);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.VacationRequests)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de VacationPolicy
            modelBuilder.Entity<VacationPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AnnualDays).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Company)
                      .WithMany(c => c.VacationPolicies)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de VacationBalance
            modelBuilder.Entity<VacationBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.TotalDays).IsRequired();
                entity.Property(e => e.UsedDays).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.VacationBalances)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice único por empleado y año
                entity.HasIndex(e => new { e.EmployeeId, e.Year })
                      .IsUnique()
                      .HasDatabaseName("IX_VacationBalances_EmployeeId_Year");
            });

            // Configuración de conversiones de enums
            modelBuilder.Entity<User>()
                .Property(e => e.Role)
                .HasConversion<int>();

            modelBuilder.Entity<TimeRecord>()
                .Property(e => e.Type)
                .HasConversion<int>();

            modelBuilder.Entity<VacationRequest>()
                .Property(e => e.Status)
                .HasConversion<int>();
        }
    }
}