using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sphere.Admin.Server.Data;
using Shared.Models.DTOs.Auth;
using Shared.Services.Security;
using Shared.Models.Core;  // Para SphereAdmin
using System.Security.Claims;

namespace Sphere.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SphereDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SphereDbContext context,
            IJwtService jwtService,
            IPasswordService passwordService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _logger = logger;
        }

        /// <summary>
        /// Login para super administradores de Sphere
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada no válidos"
                    });
                }

                // Buscar el super administrador usando el namespace completo para evitar ambigüedad
                var sphereAdmin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(sa => sa.Email == request.Email && sa.Active);

                if (sphereAdmin == null)
                {
                    _logger.LogWarning("Intento de login fallido para super admin {Email}", request.Email);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Credenciales inválidas"
                    });
                }

                // Verificar la contraseña
                if (!_passwordService.VerifyPassword(request.Password, sphereAdmin.PasswordHash))
                {
                    _logger.LogWarning("Contraseña incorrecta para super admin {Email}", request.Email);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Credenciales inválidas"
                    });
                }

                // Actualizar último login
                sphereAdmin.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar JWT token
                var claims = new Dictionary<string, string>
                {
                    ["user_id"] = sphereAdmin.Id.ToString(),
                    ["email"] = sphereAdmin.Email,
                    ["role"] = "SuperAdmin",
                    ["first_name"] = sphereAdmin.FirstName,
                    ["last_name"] = sphereAdmin.LastName,
                    ["sphere_admin"] = "true"
                };

                var token = _jwtService.GenerateToken(sphereAdmin.Email, claims);

                _logger.LogInformation("Login exitoso para super admin {Email}", request.Email);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = token,
                    User = new UserInfo
                    {
                        Id = sphereAdmin.Id,
                        Email = sphereAdmin.Email,
                        FirstName = sphereAdmin.FirstName,
                        LastName = sphereAdmin.LastName,
                        Role = "SuperAdmin"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtener información del usuario autenticado
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Token inválido");
                }

                var sphereAdmin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(sa => sa.Id == userId && sa.Active);

                if (sphereAdmin == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                return Ok(new UserInfo
                {
                    Id = sphereAdmin.Id,
                    Email = sphereAdmin.Email,
                    FirstName = sphereAdmin.FirstName,
                    LastName = sphereAdmin.LastName,
                    Role = "SuperAdmin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información del usuario");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Cambiar contraseña del super administrador
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Datos de entrada no válidos");
                }

                var userIdClaim = User.FindFirst("user_id");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Token inválido");
                }

                var sphereAdmin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(sa => sa.Id == userId && sa.Active);

                if (sphereAdmin == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Verificar contraseña actual
                if (!_passwordService.VerifyPassword(request.CurrentPassword, sphereAdmin.PasswordHash))
                {
                    return BadRequest("Contraseña actual incorrecta");
                }

                // Actualizar contraseña
                sphereAdmin.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                sphereAdmin.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contraseña cambiada para super admin {Email}", sphereAdmin.Email);

                return Ok(new { Message = "Contraseña actualizada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Logout (invalidar token del lado del cliente)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // En una implementación más completa, aquí se podría agregar el token a una blacklist
            // Por ahora, simplemente informamos que el logout debe manejarse del lado del cliente
            
            _logger.LogInformation("Logout para usuario {UserId}", User.FindFirst("user_id")?.Value);
            
            return Ok(new { Message = "Logout exitoso" });
        }
    }

    // DTOs específicos para cambio de contraseña
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}