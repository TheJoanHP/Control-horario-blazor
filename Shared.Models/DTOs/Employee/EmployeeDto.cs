using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    /// <summary>
    /// DTO para mostrar empleados
    /// </summary>
    public class EmployeeDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? DepartmentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? Position { get; set; }
        public UserRole Role { get; set; }
        public DateTime? HireDate { get; set; }
        public decimal? Salary { get; set; }
        public bool Active { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navegación
        public string? DepartmentName { get; set; }
        public string? CompanyName { get; set; }
        
        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}";
        public int? YearsOfService => HireDate.HasValue 
            ? (DateTime.UtcNow - HireDate.Value).Days / 365 
            : null;
    }
}