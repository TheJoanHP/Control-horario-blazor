using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeRecordsController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly IReportService _reportService;
        private readonly ILogger<TimeRecordsController> _logger;

        public TimeRecordsController(
            CompanyDbContext context,
            ITimeTrackingService timeTrackingService,
            IReportService reportService,
            ILogger<TimeRecordsController> logger)
        {
            _context = context;
            _timeTrackingService = timeTrackingService;
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener registros de tiempo con filtros
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeRecordDto>>> GetTimeRecords(
            [FromQuery] int? employeeId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] RecordType? type = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                    .AsQueryable();

                if (employeeId.HasValue)
                    query = query.Where(tr => tr.EmployeeId == employeeId.Value);

                if (startDate.HasValue)
                    query = query.Where(tr => tr.Date >= startDate.Value.Date);

                if (endDate.HasValue)
                    query = query.Where(tr => tr.Date <= endDate.Value.Date);

                if (type.HasValue)
                    query = query.Where(tr => tr.Type == type.Value);

                var totalCount = await query.CountAsync();

                var records = await query
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(tr => new TimeRecordDto
                    {
                        Id = tr.Id,
                        EmployeeId = tr.EmployeeId,
                        EmployeeName = tr.Employee != null ? tr.Employee.User.FullName : "",
                        Type = tr.Type,
                        Date = tr.Date,
                        Time = tr.Time,
                        DateTime = tr.Date.Add(tr.Time),
                        Notes = tr.Notes,
                        Location = tr.Location,
                        Latitude = tr.Latitude,
                        Longitude = tr.Longitude,
                        IsManualEntry = tr.IsManualEntry,
                        CreatedAt = tr.CreatedAt
                    })
                    .ToListAsync();

                Response.Headers["X-Total-Count"] = totalCount.ToString();
                Response.Headers["X-Page"] = page.ToString();
                Response.Headers["X-Page-Size"] = pageSize.ToString();

                return Ok(records);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros de tiempo");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener registro específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeRecordDto>> GetTimeRecord(int id)
        {
            try
            {
                var record = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                    .Where(tr => tr.Id == id)
                    .Select(tr => new TimeRecordDto
                    {
                        Id = tr.Id,
                        EmployeeId = tr.EmployeeId,
                        EmployeeName = tr.Employee != null ? tr.Employee.User.FullName : "",
                        Type = tr.Type,
                        Date = tr.Date,
                        Time = tr.Time,
                        DateTime = tr.Date.Add(tr.Time),
                        Notes = tr.Notes,
                        Location = tr.Location,
                        Latitude = tr.Latitude,
                        Longitude = tr.Longitude,
                        IsManualEntry = tr.IsManualEntry,
                        CreatedAt = tr.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (record == null)
                    return NotFound();

                return Ok(record);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registro {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Crear registro manual de tiempo
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TimeRecordDto>> CreateTimeRecord([FromBody] CreateTimeRecordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employee = await _context.Employees.FindAsync(dto.EmployeeId);
                if (employee == null)
                    return BadRequest("Empleado no encontrado");

                var currentTime = DateTime.Now;
                var recordDateTime = dto.Date.Date.Add(dto.Time);

                var timeRecord = new TimeRecord
                {
                    EmployeeId = dto.EmployeeId,
                    Type = dto.Type,
                    Date = dto.Date.Date,
                    Time = dto.Time,
                    Notes = dto.Notes,
                    Location = dto.Location,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    IsManualEntry = true,
                    CreatedByUserId = GetCurrentUserId(),
                    CreatedAt = currentTime,
                    UpdatedAt = currentTime
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                var result = new TimeRecordDto
                {
                    Id = timeRecord.Id,
                    EmployeeId = timeRecord.EmployeeId,
                    EmployeeName = employee.User?.FullName ?? "",
                    Type = timeRecord.Type,
                    Date = timeRecord.Date,
                    Time = timeRecord.Time,
                    DateTime = timeRecord.Date.Add(timeRecord.Time),
                    Notes = timeRecord.Notes,
                    Location = timeRecord.Location,
                    Latitude = timeRecord.Latitude,
                    Longitude = timeRecord.Longitude,
                    IsManualEntry = timeRecord.IsManualEntry,
                    CreatedAt = timeRecord.CreatedAt
                };

                return CreatedAtAction(nameof(GetTimeRecord), new { id = timeRecord.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear registro de tiempo");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualizar registro de tiempo
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TimeRecordDto>> UpdateTimeRecord(int id, [FromBody] UpdateTimeRecordDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var timeRecord = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.User)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (timeRecord == null)
                    return NotFound();

                // Solo permitir editar registros manuales o con permisos especiales
                if (!timeRecord.IsManualEntry && !HasAdminPermissions())
                    return Forbid("Solo se pueden editar registros manuales");

                if (dto.Date.HasValue)
                    timeRecord.Date = dto.Date.Value.Date;

                if (dto.Time.HasValue)
                    timeRecord.Time = dto.Time.Value;

                if (dto.Type.HasValue)
                    timeRecord.Type = dto.Type.Value;

                if (dto.Notes != null)
                    timeRecord.Notes = dto.Notes;

                if (dto.Location != null)
                    timeRecord.Location = dto.Location;

                if (dto.Latitude.HasValue)
                    timeRecord.Latitude = dto.Latitude;

                if (dto.Longitude.HasValue)
                    timeRecord.Longitude = dto.Longitude;

                timeRecord.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var result = new TimeRecordDto
                {
                    Id = timeRecord.Id,
                    EmployeeId = timeRecord.EmployeeId,
                    EmployeeName = timeRecord.Employee?.User?.FullName ?? "",
                    Type = timeRecord.Type,
                    Date = timeRecord.Date,
                    Time = timeRecord.Time,
                    DateTime = timeRecord.Date.Add(timeRecord.Time),
                    Notes = timeRecord.Notes,
                    Location = timeRecord.Location,
                    Latitude = timeRecord.Latitude,
                    Longitude = timeRecord.Longitude,
                    IsManualEntry = timeRecord.IsManualEntry,
                    CreatedAt = timeRecord.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar registro {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Eliminar registro de tiempo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeRecord(int id)
        {
            try
            {
                var timeRecord = await _context.TimeRecords.FindAsync(id);
                if (timeRecord == null)
                    return NotFound();

                // Solo permitir eliminar registros manuales o con permisos especiales
                if (!timeRecord.IsManualEntry && !HasAdminPermissions())
                    return Forbid("Solo se pueden eliminar registros manuales");

                _context.TimeRecords.Remove(timeRecord);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar registro {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener estadísticas de un empleado para un período
        /// </summary>
        [HttpGet("employee/{employeeId}/stats")]
        public async Task<ActionResult<object>> GetEmployeeStats(
            int employeeId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var fromDate = startDate ?? DateTime.Today.AddDays(-30);
                var toDate = endDate ?? DateTime.Today;

                var stats = await _reportService.GetEmployeeStatsAsync(employeeId, fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del empleado {EmployeeId}", employeeId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Check-in de empleado
        /// </summary>
        [HttpPost("checkin")]
        public async Task<ActionResult<TimeRecordDto>> CheckIn([FromBody] CheckInDto dto)
        {
            try
            {
                var record = await _timeTrackingService.CheckInAsync(dto);
                
                var result = new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    Type = record.Type,
                    Date = record.Date,
                    Time = record.Time,
                    DateTime = record.Date.Add(record.Time),
                    Notes = record.Notes,
                    Location = record.Location,
                    Latitude = record.Latitude,
                    Longitude = record.Longitude,
                    IsManualEntry = record.IsManualEntry,
                    CreatedAt = record.CreatedAt
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-in");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Check-out de empleado
        /// </summary>
        [HttpPost("checkout")]
        public async Task<ActionResult<TimeRecordDto>> CheckOut([FromBody] CheckOutDto dto)
        {
            try
            {
                var record = await _timeTrackingService.CheckOutAsync(dto);
                
                var result = new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    Type = record.Type,
                    Date = record.Date,
                    Time = record.Time,
                    DateTime = record.Date.Add(record.Time),
                    Notes = record.Notes,
                    Location = record.Location,
                    Latitude = record.Latitude,
                    Longitude = record.Longitude,
                    IsManualEntry = record.IsManualEntry,
                    CreatedAt = record.CreatedAt
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en check-out");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private bool HasAdminPermissions()
        {
            var role = User.FindFirst("role")?.Value;
            return role == UserRole.CompanyAdmin.ToString() || role == UserRole.SuperAdmin.ToString();
        }
    }
}