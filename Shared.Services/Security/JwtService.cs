using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Shared.Models.Enums;
using Shared.Models.DTOs.Auth;

namespace Shared.Services.Security
{
    /// <summary>
    /// Implementación del servicio de gestión de tokens JWT
    /// </summary>
    public class JwtService : IJwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JwtSettings:Secret"] ?? configuration["JWT:SecretKey"] ?? "SphereTimeControl-SuperSecretKey-2025-!@#$%^&*()";
            _issuer = configuration["JwtSettings:Issuer"] ?? configuration["JWT:Issuer"] ?? "SphereTimeControl";
            _audience = configuration["JwtSettings:Audience"] ?? configuration["JWT:Audience"] ?? "SphereTimeControl";
            _expirationMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? configuration["JWT:ExpirationMinutes"] ?? "480"); // 8 horas por defecto
        }

        public string GenerateToken(int userId, string email, UserRole role, Dictionary<string, string>? additionalClaims = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role.ToString()),
                new Claim("role", ((int)role).ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Agregar claims adicionales si se proporcionan
            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                {
                    claims.Add(new Claim(claim.Key, claim.Value));
                }
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public TokenInfo CreateTokenInfo(int userId, string email, string role, string? tenantId = null, Dictionary<string, string>? additionalClaims = null)
        {
            // Preparar claims adicionales
            var allClaims = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(tenantId))
                allClaims.Add("tenantId", tenantId);

            if (additionalClaims != null)
            {
                foreach (var claim in additionalClaims)
                    allClaims.Add(claim.Key, claim.Value);
            }

            // Convertir role string a enum si es necesario
            UserRole userRole;
            if (Enum.TryParse<UserRole>(role, out userRole))
            {
                // ok
            }
            else if (int.TryParse(role, out var roleInt))
            {
                userRole = (UserRole)roleInt;
            }
            else
            {
                userRole = UserRole.Employee;
            }

            var token = GenerateToken(userId, email, userRole, allClaims);
            var refreshToken = GenerateRefreshToken();

            return new TokenInfo
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                UserId = userId,
                Email = email,
                Role = userRole.ToString(),
                AdditionalClaims = allClaims
            };
        }

        public Dictionary<string, string>? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claims = new Dictionary<string, string>();

                foreach (var claim in jwtToken.Claims)
                {
                    claims[claim.Type] = claim.Value;
                }

                return claims;
            }
            catch
            {
                return null;
            }
        }

        public ClaimsPrincipal? ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secretKey);

                var principal = tokenHandler.ValidateToken(refreshToken, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            var claims = ValidateToken(token);
            if (claims != null)
            {
                if (claims.TryGetValue("userId", out var userIdStr) && int.TryParse(userIdStr, out var userId))
                    return userId;
                
                if (claims.TryGetValue(ClaimTypes.NameIdentifier, out var nameIdStr) && int.TryParse(nameIdStr, out var nameId))
                    return nameId;
            }
            return null;
        }

        public UserRole? GetUserRoleFromToken(string token)
        {
            var claims = ValidateToken(token);
            if (claims != null && claims.TryGetValue("role", out var roleStr) && int.TryParse(roleStr, out var roleInt))
                return (UserRole)roleInt;
            
            return null;
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo <= DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}