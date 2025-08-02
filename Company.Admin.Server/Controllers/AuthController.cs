using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.Auth;
using Shared.Services.Security;
using Shared.Services.Database;
using Shared.Models.Enums;
using System.Security.Claims;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IJwtService _jwtService;
        private readonly ITenantResolver _tenantResolver;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IEmployeeService employeeService,
            IJwtService jwtService,
            ITenantResolver tenantResolver,
            ILogger<AuthController> logger)
        {
            _employeeService = employeeService;
            _jwtService = jwtService;
            _tenantResolver = tenantResolver;
            _logger = logger;
        }

        /// <summary>
        /// Login para administradores y supervisores de empresa
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

                var employee = await _employeeService.AuthenticateEmployeeAsync(request.Email, request.Password);
                
                if (employee == null)
                {
                    _logger.LogWarning("Intento de login fallido para {Email}", request.Email);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Credenciales inválidas"
                    });
                }

                // Verificar que el usuario tenga permisos de administración
                if (employee.Role != UserRole.CompanyAdmin && employee.Role != UserRole.Supervisor)
                {
                    _logger.LogWarning("Usuario {Email} intentó acceso sin permisos de administración", request.Email);
                    return Forbidden(new LoginResponse
                    {
                        Success = false,
                        Message = "No tienes permisos para acceder al panel de administración"
                    });
                }

                var tenantId = _tenantResolver.GetTenantId();
                var additionalClaims = new Dictionary<string, string>
                {
                    ["employee_id"] = employee.Id.ToString(),
                    ["company_id"] = employee.CompanyId.ToString(),
                    ["department_id"] = employee.DepartmentId?.ToString() ?? "",
                    ["full_name"] = employee.FullName
                };

                var tokenInfo = _jwtService.CreateTokenInfo(
                    employee.Id,
                    employee.Email,
                    employee.Role.ToString(),
                    tenantId,
                    additionalClaims
                );

                _logger.LogInformation("Login exitoso para {Email} con rol {Role}", employee.Email, employee.Role);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = tokenInfo.Token,
                    RefreshToken = tokenInfo.RefreshToken,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    User = new UserInfo
                    {
                        Id = employee.Id,
                        FullName = employee.FullName,
                        Email = employee.Email,
                        Role = employee.Role,
                        DepartmentName = employee.Department?.Name,
                        CompanyName = employee.Company?.Name
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
        /// Renovar token de acceso
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // Aquí deberías implementar la lógica de validación del refresh token
                // Por simplicidad, este ejemplo asume que el refresh token es válido
                
                // En una implementación real, deberías:
                // 1. Validar el refresh token en base de datos
                // 2. Verificar que no haya expirado
                // 3. Obtener los datos del usuario asociado

                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = "Refresh token no implementado aún"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al renovar token");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtener información del usuario actual
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentUser()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                var employee = await _employeeService.GetEmployeeByIdAsync(userId);
                if (employee == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                return Ok(new UserInfo
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    Role = employee.Role,
                    DepartmentName = employee.Department?.Name,
                    CompanyName = employee.Company?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener información del usuario actual");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Logout (invalidar token)
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                // En una implementación real, aquí deberías:
                // 1. Invalidar el token en una blacklist
                // 2. Eliminar el refresh token de la base de datos
                // 3. Limpiar cualquier sesión del lado del servidor

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Usuario {UserId} ha cerrado sesión", userIdClaim);

                return Ok(new { message = "Logout exitoso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Cambiar contraseña del usuario actual
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

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized();
                }

                // Verificar contraseña actual
                var employee = await _employeeService.GetEmployeeByIdAsync(userId);
                if (employee == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                if (!await _employeeService.ValidateEmployeeCredentialsAsync(employee.Email, request.CurrentPassword))
                {
                    return BadRequest("La contraseña actual no es correcta");
                }

                // Cambiar contraseña
                var success = await _employeeService.ChangePasswordAsync(userId, request.NewPassword);
                if (!success)
                {
                    return BadRequest("No se pudo cambiar la contraseña");
                }

                _logger.LogInformation("Contraseña cambiada para usuario {UserId}", userId);
                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si el token es válido
        /// </summary>
        [HttpPost("verify-token")]
        public ActionResult VerifyToken([FromBody] VerifyTokenRequest request)
        {
            try
            {
                var principal = _jwtService.ValidateToken(request.Token);
                
                return Ok(new
                {
                    valid = principal != null,
                    expired = _jwtService.IsTokenExpired(request.Token)
                });
            }
            catch
            {
                return Ok(new { valid = false, expired = true });
            }
        }
    }

    // DTOs adicionales para el controlador
    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class VerifyTokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}