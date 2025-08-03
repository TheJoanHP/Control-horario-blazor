using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shared.Services.Security
{
    public class JwtService : IJwtService
    {
        private readonly string _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtService(IConfiguration configuration)
        {
            _key = configuration["JwtSettings:SecretKey"] ?? configuration["Jwt:Key"] ?? "ThisIsAVeryLongSecretKeyForJwtTokenGenerationAndValidation123456789";
            _issuer = configuration["JwtSettings:Issuer"] ?? configuration["Jwt:Issuer"] ?? "SphereTimeControl";
            _audience = configuration["JwtSettings:Audience"] ?? configuration["Jwt:Audience"] ?? "CompanyAdmin";
            _expirationMinutes = int.Parse(configuration["JwtSettings:ExpirationMinutes"] ?? configuration["Jwt:ExpirationMinutes"] ?? "480");
        }

        public string GenerateToken(int userId, string email, string role, Dictionary<string, string>? additionalClaims = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_key);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Agregar claims adicionales
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

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_key);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public bool IsTokenExpired(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                return jwt.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        public string? GetEmailFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwt = tokenHandler.ReadJwtToken(token);
                var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
                
                return emailClaim?.Value;
            }
            catch
            {
                return null;
            }
        }

        // Métodos adicionales para compatibilidad con EmployeeDto (mantener funcionalidad extendida)
        public async Task<string> GenerateTokenAsync(Shared.Models.DTOs.Employee.EmployeeDto employee)
        {
            var additionalClaims = new Dictionary<string, string>
            {
                ["employee_code"] = employee.EmployeeCode,
                ["company_id"] = employee.CompanyId.ToString(),
                ["full_name"] = employee.FullName
            };

            if (employee.DepartmentId.HasValue)
                additionalClaims["department_id"] = employee.DepartmentId.Value.ToString();

            if (!string.IsNullOrEmpty(employee.DepartmentName))
                additionalClaims["department_name"] = employee.DepartmentName;

            return await Task.FromResult(GenerateToken(employee.Id, employee.Email, employee.Role.ToString(), additionalClaims));
        }

        public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            return await Task.FromResult(ValidateToken(token));
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var principal = await ValidateTokenAsync(token);
            return principal != null && !IsTokenExpired(token);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }
    }
}