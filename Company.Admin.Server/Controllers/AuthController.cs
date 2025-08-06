using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Core;
using Shared.Models.DTOs.Auth;
using Shared.Models.Enums;
using Shared.Services.Security;
using Company.Admin.Server.Data;

namespace Company.Admin.Server.Controllers
{
    /// <summary>
    /// Controlador de autenticación para Company Admin
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            CompanyDbContext context,
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
        /// Login para administradores de empresa y empleados
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

                // Buscar usuario por email
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Department)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Active);

                if (user == null)
                {
                    _logger.LogWarning("Intento de login fallido para email: {Email}", request.Email);
                    return Unauthorized("Credenciales inválidas");
                }

                // Verificar contraseña
                if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Contraseña incorrecta para usuario: {Email}", request.Email);
                    return Unauthorized("Credenciales inválidas");
                }

                // Actualizar último login
                user.LastLogin = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generar token JWT
                var additionalClaims = new Dictionary<string, string>
                {
                    ["first_name"] = user.FirstName,
                    ["last_name"] = user.LastName,
                    ["company_id"] = user.CompanyId?.ToString() ?? ""
                };

                if (user.Employee != null)
                {
                    additionalClaims["employee_id"] = user.Employee.Id.ToString();
                    additionalClaims["department_id"] = user.Employee.DepartmentId?.ToString() ?? "";
                }

                var token = _jwtService.CreateTokenInfo(
                    user.Id, 
                    user.Email, 
                    user.Role.ToString(),
                    user.CompanyId?.ToString(),
                    additionalClaims
                );

                var userInfo = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Active = user.Active,
                    DepartmentName = user.Employee?.Department?.Name,
                    LastLogin = user.LastLogin
                };

                // Información del empleado si aplica
                if (user.Employee != null)
                {
                    userInfo.Employee = new EmployeeInfo
                    {
                        Id = user.Employee.Id,
                        EmployeeNumber = user.Employee.EmployeeNumber,
                        Position = user.Employee.Position,
                        DepartmentId = user.Employee.DepartmentId,
                        DepartmentName = user.Employee.Department?.Name,
                        HireDate = user.Employee.HireDate,
                        Active = user.Employee.Active
                    };
                }

                // Información de la empresa
                if (user.Company != null)
                {
                    userInfo.Company = new CompanyInfo
                    {
                        Id = user.Company.Id,
                        Name = user.Company.Name,
                        Subdomain = user.Company.Subdomain,
                        Email = user.Company.Email,
                        Phone = user.Company.Phone,
                        Address = user.Company.Address,
                        Active = user.Company.Active
                    };
                }

                var response = new LoginResponse
                {
                    Success = true,
                    Token = token.Token,
                    RefreshToken = token.RefreshToken,
                    ExpiresAt = token.ExpiresAt,
                    User = userInfo,
                    Message = "Login exitoso"
                };

                _logger.LogInformation("Login exitoso para usuario: {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para email: {Email}", request.Email);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Renovar token de acceso
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validar refresh token
                var principal = _jwtService.ValidateRefreshToken(request.RefreshToken);
                if (principal == null)
                {
                    return Unauthorized("Token de actualización inválido");
                }

                var userIdClaim = principal.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Token inválido");
                }

                // Buscar usuario
                var user = await _context.Users
                    .Include(u => u.Employee)
                        .ThenInclude(e => e!.Department)
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.Active);

                if (user == null)
                {
                    return Unauthorized("Usuario no encontrado o inactivo");
                }

                // Generar nuevo token
                var additionalClaims = new Dictionary<string, string>
                {
                    ["first_name"] = user.FirstName,
                    ["last_name"] = user.LastName,
                    ["company_id"] = user.CompanyId?.ToString() ?? ""
                };

                if (user.Employee != null)
                {
                    additionalClaims["employee_id"] = user.Employee.Id.ToString();
                    additionalClaims["department_id"] = user.Employee.DepartmentId?.ToString() ?? "";
                }

                var newToken = _jwtService.CreateTokenInfo(
                    user.Id, 
                    user.Email, 
                    user.Role.ToString(),
                    user.CompanyId?.ToString(),
                    additionalClaims
                );

                var userInfo = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    Active = user.Active,
                    DepartmentName = user.Employee?.Department?.Name,
                    LastLogin = user.LastLogin
                };

                // Información del empleado si aplica
                if (user.Employee != null)
                {
                    userInfo.Employee = new EmployeeInfo
                    {
                        Id = user.Employee.Id,
                        EmployeeNumber = user.Employee.EmployeeNumber,
                        Position = user.Employee.Position,
                        DepartmentId = user.Employee.DepartmentId,
                        DepartmentName = user.Employee.Department?.Name,
                        HireDate = user.Employee.HireDate,
                        Active = user.Employee.Active
                    };
                }

                // Información de la empresa
                if (user.Company != null)
                {
                    userInfo.Company = new CompanyInfo
                    {
                        Id = user.Company.Id,
                        Name = user.Company.Name,
                        Subdomain = user.Company.Subdomain,
                        Email = user.Company.Email,
                        Phone = user.Company.Phone,
                        Address = user.Company.Address,
                        Active = user.Company.Active
                    };
                }

                var response = new LoginResponse
                {
                    Success = true,
                    Token = newToken.Token,
                    RefreshToken = newToken.RefreshToken,
                    ExpiresAt = newToken.ExpiresAt,
                    User = userInfo,
                    Message = "Token renovado exitosamente"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renovar token");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Logout - Invalidar token
        /// </summary>
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // En una implementación completa, aquí invalidarías el token en una blacklist
            // Por ahora, simplemente retornamos OK
            await Task.CompletedTask;
            return Ok(new { message = "Sesión cerrada exitosamente" });
        }

        /// <summary>
        /// Cambiar contraseña
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst("userId")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Token inválido");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Verificar contraseña actual
                if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest("Contraseña actual incorrecta");
                }

                // Actualizar contraseña
                user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Contraseña cambiada para usuario: {UserId}", userId);
                return Ok(new { message = "Contraseña actualizada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Recuperar contraseña
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Active);
                
                // Por seguridad, siempre retornamos OK aunque el usuario no exista
                if (user == null)
                {
                    _logger.LogWarning("Solicitud de recuperación de contraseña para email inexistente: {Email}", request.Email);
                    return Ok(new { message = "Si el email existe, se enviará un enlace de recuperación" });
                }

                // Aquí implementarías el envío del email de recuperación
                // Por ahora, solo logueamos la acción
                _logger.LogInformation("Solicitud de recuperación de contraseña para usuario: {Email}", request.Email);

                return Ok(new { message = "Si el email existe, se enviará un enlace de recuperación" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en recuperación de contraseña");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}