using Shared.Models.Enums;

namespace Shared.Models.DTOs.Auth
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Username { get; set; }
        public UserRole Role { get; set; }
        public bool Active { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Información adicional para empleados
        public string? DepartmentName { get; set; }
        public string? Position { get; set; }
        public string? EmployeeCode { get; set; }

        // AGREGADA la propiedad que falta
        public string? CompanyName { get; set; }
        
        // Navegación 
        public EmployeeInfo? Employee { get; set; }
        public CompanyInfo? Company { get; set; }

        // Propiedades calculadas
        public string FullName 
        { 
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var parts = value.Split(' ', 2);
                    FirstName = parts[0];
                    LastName = parts.Length > 1 ? parts[1] : "";
                }
            }
        }

        public string DisplayName => !string.IsNullOrEmpty(FullName) ? FullName : Email;
    }

    public class EmployeeInfo
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? EmployeeNumber { get; set; }
        public string? Position { get; set; }
        public string? DepartmentName { get; set; }
        public int? DepartmentId { get; set; }
        public DateTime? HireDate { get; set; }
        public bool Active { get; set; }
    }

    public class CompanyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Subdomain { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool Active { get; set; } = true;
    }
}