using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.Auth;
using Shared.Services.Security;
using Shared.Services.Database;
using Employee.App.Server.DTOs;
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
                    ["department_id"] = employee.DepartmentId?.ToString() ?? ""
                };

                var tokenInfo = _jwtService.CreateTokenInfo(
                    employee.Id, 
                    employee.Email, 
                    employee.Role.ToString(),
                    tenantId,
                    additionalClaims
                );

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = tokenInfo.Token,
                    ExpiresAt = tokenInfo.ExpiresAt,
                    RefreshToken = tokenInfo.RefreshToken,
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
                _logger.LogError(ex, "Error durante el login del empleado {Email}", request.Email);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Login con PIN para empleados
        /// </summary>
        [HttpPost("pin-login")]
        public async Task<ActionResult<LoginResponse>> PinLogin([FromBody] PinLoginRequest request)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByCodeAsync(request.Pin);
                
                if (employee == null || !employee.Active)
                {
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

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = tokenInfo.Token,
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
        public async Task<ActionResult> ChangePassword([FromBody] EmployeeChangePasswordRequest request)
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
        public async Task<ActionResult> Logout() // CORREGIDO: removido async sin await
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Empleado {EmployeeId} ha cerrado sesión", userIdClaim);

                // Si necesitas operaciones async aquí, descomenta la siguiente línea:
                // await Task.CompletedTask; 

                return Ok(new { message = "Logout exitoso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el logout del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    // DTOs para las peticiones - movidos a archivo separado para evitar duplicados
}