using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.Employee;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(
            IEmployeeService employeeService,
            ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los empleados
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees(
            [FromQuery] int? departmentId = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                var employees = await _employeeService.GetAllAsync(departmentId, active);
                return Ok(employees);
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
                var employee = await _employeeService.GetByIdAsync(id);
                if (employee == null)
                    return NotFound();

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener empleado por número de empleado
        /// </summary>
        [HttpGet("by-number/{employeeNumber}")]
        public async Task<ActionResult<EmployeeDto>> GetEmployeeByNumber(string employeeNumber)
        {
            try
            {
                var employee = await _employeeService.GetByEmployeeNumberAsync(employeeNumber);
                if (employee == null)
                    return NotFound();

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleado por número {EmployeeNumber}", employeeNumber);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear nuevo empleado
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
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
        public async Task<ActionResult<EmployeeDto>> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _employeeService.UpdateAsync(id, updateDto);
                return Ok(employee);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Eliminar (desactivar) empleado
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                var result = await _employeeService.DeleteAsync(id);
                if (!result)
                    return NotFound();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar empleado {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener empleados por departamento
        /// </summary>
        [HttpGet("by-department/{departmentId}")]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployeesByDepartment(int departmentId)
        {
            try
            {
                var employees = await _employeeService.GetByDepartmentAsync(departmentId);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados del departamento {DepartmentId}", departmentId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si existe un empleado
        /// </summary>
        [HttpHead("{id}")]
        public async Task<IActionResult> EmployeeExists(int id)
        {
            try
            {
                var exists = await _employeeService.ExistsAsync(id);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del empleado {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si existe un número de empleado
        /// </summary>
        [HttpHead("by-number/{employeeNumber}")]
        public async Task<IActionResult> EmployeeNumberExists(string employeeNumber)
        {
            try
            {
                var exists = await _employeeService.ExistsByEmployeeNumberAsync(employeeNumber);
                return exists ? Ok() : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia del número de empleado {EmployeeNumber}", employeeNumber);
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }
}