using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.DTOs.Auth;
using Shared.Models.Enums;
using Shared.Services.Security;
using Sphere.Admin.Server.Data;

namespace Sphere.Admin.Server.Controllers
{
    /// <summary>
    /// Controlador de autenticación para Sphere Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SphereDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SphereDbContext context,
            IPasswordService passwordService,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtService = jwtService;
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
                    return BadRequest(ModelState);
                }

                // Buscar admin por email
                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Email == request.Email && a.Active);

                if (admin == null)
                {
                    _logger.LogWarning("Intento de login fallido para email: {Email}", request.Email);
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                // Verificar contraseña
                if (!_passwordService.VerifyPassword(request.Password, admin.PasswordHash))
                {
                    _logger.LogWarning("Contraseña incorrecta para admin: {Email}", request.Email);
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                // Actualizar último login
                admin.LastLogin = DateTime.UtcNow;
                admin.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar token JWT
                var token = _jwtService.GenerateToken(
                    admin.Id, 
                    admin.Email, 
                    UserRole.SuperAdmin,
                    new Dictionary<string, string>
                    {
                        { "FirstName", admin.FirstName },
                        { "LastName", admin.LastName }
                    });

                // Crear respuesta
                var userInfo = new UserInfo
                {
                    Id = admin.Id,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    Email = admin.Email,
                    Role = UserRole.SuperAdmin,
                    LastLogin = admin.LastLogin
                };

                var response = new LoginResponse
                {
                    Success = true,
                    Token = token,
                    User = userInfo,
                    Message = "Login exitoso"
                };

                _logger.LogInformation("Login exitoso para super admin: {Email}", admin.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Verificar validez del token
        /// </summary>
        [HttpPost("verify")]
        public async Task<ActionResult<UserInfo>> VerifyToken([FromBody] TokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { message = "Token requerido" });
                }

                // Validar token
                var claims = _jwtService.ValidateToken(request.Token);
                if (claims == null)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                // Obtener ID del usuario del token
                var userId = _jwtService.GetUserIdFromToken(request.Token);
                if (userId == null)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                // Buscar admin en la base de datos
                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Id == userId && a.Active);

                if (admin == null)
                {
                    return Unauthorized(new { message = "Usuario no encontrado o inactivo" });
                }

                // Crear respuesta con información del usuario
                var userInfo = new UserInfo
                {
                    Id = admin.Id,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    Email = admin.Email,
                    Role = UserRole.SuperAdmin,
                    LastLogin = admin.LastLogin
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando token");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Logout (invalidar token del lado del cliente)
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {
            try
            {
                // En este caso simple, el logout es manejado del lado del cliente
                // eliminando el token del almacenamiento local.
                // En implementaciones más avanzadas, se podría mantener una lista
                // de tokens revocados en Redis o base de datos.

                _logger.LogInformation("Logout ejecutado");
                return Ok(new { message = "Logout exitoso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante logout");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Cambiar contraseña del admin actual
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Obtener el token del header Authorization
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Token requerido" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userId = _jwtService.GetUserIdFromToken(token);

                if (userId == null)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                // Buscar admin
                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Id == userId && a.Active);

                if (admin == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Verificar contraseña actual
                if (!_passwordService.VerifyPassword(request.CurrentPassword, admin.PasswordHash))
                {
                    return BadRequest(new { message = "Contraseña actual incorrecta" });
                }

                // Actualizar contraseña
                admin.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                admin.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contraseña cambiada para admin: {Email}", admin.Email);
                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener información del usuario actual
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                // Obtener el token del header Authorization
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader == null || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { message = "Token requerido" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var userId = _jwtService.GetUserIdFromToken(token);

                if (userId == null)
                {
                    return Unauthorized(new { message = "Token inválido" });
                }

                // Buscar admin
                var admin = await _context.SphereAdmins
                    .FirstOrDefaultAsync(a => a.Id == userId && a.Active);

                if (admin == null)
                {
                    return NotFound(new { message = "Usuario no encontrado" });
                }

                // Crear respuesta
                var userInfo = new UserInfo
                {
                    Id = admin.Id,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    Email = admin.Email,
                    Role = UserRole.SuperAdmin,
                    LastLogin = admin.LastLogin
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información del usuario");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}