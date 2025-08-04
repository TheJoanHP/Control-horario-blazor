using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Auth
{
    /// <summary>
    /// Request para verificar token
    /// </summary>
    public class TokenRequest
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;
    }
}