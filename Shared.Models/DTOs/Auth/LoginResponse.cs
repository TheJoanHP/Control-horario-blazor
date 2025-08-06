namespace Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Response del login de usuario
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserInfo User { get; set; } = new();
    }

    /// <summary>
    /// Información del token para el servicio JWT
    /// </summary>
    public class TokenInfo
    {
        public string Token { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public Dictionary<string, string>? AdditionalClaims { get; set; }
    }
}