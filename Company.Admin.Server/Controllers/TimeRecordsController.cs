using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.DTOs.TimeTracking;
using Shared.Models.Enums;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeRecordsController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<TimeRecordsController> _logger;

        public TimeRecordsController(
            CompanyDbContext context, 
            IMapper mapper,
            ILogger<TimeRecordsController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: api/timerecords
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeRecordDto>>> GetTimeRecords(
            [FromQuery] int? employeeId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] RecordType? recordType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .AsQueryable();

                // Filtros
                if (employeeId.HasValue)
                    query = query.Where(tr => tr.EmployeeId == employeeId.Value);

                if (fromDate.HasValue)
                    query = query.Where(tr => tr.Timestamp.Date >= fromDate.Value.Date);

                if (toDate.HasValue)
                    query = query.Where(tr => tr.Timestamp.Date <= toDate.Value.Date);

                if (recordType.HasValue)
                    query = query.Where(tr => tr.Type == recordType.Value);

                // Paginación
                var totalRecords = await query.CountAsync();
                var records = await query
                    .OrderByDescending(tr => tr.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var recordDtos = records.Select(record => new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
                    EmployeeCode = record.Employee.EmployeeCode,
                    Type = record.Type,
                    Timestamp = record.Timestamp,
                    Notes = record.Notes,
                    Location = record.Location,
                    DeviceInfo = record.DeviceInfo,
                    IpAddress = record.IpAddress,
                    CreatedAt = record.CreatedAt
                }).ToList();

                // Agregar headers de paginación
                Response.Headers.Add("X-Total-Count", totalRecords.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(recordDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registros de tiempo");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/timerecords/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeRecordDto>> GetTimeRecord(int id)
        {
            try
            {
                var record = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (record == null)
                    return NotFound($"Registro de tiempo con ID {id} no encontrado");

                var recordDto = new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
                    EmployeeCode = record.Employee.EmployeeCode,
                    Type = record.Type,
                    Timestamp = record.Timestamp,
                    Notes = record.Notes,
                    Location = record.Location,
                    DeviceInfo = record.DeviceInfo,
                    IpAddress = record.IpAddress,
                    CreatedAt = record.CreatedAt
                };

                return Ok(recordDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registro de tiempo con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // POST: api/timerecords
        [HttpPost]
        public async Task<ActionResult<TimeRecordDto>> CreateTimeRecord(CreateTimeRecordDto createDto)
        {
            try
            {
                // Validar que el empleado existe
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == createDto.EmployeeId && e.Active);

                if (employee == null)
                    return BadRequest("Empleado no encontrado o inactivo");

                // Validar lógica de negocio según el tipo de registro
                var validationResult = await ValidateTimeRecord(createDto);
                if (!validationResult.IsValid)
                    return BadRequest(validationResult.ErrorMessage);

                var timeRecord = new TimeRecord
                {
                    EmployeeId = createDto.EmployeeId,
                    Type = createDto.Type,
                    Timestamp = createDto.Timestamp ?? DateTime.UtcNow,
                    Notes = createDto.Notes,
                    Location = createDto.Location,
                    DeviceInfo = createDto.DeviceInfo,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                // Recargar con el empleado para el DTO
                await _context.Entry(timeRecord)
                    .Reference(tr => tr.Employee)
                    .LoadAsync();

                var recordDto = new TimeRecordDto
                {
                    Id = timeRecord.Id,
                    EmployeeId = timeRecord.EmployeeId,
                    EmployeeName = $"{timeRecord.Employee.FirstName} {timeRecord.Employee.LastName}",
                    EmployeeCode = timeRecord.Employee.EmployeeCode,
                    Type = timeRecord.Type,
                    Timestamp = timeRecord.Timestamp,
                    Notes = timeRecord.Notes,
                    Location = timeRecord.Location,
                    DeviceInfo = timeRecord.DeviceInfo,
                    IpAddress = timeRecord.IpAddress,
                    CreatedAt = timeRecord.CreatedAt
                };

                return CreatedAtAction(nameof(GetTimeRecord), new { id = timeRecord.Id }, recordDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando registro de tiempo");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // PUT: api/timerecords/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TimeRecordDto>> UpdateTimeRecord(int id, UpdateTimeRecordDto updateDto)
        {
            try
            {
                var record = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .FirstOrDefaultAsync(tr => tr.Id == id);

                if (record == null)
                    return NotFound($"Registro de tiempo con ID {id} no encontrado");

                // Solo permitir actualizar ciertos campos
                if (!string.IsNullOrEmpty(updateDto.Notes))
                    record.Notes = updateDto.Notes;

                if (!string.IsNullOrEmpty(updateDto.Location))
                    record.Location = updateDto.Location;

                if (updateDto.Timestamp.HasValue)
                {
                    // Validar que la nueva fecha/hora es razonable
                    var now = DateTime.UtcNow;
                    if (updateDto.Timestamp.Value > now.AddMinutes(5) || 
                        updateDto.Timestamp.Value < now.AddDays(-30))
                    {
                        return BadRequest("La fecha/hora no está en un rango válido");
                    }
                    record.Timestamp = updateDto.Timestamp.Value;
                }

                await _context.SaveChangesAsync();

                var recordDto = new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
                    EmployeeCode = record.Employee.EmployeeCode,
                    Type = record.Type,
                    Timestamp = record.Timestamp,
                    Notes = record.Notes,
                    Location = record.Location,
                    DeviceInfo = record.DeviceInfo,
                    IpAddress = record.IpAddress,
                    CreatedAt = record.CreatedAt
                };

                return Ok(recordDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando registro de tiempo con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // DELETE: api/timerecords/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeRecord(int id)
        {
            try
            {
                var record = await _context.TimeRecords.FindAsync(id);
                if (record == null)
                    return NotFound($"Registro de tiempo con ID {id} no encontrado");

                _context.TimeRecords.Remove(record);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando registro de tiempo con ID {Id}", id);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/timerecords/employee/{employeeId}/today
        [HttpGet("employee/{employeeId}/today")]
        public async Task<ActionResult<IEnumerable<TimeRecordDto>>> GetTodayRecords(int employeeId)
        {
            try
            {
                var today = DateTime.Today;
                var records = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .Where(tr => tr.EmployeeId == employeeId && 
                                tr.Timestamp.Date == today)
                    .OrderBy(tr => tr.Timestamp)
                    .ToListAsync();

                var recordDtos = records.Select(record => new TimeRecordDto
                {
                    Id = record.Id,
                    EmployeeId = record.EmployeeId,
                    EmployeeName = $"{record.Employee.FirstName} {record.Employee.LastName}",
                    EmployeeCode = record.Employee.EmployeeCode,
                    Type = record.Type,
                    Timestamp = record.Timestamp,
                    Notes = record.Notes,
                    Location = record.Location,
                    DeviceInfo = record.DeviceInfo,
                    IpAddress = record.IpAddress,
                    CreatedAt = record.CreatedAt
                }).ToList();

                return Ok(recordDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registros de hoy para empleado {EmployeeId}", employeeId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // GET: api/timerecords/employee/{employeeId}/status
        [HttpGet("employee/{employeeId}/status")]
        public async Task<ActionResult<object>> GetEmployeeStatus(int employeeId)
        {
            try
            {
                var today = DateTime.Today;
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId)
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && 
                                tr.Timestamp.Date == today)
                    .OrderBy(tr => tr.Timestamp)
                    .ToListAsync();

                var status = new
                {
                    IsWorking = lastRecord?.Type == RecordType.CheckIn || 
                               lastRecord?.Type == RecordType.BreakEnd,
                    LastRecord = lastRecord?.Type.ToString(),
                    LastRecordTime = lastRecord?.Timestamp,
                    TodayRecords = todayRecords.Count,
                    CheckInTime = todayRecords.FirstOrDefault(r => r.Type == RecordType.CheckIn)?.Timestamp,
                    CheckOutTime = todayRecords.LastOrDefault(r => r.Type == RecordType.CheckOut)?.Timestamp
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado del empleado {EmployeeId}", employeeId);
                return StatusCode(500, "Error interno del servidor");
            }
        }

        #region Private Methods

        private async Task<(bool IsValid, string ErrorMessage)> ValidateTimeRecord(CreateTimeRecordDto dto)
        {
            var today = DateTime.Today;
            var lastRecord = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == dto.EmployeeId)
                .OrderByDescending(tr => tr.Timestamp)
                .FirstOrDefaultAsync();

            switch (dto.Type)
            {
                case RecordType.CheckIn:
                    if (lastRecord?.Type == RecordType.CheckIn && lastRecord.Timestamp.Date == today)
                        return (false, "Ya has fichado entrada hoy");
                    break;

                case RecordType.CheckOut:
                    if (lastRecord?.Type != RecordType.CheckIn && lastRecord?.Type != RecordType.BreakEnd)
                        return (false, "Debes fichar entrada antes de salir");
                    break;

                case RecordType.BreakStart:
                    if (lastRecord?.Type != RecordType.CheckIn && lastRecord?.Type != RecordType.BreakEnd)
                        return (false, "Debes estar trabajando para tomar un descanso");
                    break;

                case RecordType.BreakEnd:
                    if (lastRecord?.Type != RecordType.BreakStart)
                        return (false, "Debes estar en descanso para terminar el descanso");
                    break;
            }

            return (true, string.Empty);
        }

        private string GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }
}