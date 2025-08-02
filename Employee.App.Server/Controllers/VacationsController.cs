using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Employee.App.Server.Services;
using System.Security.Claims;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VacationsController : ControllerBase
    {
        private readonly IEmployeeAppService _employeeAppService;
        private readonly ILogger<VacationsController> _logger;

        public VacationsController(
            IEmployeeAppService employeeAppService,
            ILogger<VacationsController> logger)
        {
            _employeeAppService = employeeAppService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todas las solicitudes de vacaciones del empleado
        /// </summary>
        [HttpGet("requests")]
        public async Task<ActionResult> GetVacationRequests()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var requests = await _employeeAppService.GetEmployeeVacationRequestsAsync(employeeId.Value);
                
                var requestData = requests.Select(vr => new
                {
                    Id = vr.Id,
                    StartDate = vr.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = vr.EndDate.ToString("yyyy-MM-dd"),
                    DaysRequested = vr.DaysRequested,
                    Status = vr.Status.ToString(),
                    Comments = vr.Comments,
                    ResponseComments = vr.ResponseComments,
                    ReviewedBy = vr.ReviewedBy?.FullName,
                    ReviewedAt = vr.ReviewedAt,
                    CreatedAt = vr.CreatedAt,
                    CanCancel = vr.Status == Shared.Models.Enums.VacationStatus.Pending
                });

                return Ok(requestData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener solicitudes de vacaciones");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear nueva solicitud de vacaciones
        /// </summary>
        [HttpPost("requests")]
        public async Task<ActionResult> CreateVacationRequest([FromBody] CreateVacationRequestDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var request = await _employeeAppService.CreateVacationRequestAsync(
                    employeeId.Value, 
                    dto.StartDate, 
                    dto.EndDate, 
                    dto.Comments);

                return CreatedAtAction(
                    nameof(GetVacationRequest),
                    new { id = request.Id },
                    new
                    {
                        Id = request.Id,
                        StartDate = request.StartDate.ToString("yyyy-MM-dd"),
                        EndDate = request.EndDate.ToString("yyyy-MM-dd"),
                        DaysRequested = request.DaysRequested,
                        Status = request.Status.ToString(),
                        CreatedAt = request.CreatedAt
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear solicitud de vacaciones");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener solicitud específica de vacaciones
        /// </summary>
        [HttpGet("requests/{id}")]
        public async Task<ActionResult> GetVacationRequest(int id)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var requests = await _employeeAppService.GetEmployeeVacationRequestsAsync(employeeId.Value);
                var request = requests.FirstOrDefault(vr => vr.Id == id);

                if (request == null)
                {
                    return NotFound("Solicitud de vacaciones no encontrada");
                }

                return Ok(new
                {
                    Id = request.Id,
                    StartDate = request.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = request.EndDate.ToString("yyyy-MM-dd"),
                    DaysRequested = request.DaysRequested,
                    Status = request.Status.ToString(),
                    Comments = request.Comments,
                    ResponseComments = request.ResponseComments,
                    ReviewedBy = request.ReviewedBy?.FullName,
                    ReviewedAt = request.ReviewedAt,
                    CreatedAt = request.CreatedAt,
                    UpdatedAt = request.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener solicitud de vacaciones {RequestId}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener balance de vacaciones del empleado
        /// </summary>
        [HttpGet("balance")]
        public async Task<ActionResult> GetVacationBalance()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var balance = await _employeeAppService.GetVacationBalanceAsync(employeeId.Value);
                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener balance de vacaciones");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Verificar si se pueden solicitar vacaciones en determinadas fechas
        /// </summary>
        [HttpPost("check-availability")]
        public async Task<ActionResult> CheckVacationAvailability([FromBody] CheckAvailabilityDto dto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var canRequest = await _employeeAppService.CanRequestVacationAsync(
                    employeeId.Value, 
                    dto.StartDate, 
                    dto.EndDate);

                var daysRequested = CalculateWorkingDays(dto.StartDate, dto.EndDate);

                return Ok(new
                {
                    CanRequest = canRequest,
                    StartDate = dto.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = dto.EndDate.ToString("yyyy-MM-dd"),
                    DaysRequested = daysRequested,
                    Message = canRequest ? "Las fechas están disponibles" : "Las fechas no están disponibles"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar disponibilidad de vacaciones");
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

        // Método auxiliar para calcular días laborables
        private static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            var current = startDate.Date;

            while (current <= endDate.Date)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }
    }

    // DTOs para vacaciones
    public class CreateVacationRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Comments { get; set; }
    }

    public class CheckAvailabilityDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}