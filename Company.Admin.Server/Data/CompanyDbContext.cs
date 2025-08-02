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
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        
        // Entidades de control de tiempo
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
            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TaxId).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.HasIndex(e => e.TaxId).IsUnique().HasFilter("[TaxId] IS NOT NULL");
                entity.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
                
                // Configuración de valores por defecto
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.WorkStartTime).HasDefaultValue(new TimeSpan(9, 0, 0));
                entity.Property(e => e.WorkEndTime).HasDefaultValue(new TimeSpan(17, 0, 0));
                entity.Property(e => e.ToleranceMinutes).HasDefaultValue(15);
                entity.Property(e => e.VacationDaysPerYear).HasDefaultValue(22);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configuración de Department
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                
                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índice único por empresa
                entity.HasIndex(e => new { e.CompanyId, e.Name })
                      .IsUnique()
                      .HasDatabaseName("IX_Departments_CompanyId_Name");
            });

            // Configuración de Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.HiredAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Relaciones
                entity.HasOne(e => e.Company)
                      .WithMany(c => c.Employees)
                      .HasForeignKey(e => e.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Employees)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Índices únicos
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Employees_Email");

                entity.HasIndex(e => new { e.CompanyId, e.EmployeeCode })
                      .IsUnique()
                      .HasDatabaseName("IX_Employees_CompanyId_EmployeeCode");

                // Propiedad calculada para FullName
                entity.Ignore(e => e.FullName);
                entity.Ignore(e => e.WorkStartTime);
                entity.Ignore(e => e.WorkEndTime);
            });

            // Configuración de TimeRecord
            modelBuilder.Entity<TimeRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.DeviceInfo).HasMaxLength(100);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.TimeRecords)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índices para optimizar consultas
                entity.HasIndex(e => new { e.EmployeeId, e.Timestamp })
                      .HasDatabaseName("IX_TimeRecords_EmployeeId_Timestamp");

                entity.HasIndex(e => new { e.Type, e.Timestamp })
                      .HasDatabaseName("IX_TimeRecords_Type_Timestamp");

                entity.HasIndex(e => e.Timestamp)
                      .HasDatabaseName("IX_TimeRecords_Timestamp");
            });

            // Configuración de WorkSchedule
            modelBuilder.Entity<WorkSchedule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Índice para consultas por empleado y fecha efectiva
                entity.HasIndex(e => new { e.EmployeeId, e.EffectiveFrom, e.Active })
                      .HasDatabaseName("IX_WorkSchedules_Employee_Effective");
            });

            // Configuración de Break
            modelBuilder.Entity<Break>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Propiedades calculadas
                entity.Ignore(e => e.Duration);
                entity.Ignore(e => e.IsActive);

                // Índices
                entity.HasIndex(e => new { e.EmployeeId, e.StartTime })
                      .HasDatabaseName("IX_Breaks_EmployeeId_StartTime");
            });

            // Configuración de Overtime
            modelBuilder.Entity<Overtime>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Duration).IsRequired();
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.Approved).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ApprovedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ApprovedById)
                      .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(e => new { e.EmployeeId, e.Date })
                      .HasDatabaseName("IX_Overtime_EmployeeId_Date");
            });

            // Configuración de VacationRequest
            modelBuilder.Entity<VacationRequest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.DaysRequested).IsRequired();
                entity.Property(e => e.Comments).HasMaxLength(1000);
                entity.Property(e => e.Status).HasDefaultValue(Shared.Models.Enums.VacationStatus.Pending);
                entity.Property(e => e.ResponseComments).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany(emp => emp.VacationRequests)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReviewedBy)
                      .WithMany()
                      .HasForeignKey(e => e.ReviewedById)
                      .OnDelete(DeleteBehavior.SetNull);

                // Índices
                entity.HasIndex(e => new { e.EmployeeId, e.StartDate })
                      .HasDatabaseName("IX_VacationRequests_EmployeeId_StartDate");

                entity.HasIndex(e => e.Status)
                      .HasDatabaseName("IX_VacationRequests_Status");
            });

            // Configuración de VacationPolicy
            modelBuilder.Entity<VacationPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.AnnualDays).HasDefaultValue(22);
                entity.Property(e => e.MaxConsecutiveDays).HasDefaultValue(15);
                entity.Property(e => e.MinAdvanceNoticeDays).HasDefaultValue(15);
                entity.Property(e => e.RequireApproval).HasDefaultValue(true);
                entity.Property(e => e.CarryOverEnabled).HasDefaultValue(true);
                entity.Property(e => e.MaxCarryOverDays).HasDefaultValue(5);
                entity.Property(e => e.Active).HasDefaultValue(true);
                entity.Property(e => e.EffectiveFrom).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Índice único por empresa y periodo
                entity.HasIndex(e => new { e.CompanyId, e.EffectiveFrom, e.Active })
                      .HasDatabaseName("IX_VacationPolicies_Company_Effective");
            });

            // Configuración de VacationBalance
            modelBuilder.Entity<VacationBalance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Year).IsRequired();
                entity.Property(e => e.TotalDays).IsRequired();
                entity.Property(e => e.UsedDays).HasDefaultValue(0);
                entity.Property(e => e.PendingDays).HasDefaultValue(0);
                entity.Property(e => e.CarriedOverDays).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Employee)
                      .WithMany()
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Propiedades calculadas
                entity.Ignore(e => e.AvailableDays);
                entity.Ignore(e => e.RemainingDays);

                // Índice único por empleado y año
                entity.HasIndex(e => new { e.EmployeeId, e.Year })
                      .IsUnique()
                      .HasDatabaseName("IX_VacationBalances_EmployeeId_Year");
            });

            // Configurar nombres de tablas si es necesario
            ConfigureTableNames(modelBuilder);
        }

        private static void ConfigureTableNames(ModelBuilder modelBuilder)
        {
            // Si necesitas nombres de tabla específicos, configúralos aquí
            // modelBuilder.Entity<Company>().ToTable("Companies");
            // modelBuilder.Entity<Department>().ToTable("Departments");
            // etc.
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
                .Where(e => e.Entity is Company || e.Entity is Department || e.Entity is Employee || 
                           e.Entity is VacationRequest || e.Entity is VacationBalance)
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
}