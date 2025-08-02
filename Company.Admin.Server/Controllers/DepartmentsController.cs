using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.Core;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            IDepartmentService departmentService,
            ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los departamentos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Department>>> GetDepartments([FromQuery] bool? active = null)
        {
            try
            {
                var departments = await _departmentService.GetDepartmentsAsync(active);
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener departamentos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener departamentos activos
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<Department>>> GetActiveDepartments()
        {
            try
            {
                var departments = await _departmentService.GetActiveDepartmentsAsync();
                return Ok(departments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener departamentos activos");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener departamento por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            try
            {
                var department = await _departmentService.GetDepartmentByIdAsync(id);
                
                if (department == null)
                {
                    return NotFound($"Departamento con ID {id} no encontrado");
                }

                return Ok(department);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear nuevo departamento
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Department>> CreateDepartment([FromBody] CreateDepartmentDto createDepartmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var department = await _departmentService.CreateDepartmentAsync(
                    createDepartmentDto.Name,
                    createDepartmentDto.Description
                );

                return CreatedAtAction(
                    nameof(GetDepartment),
                    new { id = department.Id },
                    department
                );
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear departamento");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualizar departamento
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Department>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto updateDepartmentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var department = await _departmentService.UpdateDepartmentAsync(
                    id,
                    updateDepartmentDto.Name,
                    updateDepartmentDto.Description
                );

                return Ok(department);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Eliminar departamento (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteDepartment(int id)
        {
            try
            {
                var success = await _departmentService.DeleteDepartmentAsync(id);
                
                if (!success)
                {
                    return NotFound($"Departamento con ID {id} no encontrado");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Activar departamento
        /// </summary>
        [HttpPatch("{id}/activate")]
        public async Task<ActionResult> ActivateDepartment(int id)
        {
            try
            {
                var success = await _departmentService.ActivateDepartmentAsync(id);
                
                if (!success)
                {
                    return NotFound($"Departamento con ID {id} no encontrado");
                }

                return Ok(new { message = "Departamento activado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Desactivar departamento
        /// </summary>
        [HttpPatch("{id}/deactivate")]
        public async Task<ActionResult> DeactivateDepartment(int id)
        {
            try
            {
                var success = await _departmentService.DeactivateDepartmentAsync(id);
                
                if (!success)
                {
                    return NotFound($"Departamento con ID {id} no encontrado");
                }

                return Ok(new { message = "Departamento desactivado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener estadísticas del departamento
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<ActionResult> GetDepartmentStats(int id)
        {
            try
            {
                var department = await _departmentService.GetDepartmentByIdAsync(id);
                if (department == null)
                {
                    return NotFound($"Departamento con ID {id} no encontrado");
                }

                var employeeCount = await _departmentService.GetEmployeeCountAsync(id);
                var canDelete = await _departmentService.CanDeleteDepartmentAsync(id);

                return Ok(new
                {
                    departmentId = id,
                    departmentName = department.Name,
                    employeeCount,
                    canDelete,
                    active = department.Active,
                    createdAt = department.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del departamento {DepartmentId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si un nombre de departamento está disponible
        /// </summary>
        [HttpGet("check-name")]
        public async Task<ActionResult> CheckDepartmentName([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return BadRequest("El nombre es requerido");
                }

                var isUnique = await _departmentService.IsDepartmentNameUniqueAsync(name, excludeId);
                
                return Ok(new
                {
                    available = isUnique,
                    name = name.Trim()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de departamento");
                return StatusCode(500, "Error interno del servidor");
            }
        }
    }

    // DTOs para el controlador
    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}