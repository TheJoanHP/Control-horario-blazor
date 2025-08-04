using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Enums;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace Shared.Models.Core
{
    /// <summary>
    /// Representa un empleado en el sistema
    /// </summary>
    [Table("Employees")]
    public class Employee
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? DepartmentId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Position { get; set; }

        public UserRole Role { get; set; } = UserRole.Employee;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime? HireDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Salary { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public virtual ICollection<TimeRecord> TimeRecords { get; set; } = new List<TimeRecord>();
        public virtual ICollection<WorkSchedule> WorkSchedules { get; set; } = new List<WorkSchedule>();
        public virtual ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
        public virtual ICollection<VacationBalance> VacationBalances { get; set; } = new List<VacationBalance>();

        // Propiedades calculadas
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public int? YearsOfService => HireDate.HasValue 
            ? (DateTime.UtcNow - HireDate.Value).Days / 365 
            : null;

        // Alias para compatibilidad
        [NotMapped]
        public DateTime? HiredAt
        {
            get => HireDate;
            set => HireDate = value;
        }

        [NotMapped]
        public DateTime? LastLogin
        {
            get => LastLoginAt;
            set => LastLoginAt = value;
        }
    }
}