using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using AutoMapper;
using System.ComponentModel.DataAnnotations;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IMapper _mapper;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IEmployeeService employeeService,
            IMapper mapper,
            ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los empleados con filtros opcionales
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees(
            [FromQuery] string? search = null,
            [FromQuery] int? departmentId = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                var employees = await _employeeService.GetEmployeesAsync(search, departmentId, active);
                var employeeDtos = _mapper.Map<IEnumerable<EmployeeDto>>(employees);
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener empleado por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(int id)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                
                if (employee == null)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                var employeeDto = _mapper.Map<EmployeeDto>(employee);
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear nuevo empleado
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeDto createEmployeeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.CreateEmployeeAsync(createEmployeeDto);
                var employeeDto = _mapper.Map<EmployeeDto>(employee);

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employeeDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualizar empleado
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.UpdateEmployeeAsync(id, updateEmployeeDto);
                var employeeDto = _mapper.Map<EmployeeDto>(employee);

                return Ok(employeeDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Eliminar empleado
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(int id)
        {
            try
            {
                var deleted = await _employeeService.DeleteEmployeeAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return Ok(new { message = "Empleado eliminado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Activar empleado
        /// </summary>
        [HttpPost("{id}/activate")]
        public async Task<ActionResult> ActivateEmployee(int id)
        {
            try
            {
                var activated = await _employeeService.ActivateEmployeeAsync(id);
                
                if (!activated)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return Ok(new { message = "Empleado activado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Desactivar empleado
        /// </summary>
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult> DeactivateEmployee(int id)
        {
            try
            {
                var deactivated = await _employeeService.DeactivateEmployeeAsync(id);
                
                if (!deactivated)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return Ok(new { message = "Empleado desactivado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Cambiar contraseña de empleado
        /// </summary>
        [HttpPost("{id}/change-password")]
        public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangeEmployeePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var changed = await _employeeService.ChangePasswordAsync(id, changePasswordDto.NewPassword);
                
                if (!changed)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return Ok(new { message = "Contraseña cambiada correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si un email está disponible
        /// </summary>
        [HttpGet("check-email")]
        public async Task<ActionResult> CheckEmailAvailability([FromQuery] string email, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("El email es requerido");
                }

                var isUnique = await _employeeService.IsEmailUniqueAsync(email, excludeId);
                
                return Ok(new
                {
                    available = isUnique,
                    email = email.Trim().ToLower()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar disponibilidad de email");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si un código de empleado está disponible
        /// </summary>
        [HttpGet("check-employee-code")]
        public async Task<ActionResult> CheckEmployeeCodeAvailability([FromQuery] string employeeCode, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest("El código de empleado es requerido");
                }

                var isUnique = await _employeeService.IsEmployeeCodeUniqueAsync(employeeCode, excludeId);
                
                return Ok(new
                {
                    available = isUnique,
                    employeeCode = employeeCode.Trim().ToUpper()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar disponibilidad de código de empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Generar código de empleado único
        /// </summary>
        [HttpGet("generate-employee-code")]
        public async Task<ActionResult> GenerateEmployeeCode()
        {
            try
            {
                var employeeCode = await _employeeService.GenerateUniqueEmployeeCodeAsync();
                
                return Ok(new
                {
                    employeeCode,
                    generated = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar código de empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener estadísticas de empleados
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetEmployeeStats()
        {
            try
            {
                var totalEmployees = await _employeeService.GetTotalEmployeesAsync();
                var activeEmployees = await _employeeService.GetActiveEmployeesAsync();
                var inactiveEmployees = totalEmployees - activeEmployees;

                return Ok(new
                {
                    totalEmployees,
                    activeEmployees,
                    inactiveEmployees,
                    activePercentage = totalEmployees > 0 ? Math.Round((double)activeEmployees / totalEmployees * 100, 1) : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de empleados");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    /// <summary>
    /// DTO para cambiar contraseña de empleado
    /// </summary>
    public class ChangeEmployeePasswordDto
    {
        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}