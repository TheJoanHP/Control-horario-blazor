using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;
using Shared.Models.TimeTracking;
using Shared.Models.Vacations;

namespace Shared.Models.Core
{
    public class Employee
    {
        public int Id { get; set; }
        
        public int CompanyId { get; set; }
        
        public int? DepartmentId { get; set; }
        
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;
        
        [Required, MaxLength(50)]
        public string LastName { get; set; } = string.Empty;
        
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(50)]
        public string EmployeeCode { get; set; } = string.Empty;
        
        public UserRole Role { get; set; } = UserRole.Employee;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public bool Active { get; set; } = true;
        
        public DateTime? LastLoginAt { get; set; }
        
        // Configuración personal de horarios
        public TimeSpan? CustomWorkStartTime { get; set; }
        public TimeSpan? CustomWorkEndTime { get; set; }
        
        public DateTime HiredAt { get; set; } = DateTime.UtcNow;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}";
        
        public TimeSpan WorkStartTime => CustomWorkStartTime ?? Company?.WorkStartTime ?? new TimeSpan(9, 0, 0);
        public TimeSpan WorkEndTime => CustomWorkEndTime ?? Company?.WorkEndTime ?? new TimeSpan(17, 0, 0);
        
        // Navegación
        public Company Company { get; set; } = null!;
        public Department? Department { get; set; }
        public ICollection<TimeRecord> TimeRecords { get; set; } = new List<TimeRecord>();
        public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
    }
}