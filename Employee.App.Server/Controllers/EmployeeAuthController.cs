using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.Auth;
using Shared.Services.Security;
using Shared.Services.Database;
using System.Security.Claims;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeAuthController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IJwtService _jwtService;
        private readonly ITenantResolver _tenantResolver;
        private readonly ILogger<EmployeeAuthController> _logger;

        public EmployeeAuthController(
            IEmployeeService employeeService,
            IJwtService jwtService,
            ITenantResolver tenantResolver,
            ILogger<EmployeeAuthController> logger)
        {
            _employeeService = employeeService;
            _jwtService = jwtService;
            _tenantResolver = tenantResolver;
            _logger = logger;
        }

        /// <summary>
        /// Login para empleados (todos los roles)
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
                    _logger.LogWarning("Intento de login fallido para empleado {Email}", request.Email);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "Credenciales inválidas"
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

                _logger.LogInformation("Login de empleado exitoso para {Email} con rol {Role}", employee.Email, employee.Role);

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
                _logger.LogError(ex, "Error durante el login de empleado para {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Login con PIN (para kioscos/dispositivos compartidos)
        /// </summary>
        [HttpPost("pin-login")]
        public async Task<ActionResult<LoginResponse>> PinLogin([FromBody] PinLoginRequest request)
        {
            try
            {
                // Por ahora, simulamos que el PIN es los últimos 4 dígitos del ID del empleado
                // En una implementación real, tendrías una tabla de PINs
                
                var employee = await _employeeService.GetEmployeeByIdAsync(request.EmployeeId);
                
                if (employee == null || !employee.Active)
                {
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "PIN inválido"
                    });
                }

                // Validación simple del PIN (implementar lógica real)
                var expectedPin = employee.Id.ToString().PadLeft(4, '0').Substring(Math.Max(0, employee.Id.ToString().Length - 4));
                
                if (request.Pin != expectedPin)
                {
                    _logger.LogWarning("Intento de login con PIN fallido para empleado {EmployeeId}", request.EmployeeId);
                    return Unauthorized(new LoginResponse
                    {
                        Success = false,
                        Message = "PIN inválido"
                    });
                }

                var tenantId = _tenantResolver.GetTenantId();
                var additionalClaims = new Dictionary<string, string>
                {
                    ["employee_id"] = employee.Id.ToString(),
                    ["company_id"] = employee.CompanyId.ToString(),
                    ["login_type"] = "pin"
                };

                var tokenInfo = _jwtService.CreateTokenInfo(
                    employee.Id,
                    employee.Email,
                    employee.Role.ToString(),
                    tenantId,
                    additionalClaims
                );

                _logger.LogInformation("Login con PIN exitoso para empleado {EmployeeId}", employee.Id);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login con PIN exitoso",
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
                _logger.LogError(ex, "Error durante el login con PIN");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtener información del empleado actual
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfo>> GetCurrentEmployee()
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
                    return NotFound("Empleado no encontrado");
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
                _logger.LogError(ex, "Error al obtener información del empleado actual");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Cambiar contraseña del empleado
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
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
                    return NotFound("Empleado no encontrado");
                }

                // Verificar contraseña actual
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

                _logger.LogInformation("Contraseña cambiada para empleado {EmployeeId}", userId);
                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Logout del empleado
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Empleado {EmployeeId} ha cerrado sesión", userIdClaim);

                return Ok(new { message = "Logout exitoso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    // DTOs específicos para empleados
    public class PinLoginRequest
    {
        public int EmployeeId { get; set; }
        public string Pin { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}