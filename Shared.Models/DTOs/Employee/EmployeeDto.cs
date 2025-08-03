using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool Active { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? HiredAt { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Información de departamento
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        
        // Información de empresa
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        
        // Estadísticas (opcionales)
        public int? TotalTimeRecords { get; set; }
        public decimal? VacationDaysUsed { get; set; }
        public decimal? VacationDaysAvailable { get; set; }
        
        // Estado actual
        public bool IsCheckedIn { get; set; }
        public bool IsOnBreak { get; set; }
        public DateTime? LastCheckIn { get; set; }
        public DateTime? LastCheckOut { get; set; }
    }
}