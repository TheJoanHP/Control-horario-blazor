using Shared.Models.Enums;
using Shared.Models.DTOs.Auth;
using System.Security.Claims;

namespace Shared.Services.Security
{
    /// <summary>
    /// Interfaz para el servicio de gestión de tokens JWT
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Genera un token JWT para un usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="email">Email del usuario</param>
        /// <param name="role">Rol del usuario</param>
        /// <param name="additionalClaims">Claims adicionales opcionales</param>
        /// <returns>Token JWT generado</returns>
        string GenerateToken(int userId, string email, UserRole role, Dictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Crea información completa del token incluyendo refresh token
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="email">Email del usuario</param>
        /// <param name="role">Rol del usuario</param>
        /// <param name="tenantId">ID del tenant (opcional)</param>
        /// <param name="additionalClaims">Claims adicionales opcionales</param>
        /// <returns>Información completa del token</returns>
        TokenInfo CreateTokenInfo(int userId, string email, string role, string? tenantId = null, Dictionary<string, string>? additionalClaims = null);

        /// <summary>
        /// Valida un token JWT
        /// </summary>
        /// <param name="token">Token a validar</param>
        /// <returns>Claims del token si es válido, null si no es válido</returns>
        Dictionary<string, string>? ValidateToken(string token);

        /// <summary>
        /// Valida un refresh token
        /// </summary>
        /// <param name="refreshToken">Refresh token a validar</param>
        /// <returns>Claims principal si es válido, null si no es válido</returns>
        ClaimsPrincipal? ValidateRefreshToken(string refreshToken);

        /// <summary>
        /// Obtiene el ID del usuario del token
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>ID del usuario o null si el token no es válido</returns>
        int? GetUserIdFromToken(string token);

        /// <summary>
        /// Obtiene el rol del usuario del token
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>Rol del usuario o null si el token no es válido</returns>
        UserRole? GetUserRoleFromToken(string token);

        /// <summary>
        /// Verifica si un token ha expirado
        /// </summary>
        /// <param name="token">Token JWT</param>
        /// <returns>True si ha expirado, false si aún es válido</returns>
        bool IsTokenExpired(string token);

        /// <summary>
        /// Genera un refresh token
        /// </summary>
        /// <returns>Refresh token generado</returns>
        string GenerateRefreshToken();
    }
}