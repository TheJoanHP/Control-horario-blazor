using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Employee.App.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        private readonly ILogger<TimeController> _logger;

        public TimeController(EmployeeDbContext context, ILogger<TimeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("check-in")]
        public async Task<ActionResult<TimeRecord>> CheckIn([FromBody] SimpleTimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Verificar que no tenga un check-in activo
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type == RecordType.CheckIn)
                {
                    return BadRequest(new { message = "Ya tienes una entrada registrada. Debes registrar la salida primero." });
                }

                var now = DateTime.Now;
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckIn,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Check-in registrado para empleado {EmployeeId}", employee.Id);

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando check-in");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("check-out")]
        public async Task<ActionResult<TimeRecord>> CheckOut([FromBody] SimpleTimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Verificar que tenga un check-in activo
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.CheckIn && lastRecord?.Type != RecordType.BreakEnd)
                {
                    return BadRequest(new { message = "No tienes una entrada registrada para poder marcar la salida." });
                }

                var now = DateTime.Now;
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckOut,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Check-out registrado para empleado {EmployeeId}", employee.Id);

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando check-out");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-start")]
        public async Task<ActionResult<TimeRecord>> StartBreak([FromBody] SimpleTimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Verificar que esté en horario de trabajo
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.CheckIn && lastRecord?.Type != RecordType.BreakEnd)
                {
                    return BadRequest(new { message = "Debes estar trabajando para tomar un descanso." });
                }

                var now = DateTime.Now;
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakStart,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Break start registrado para empleado {EmployeeId}", employee.Id);

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando inicio de descanso");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-end")]
        public async Task<ActionResult<TimeRecord>> EndBreak([FromBody] SimpleTimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Verificar que esté en descanso
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.BreakStart)
                {
                    return BadRequest(new { message = "No estás en un descanso activo." });
                }

                var now = DateTime.Now;
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakEnd,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Break end registrado para empleado {EmployeeId}", employee.Id);

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando fin de descanso");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("today")]
        public async Task<ActionResult<object>> GetTodayRecords()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var today = DateTime.Today;
                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Date == today)
                    .OrderBy(tr => tr.Time)
                    .ToListAsync();

                return Ok(new
                {
                    Date = today,
                    Records = records,
                    Summary = new
                    {
                        TotalRecords = records.Count,
                        CheckIn = records.FirstOrDefault(r => r.Type == RecordType.CheckIn)?.Time,
                        CheckOut = records.FirstOrDefault(r => r.Type == RecordType.CheckOut)?.Time,
                        BreakCount = records.Count(r => r.Type == RecordType.BreakStart)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo registros de hoy");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    // DTO simplificado para este controlador
    public class SimpleTimeClockRequest
    {
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}