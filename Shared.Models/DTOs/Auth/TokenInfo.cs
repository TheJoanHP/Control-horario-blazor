using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Auth
{
    public class TokenInfo
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
    }
    
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "El token de renovación es requerido")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}