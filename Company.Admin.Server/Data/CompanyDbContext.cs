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

        // DbSets
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<Break> Breaks { get; set; }
        public DbSet<Overtime> Overtimes { get; set; }
        public DbSet<VacationRequest> VacationRequests { get; set; }
        public DbSet<VacationPolicy> VacationPolicies { get; set; }
        public DbSet<VacationBalance> VacationBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Company
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.Website).HasMaxLength(255);
                entity.Property(c => c.Email).HasMaxLength(255);
                entity.Property(c => c.Phone).HasMaxLength(20);
                entity.Property(c => c.Address).HasMaxLength(500);
                entity.HasIndex(c => c.Name).IsUnique();
                entity.HasIndex(c => c.Email).IsUnique();
            });

            // Configuración de Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.Name).IsRequired().HasMaxLength(100);
                entity.Property(d => d.Description).HasMaxLength(500);
                
                entity.HasOne(d => d.Company)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(d => d.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(d => new { d.CompanyId, d.Name }).IsUnique();
            });

            // Configuración de Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Role).HasConversion<string>();

                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Employees)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => new { e.CompanyId, e.EmployeeCode }).IsUnique();
            });

            // Configuración de TimeRecord
            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(tr => tr.Id);
                entity.Property(tr => tr.Date).HasColumnType("date");
                entity.Property(tr => tr.TotalHours).HasColumnType("decimal(5,2)");
                entity.Property(tr => tr.RecordType).HasConversion<string>();
                entity.Property(tr => tr.Location).HasMaxLength(200);
                entity.Property(tr => tr.Notes).HasMaxLength(500);

                entity.HasOne(tr => tr.Employee)
                      .WithMany(e => e.TimeRecords)
                      .HasForeignKey(tr => tr.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(tr => new { tr.EmployeeId, tr.Date, tr.RecordType });
            });

            // Configuración de WorkSchedule
            modelBuilder.Entity<WorkSchedule>(entity =>
            {
                entity.HasKey(ws => ws.Id);
                entity.Property(ws => ws.Name).IsRequired().HasMaxLength(100);
                entity.Property(ws => ws.DayOfWeek).HasConversion<string>();

                entity.HasOne(ws => ws.Employee)
                      .WithMany()
                      .HasForeignKey(ws => ws.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ws => ws.Department)
                      .WithMany()
                      .HasForeignKey(ws => ws.DepartmentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Break
            modelBuilder.Entity<Break>(entity =>
            {
                entity.HasKey(b => b.Id);
                entity.Property(b => b.Reason).HasMaxLength(200);
                entity.Property(b => b.Notes).HasMaxLength(500);

                entity.HasOne(b => b.TimeRecord)
                      .WithMany()
                      .HasForeignKey(b => b.TimeRecordId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de Overtime
            modelBuilder.Entity<Overtime>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Date).HasColumnType("date");
                entity.Property(o => o.Hours).HasColumnType("decimal(5,2)");
                entity.Property(o => o.Reason).HasMaxLength(500);
                entity.Property(o => o.Notes).HasMaxLength(500);

                entity.HasOne(o => o.Employee)
                      .WithMany()
                      .HasForeignKey(o => o.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.ApprovedByEmployee)
                      .WithMany()
                      .HasForeignKey(o => o.ApprovedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de VacationRequest
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(vr => vr.Id);
                entity.Property(vr => vr.StartDate).HasColumnType("date");
                entity.Property(vr => vr.EndDate).HasColumnType("date");
                entity.Property(vr => vr.DaysRequested).HasColumnType("decimal(4,1)");
                entity.Property(vr => vr.Reason).IsRequired().HasMaxLength(500);
                entity.Property(vr => vr.Status).HasConversion<string>();
                entity.Property(vr => vr.Comments).HasMaxLength(500);
                entity.Property(vr => vr.ReviewComments).HasMaxLength(500);

                entity.HasOne(vr => vr.Employee)
                      .WithMany(e => e.VacationRequests)
                      .HasForeignKey(vr => vr.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(vr => vr.ReviewedByEmployee)
                      .WithMany()
                      .HasForeignKey(vr => vr.ReviewedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Configuración de VacationPolicy
            modelBuilder.Entity<VacationPolicy>(entity =>
            {
                entity.HasKey(vp => vp.Id);
                entity.Property(vp => vp.Name).IsRequired().HasMaxLength(100);
                entity.Property(vp => vp.Description).HasMaxLength(500);
                entity.Property(vp => vp.AnnualDays).HasColumnType("decimal(4,1)");
                entity.Property(vp => vp.MaxConsecutiveDays).HasColumnType("decimal(4,1)");
                entity.Property(vp => vp.MinRequestDays).HasColumnType("decimal(4,1)");

                entity.HasOne(vp => vp.Company)
                      .WithMany()
                      .HasForeignKey(vp => vp.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de VacationBalance
            modelBuilder.Entity<VacationBalance>(entity =>
            {
                entity.HasKey(vb => vb.Id);
                entity.Property(vb => vb.TotalDays).HasColumnType("decimal(4,1)");
                entity.Property(vb => vb.UsedDays).HasColumnType("decimal(4,1)");
                entity.Property(vb => vb.PendingDays).HasColumnType("decimal(4,1)");
                entity.Property(vb => vb.CarryOverDays).HasColumnType("decimal(4,1)");

                entity.HasOne(vb => vb.Employee)
                      .WithMany()
                      .HasForeignKey(vb => vb.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(vb => new { vb.EmployeeId, vb.Year }).IsUnique();
            });

            // Datos semilla para desarrollo
            if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                SeedData(modelBuilder);
            }
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Empresa de ejemplo
            modelBuilder.Entity<Company>().HasData(
                new Company
                {
                    Id = 1,
                    Name = "Empresa Demo",
                    Description = "Empresa de demostración para pruebas",
                    Email = "info@empresademo.com",
                    Phone = "+34 123 456 789",
                    Address = "Calle Principal 123, Madrid, España",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Departamentos de ejemplo
            modelBuilder.Entity<Department>().HasData(
                new Department
                {
                    Id = 1,
                    CompanyId = 1,
                    Name = "Recursos Humanos",
                    Description = "Departamento de gestión de recursos humanos",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Id = 2,
                    CompanyId = 1,
                    Name = "Desarrollo",
                    Description = "Departamento de desarrollo de software",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Department
                {
                    Id = 3,
                    CompanyId = 1,
                    Name = "Marketing",
                    Description = "Departamento de marketing y ventas",
                    Active = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );

            // Empleado administrador de ejemplo
            modelBuilder.Entity<Employee>().HasData(
                new Employee
                {
                    Id = 1,
                    CompanyId = 1,
                    DepartmentId = 1,
                    FirstName = "Admin",
                    LastName = "Sistema",
                    Email = "admin@empresademo.com",
                    Phone = "+34 123 456 700",
                    EmployeeCode = "ADM001",
                    Role = UserRole.CompanyAdmin,
                    PasswordHash = "$2a$11$123456789012345678901e", // Placeholder - se debe hashear "admin123"
                    Active = true,
                    HiredAt = DateTime.UtcNow.AddMonths(-6),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Employee && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Modified)
                {
                    ((Employee)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}