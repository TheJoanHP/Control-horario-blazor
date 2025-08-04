namespace Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Response del login de usuario
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public UserInfo? User { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}