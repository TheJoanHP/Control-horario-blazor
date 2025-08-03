using Shared.Models.Enums;

namespace Shared.Models.DTOs.Auth
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserInfo? User { get; set; }
    }
    
    public class UserInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool Active { get; set; }
        public DateTime? LastLogin { get; set; }
        public string? DepartmentName { get; set; }
        public string? CompanyName { get; set; }
        public EmployeeInfo? Employee { get; set; }
    }
    
    public class EmployeeInfo
    {
        public int Id { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public DateTime? HiredAt { get; set; }
        public CompanyInfo? Company { get; set; }
    }
    
    public class CompanyInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? TaxId { get; set; }
    }
}