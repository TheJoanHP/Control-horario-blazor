using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.Core;
using Shared.Models.DTOs.Employee;
using Shared.Services.Database;
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
        private readonly ITenantResolver _tenantResolver;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IEmployeeService employeeService,
            IMapper mapper,
            ITenantResolver tenantResolver,
            ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _mapper = mapper;
            _tenantResolver = tenantResolver;
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
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleados");
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

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear nuevo empleado
        /// </summary>
        [HttpPost]
        public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createDto, int companyId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.CreateEmployeeAsync(createDto);

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, _mapper.Map<EmployeeDto>(employee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualizar empleado existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.UpdateEmployeeAsync(id, updateDto);
                if (employee == null)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando empleado {EmployeeId}", id);
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
                var result = await _employeeService.DeleteEmployeeAsync(id);
                if (!result)
                {
                    return NotFound($"Empleado con ID {id} no encontrado");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando empleado {EmployeeId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}