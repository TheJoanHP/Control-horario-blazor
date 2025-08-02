using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Sphere.Admin.Server.Data;
using Shared.Models.DTOs;
using Shared.Models.Core;

namespace Sphere.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SphereDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(SphereDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Buscar admin en la base de datos
                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Email == request.Email && a.Active);

                if (admin == null)
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Credenciales inválidas" 
                    });
                }

                // Verificar contraseña
                if (!BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
                {
                    return Unauthorized(new LoginResponse 
                    { 
                        Success = false, 
                        Message = "Credenciales inválidas" 
                    });
                }

                // Actualizar último login
                admin.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar JWT
                var token = GenerateJwtToken(admin);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Message = "Login exitoso",
                    User = new UserDto
                    {
                        Id = admin.Id,
                        Name = admin.Name,
                        Email = admin.Email,
                        Role = "SPHERE_ADMIN",
                        Active = admin.Active,
                        LastLogin = admin.LastLogin
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Error interno del servidor" 
                });
            }
        }

        [HttpGet("profile")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult<UserDto>> GetProfile()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Email == userEmail);

                if (admin == null)
                    return NotFound();

                return Ok(new UserDto
                {
                    Id = admin.Id,
                    Name = admin.Name,
                    Email = admin.Email,
                    Role = "SPHERE_ADMIN",
                    Active = admin.Active,
                    LastLogin = admin.LastLogin
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo perfil" });
            }
        }

        private string GenerateJwtToken(SphereAdmin admin)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                    new Claim(ClaimTypes.Email, admin.Email),
                    new Claim(ClaimTypes.Name, admin.Name),
                    new Claim(ClaimTypes.Role, "SPHERE_ADMIN")
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}