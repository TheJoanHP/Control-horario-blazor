using Shared.Models.Enums;

namespace Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Información del usuario autenticado
    /// </summary>
    public class UserInfo
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? TenantId { get; set; }
        public string? CompanyName { get; set; }
        public DateTime? LastLogin { get; set; }

        // Propiedades calculadas
        public string FullName => $"{FirstName} {LastName}";
        
        public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
        public bool IsCompanyAdmin => Role == UserRole.CompanyAdmin;
        public bool IsSupervisor => Role == UserRole.Supervisor;
        public bool IsEmployee => Role == UserRole.Employee;
        
        public bool CanManageEmployees => Role == UserRole.CompanyAdmin || Role == UserRole.Supervisor;
        public bool CanViewReports => Role != UserRole.Employee;
    }
}