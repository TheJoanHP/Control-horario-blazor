using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public string? TenantCode { get; set; }
    }
}