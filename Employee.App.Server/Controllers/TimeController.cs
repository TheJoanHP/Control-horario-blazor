using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Employee.App.Server.Data; // Corregido namespace
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace Employee.App.Server.Controllers // Corregido namespace
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeController : ControllerBase
    {
        private readonly EmployeeDbContext _context;

        public TimeController(EmployeeDbContext context)
        {
            _context = context;
        }

        [HttpPost("check-in")]
        public async Task<ActionResult<TimeRecord>> CheckIn([FromBody] TimeClockRequest request)
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
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type == RecordType.CheckIn)
                {
                    return BadRequest(new { message = "Ya tienes una entrada registrada. Debes registrar la salida primero." });
                }

                // Crear registro de entrada
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckIn,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("check-out")]
        public async Task<ActionResult<TimeRecord>> CheckOut([FromBody] TimeClockRequest request)
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
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.CheckIn)
                {
                    return BadRequest(new { message = "No hay una entrada activa para registrar la salida." });
                }

                // Crear registro de salida
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckOut,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-start")]
        public async Task<ActionResult<TimeRecord>> StartBreak([FromBody] TimeClockRequest request)
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
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.CheckIn)
                {
                    return BadRequest(new { message = "Debes estar trabajando para tomar un descanso." });
                }

                // Crear registro de inicio de descanso
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakStart,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-end")]
        public async Task<ActionResult<TimeRecord>> EndBreak([FromBody] TimeClockRequest request)
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
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.BreakStart)
                {
                    return BadRequest(new { message = "No estás en un descanso activo." });
                }

                // Crear registro de fin de descanso
                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakEnd,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Location = request.Location,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    IsManualEntry = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(timeRecord);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult> GetCurrentStatus()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                var status = lastRecord?.Type switch
                {
                    RecordType.CheckIn => "Trabajando",
                    RecordType.CheckOut => "Fuera del trabajo",
                    RecordType.BreakStart => "En descanso",
                    RecordType.BreakEnd => "Trabajando",
                    _ => "Sin estado"
                };

                return Ok(new
                {
                    Status = status,
                    LastRecord = lastRecord != null ? new
                    {
                        lastRecord.Type,
                        lastRecord.Timestamp,
                        lastRecord.Location
                    } : null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }

    // DTO para las solicitudes de tiempo
    public class TimeClockRequest
    {
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}