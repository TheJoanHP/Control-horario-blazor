using System.Security.Claims;
using Shared.Models.DTOs.Auth;

namespace Shared.Services.Security
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string email, string role, string tenantId, Dictionary<string, string>? additionalClaims = null);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        TokenInfo CreateTokenInfo(int userId, string email, string role, string tenantId, Dictionary<string, string>? additionalClaims = null);
        bool IsTokenExpired(string token);
        string? GetClaimValue(string token, string claimType);
    }
}