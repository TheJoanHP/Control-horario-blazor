using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.Employee
{
    public class EmployeeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int? DepartmentId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Phone { get; set; }
        public UserRole Role { get; set; } = UserRole.Employee;
        public decimal? Salary { get; set; }
        public DateTime HireDate { get; set; }
        public bool Active { get; set; }

        // AGREGADAS las propiedades que faltan
        public string? DepartmentName { get; set; }
        public string? CompanyName { get; set; }

        // Para compatibilidad con código existente
        public DepartmentInfo? Department { get; set; }
        public CompanyInfo? Company { get; set; }

        // Propiedades de horario de trabajo
        public TimeSpan? WorkStartTime { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? WorkEndTime { get; set; } = new TimeSpan(17, 0, 0);

        // Fecha de contratación (alias)
        public DateTime? HiredAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email;
        public int? YearsOfService { get; set; }

        // Sincronizar propiedades
        public void SyncProperties()
        {
            HiredAt = HireDate;
            
            if (Department != null)
            {
                DepartmentName = Department.Name;
            }
            
            if (Company != null)
            {
                CompanyName = Company.Name;
            }
        }
    }

    // Clases auxiliares para compatibilidad
    public class DepartmentInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CompanyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class CreateEmployeeDto
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Los apellidos son requeridos")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "El código de empleado no puede exceder 20 caracteres")]
        public string? EmployeeCode { get; set; }

        public int CompanyId { get; set; }

        public int? DepartmentId { get; set; }

        [StringLength(100, ErrorMessage = "El puesto no puede exceder 100 caracteres")]
        public string? Position { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Phone { get; set; }

        public DateTime? HireDate { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string? Password { get; set; }
    }

    public class UpdateEmployeeDto
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        [StringLength(255, ErrorMessage = "El email no puede exceder 255 caracteres")]
        public string? Email { get; set; }

        [StringLength(20, ErrorMessage = "El código de empleado no puede exceder 20 caracteres")]
        public string? EmployeeCode { get; set; }

        public int? DepartmentId { get; set; }

        [StringLength(100, ErrorMessage = "El puesto no puede exceder 100 caracteres")]
        public string? Position { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string? Phone { get; set; }

        public DateTime? HireDate { get; set; }

        public bool? Active { get; set; }
    }

    public class EmployeeListDto
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime HireDate { get; set; }
        public bool Active { get; set; }
        public string Status => Active ? "Activo" : "Inactivo";
    }

    public class EmployeeStatsDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public bool Active { get; set; }
        public DateTime HireDate { get; set; }
        public TimeSpan TotalHoursThisMonth { get; set; }
        public TimeSpan TotalHoursToday { get; set; }
        public int DaysWorkedThisMonth { get; set; }
        public bool IsCurrentlyWorking { get; set; }
        public bool IsOnBreak { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime? LastCheckIn { get; set; }
    }
}