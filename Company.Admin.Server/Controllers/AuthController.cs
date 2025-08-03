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

                // Verificar que tenga permisos de administrador o supervisor
                if (employee.Role != UserRole.CompanyAdmin && employee.Role != UserRole.Supervisor)
                {
                    _logger.LogWarning("Acceso denegado para {Email} - Rol: {Role}", request.Email, employee.Role);
                    return StatusCode(403, new LoginResponse
                    {
                        Success = false,
                        Message = "No tiene permisos para acceder al panel de administración"
                    });
                }

                var tenantId = _tenantResolver.GetTenantId();
                var additionalClaims = new Dictionary<string, string>
                {
                    ["employee_id"] = employee.Id.ToString(),
                    ["company_id"] = employee.CompanyId.ToString(),
                    ["department_id"] = employee.DepartmentId?.ToString() ?? "",
                    ["tenant_id"] = tenantId
                };

                var token = _jwtService.GenerateToken(employee.Id, employee.Email, employee.Role.ToString(), additionalClaims);
                var expiresAt = DateTime.UtcNow.AddHours(8); // Ajustar según configuración

                var response = new LoginResponse
                {
                    Success = true,
                    Token = token,
                    ExpiresAt = expiresAt,
                    Message = "Login exitoso",
                    User = new UserInfo
                    {
                        Id = employee.Id,
                        Name = employee.FirstName,
                        FullName = employee.FullName,
                        Email = employee.Email,
                        Role = employee.Role,
                        Active = employee.Active,
                        LastLogin = employee.LastLoginAt,
                        DepartmentName = employee.Department?.Name,
                        CompanyName = employee.Company?.Name,
                        Employee = new EmployeeInfo
                        {
                            Id = employee.Id,
                            EmployeeCode = employee.EmployeeCode,
                            DepartmentName = employee.Department?.Name,
                            HiredAt = employee.HiredAt,
                            Company = new CompanyInfo
                            {
                                Id = employee.CompanyId,
                                Name = employee.Company?.Name ?? ""
                            }
                        }
                    }
                };

                _logger.LogInformation("Login exitoso para {Email}", request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login");
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
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Token inválido");
                }

                var employee = await _employeeService.GetEmployeeByIdAsync(userId);
                if (employee == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                var userInfo = new UserInfo
                {
                    Id = employee.Id,
                    Name = employee.FirstName,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    Role = employee.Role,
                    Active = employee.Active,
                    LastLogin = employee.LastLoginAt,
                    DepartmentName = employee.Department?.Name,
                    CompanyName = employee.Company?.Name,
                    Employee = new EmployeeInfo
                    {
                        Id = employee.Id,
                        EmployeeCode = employee.EmployeeCode,
                        DepartmentName = employee.Department?.Name,
                        HiredAt = employee.HiredAt,
                        Company = new CompanyInfo
                        {
                            Id = employee.CompanyId,
                            Name = employee.Company?.Name ?? ""
                        }
                    }
                };

                return Ok(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario actual");
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
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Token inválido");
                }

                var employee = await _employeeService.GetEmployeeByIdAsync(userId);
                if (employee == null)
                {
                    return NotFound("Usuario no encontrado");
                }

                // Verificar contraseña actual
                if (!await _employeeService.ValidateEmployeeCredentialsAsync(employee.Email, request.CurrentPassword))
                {
                    return BadRequest("La contraseña actual es incorrecta");
                }

                // Cambiar contraseña
                await _employeeService.ChangePasswordAsync(userId, request.NewPassword);

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