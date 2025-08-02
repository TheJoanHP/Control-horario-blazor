using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Employee.App.Server.Services;
using System.Security.Claims;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeDashboardController : ControllerBase
    {
        private readonly IEmployeeAppService _employeeAppService;
        private readonly ILogger<EmployeeDashboardController> _logger;

        public EmployeeDashboardController(
            IEmployeeAppService employeeAppService,
            ILogger<EmployeeDashboardController> logger)
        {
            _employeeAppService = employeeAppService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener dashboard completo del empleado
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> GetDashboard()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var dashboard = await _employeeAppService.GetEmployeeDashboardAsync(employeeId.Value);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener perfil del empleado
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfile()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var profile = await _employeeAppService.GetEmployeeProfileAsync(employeeId.Value);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualizar perfil del empleado
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var success = await _employeeAppService.UpdateEmployeeProfileAsync(
                    employeeId.Value,
                    dto.FirstName,
                    dto.LastName,
                    dto.Phone);

                if (!success)
                {
                    return NotFound("Empleado no encontrado");
                }

                return Ok(new { message = "Perfil actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // Método auxiliar para obtener el ID del empleado actual
        private int? GetCurrentEmployeeId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }
            return userId;
        }
    }

    // DTO para actualizar perfil
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}