using Shared.Models.Enums;

namespace Shared.Models.DTOs.Auth
{
    public class UserInfo
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool Active { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? LastLogin { get; set; }
        public EmployeeInfo? Employee { get; set; }
        public CompanyInfo? Company { get; set; }
    }

    public class EmployeeInfo
    {
        public int Id { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime HireDate { get; set; }
        public bool Active { get; set; }
    }

    public class CompanyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool Active { get; set; }
    }
}