using System.Security.Claims;

namespace Shared.Services.Security
{
    public interface IJwtService
    {
        /// <summary>
        /// Genera un token JWT para un usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="email">Email del usuario</param>
        /// <param name="role">Rol del usuario</param>
        /// <param name="additionalClaims">Claims adicionales</param>
        /// <returns>Token JWT</returns>
        string GenerateToken(int userId, string email, string role, Dictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Valida un token JWT y retorna los claims
        /// </summary>
        /// <param name="token">Token a validar</param>
        /// <returns>ClaimsPrincipal si es válido, null si no</returns>
        ClaimsPrincipal? ValidateToken(string token);

        /// <summary>
        /// Verifica si un token ha expirado
        /// </summary>
        /// <param name="token">Token a verificar</param>
        /// <returns>True si ha expirado</returns>
        bool IsTokenExpired(string token);

        /// <summary>
        /// Obtiene el ID de usuario desde un token
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>ID del usuario o null</returns>
        int? GetUserIdFromToken(string token);

        /// <summary>
        /// Obtiene el email desde un token
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>Email del usuario o null</returns>
        string? GetEmailFromToken(string token);
    }
}