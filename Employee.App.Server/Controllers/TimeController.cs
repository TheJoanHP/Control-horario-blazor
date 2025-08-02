using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeApp.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;

namespace EmployeeApp.Server.Controllers
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

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckIn,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Location = request.Location,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                // Cargar el registro completo
                timeRecord = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                        .ThenInclude(e => e.User)
                    .FirstAsync(tr => tr.Id == timeRecord.Id);

                return Ok(new
                {
                    success = true,
                    message = "Entrada registrada correctamente",
                    record = timeRecord,
                    timestamp = timeRecord.Timestamp.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error registrando entrada" });
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
                    return BadRequest(new { message = "No tienes una entrada registrada. Debes registrar la entrada primero." });
                }

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckOut,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    Location = request.Location,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                // Calcular horas trabajadas
                var hoursWorked = (timeRecord.Timestamp - lastRecord.Timestamp).TotalHours;

                // Cargar el registro completo
                timeRecord = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                        .ThenInclude(e => e.User)
                    .FirstAsync(tr => tr.Id == timeRecord.Id);

                return Ok(new
                {
                    success = true,
                    message = "Salida registrada correctamente",
                    record = timeRecord,
                    timestamp = timeRecord.Timestamp.ToString("HH:mm:ss"),
                    hoursWorked = Math.Round(hoursWorked, 2)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error registrando salida" });
            }
        }

        [HttpPost("break-start")]
        public async Task<ActionResult<TimeRecord>> StartBreak([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakStart,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Inicio de descanso registrado",
                    record = timeRecord,
                    timestamp = timeRecord.Timestamp.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error registrando inicio de descanso" });
            }
        }

        [HttpPost("break-end")]
        public async Task<ActionResult<TimeRecord>> EndBreak([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakEnd,
                    Timestamp = DateTime.UtcNow,
                    Notes = request.Notes,
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Fin de descanso registrado",
                    record = timeRecord,
                    timestamp = timeRecord.Timestamp.ToString("HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error registrando fin de descanso" });
            }
        }

        [HttpGet("my-records")]
        public async Task<ActionResult> GetMyRecords([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Fechas por defecto (último mes)
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && 
                                tr.Timestamp >= fromDate && 
                                tr.Timestamp <= toDate)
                    .OrderByDescending(tr => tr.Timestamp)
                    .Select(tr => new
                    {
                        tr.Id,
                        tr.Type,
                        tr.Timestamp,
                        tr.Notes,
                        tr.Location,
                        FormattedTime = tr.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                        TypeName = tr.Type.ToString()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    records = records,
                    total = records.Count,
                    from = fromDate,
                    to = toDate
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo registros" });
            }
        }

        [HttpGet("current-status")]
        public async Task<ActionResult> GetCurrentStatus()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Obtener último registro del día
                var today = DateTime.UtcNow.Date;
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && 
                                tr.Timestamp >= today)
                    .OrderByDescending(tr => tr.Timestamp)
                    .FirstOrDefaultAsync();

                // Obtener todos los registros de hoy para calcular horas
                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && 
                                tr.Timestamp >= today)
                    .OrderBy(tr => tr.Timestamp)
                    .ToListAsync();

                var hoursToday = CalculateHoursWorked(todayRecords);

                return Ok(new
                {
                    success = true,
                    employee = new
                    {
                        employee.Id,
                        employee.User.Name,
                        employee.EmployeeCode,
                        employee.Department,
                        employee.Position,
                        Company = employee.Company.Name
                    },
                    currentStatus = GetStatusFromLastRecord(lastRecord),
                    lastRecord = lastRecord != null ? new
                    {
                        lastRecord.Type,
                        lastRecord.Timestamp,
                        FormattedTime = lastRecord.Timestamp.ToString("HH:mm:ss")
                    } : null,
                    hoursToday = Math.Round(hoursToday, 2),
                    todayRecordsCount = todayRecords.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo estado actual" });
            }
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (Request.Headers.ContainsKey("X-Real-IP"))
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
            return ipAddress ?? "Unknown";
        }

        private string GetStatusFromLastRecord(TimeRecord? lastRecord)
        {
            if (lastRecord == null)
                return "Sin registros";

            return lastRecord.Type switch
            {
                RecordType.CheckIn => "Trabajando",
                RecordType.CheckOut => "Fuera del trabajo",
                RecordType.BreakStart => "En descanso",
                RecordType.BreakEnd => "Trabajando",
                RecordType.LunchStart => "En comida",
                RecordType.LunchEnd => "Trabajando",
                _ => "Estado desconocido"
            };
        }

        private double CalculateHoursWorked(List<TimeRecord> records)
        {
            double totalHours = 0;
            TimeRecord? lastCheckIn = null;

            foreach (var record in records)
            {
                switch (record.Type)
                {
                    case RecordType.CheckIn:
                        lastCheckIn = record;
                        break;
                    case RecordType.CheckOut:
                        if (lastCheckIn != null)
                        {
                            totalHours += (record.Timestamp - lastCheckIn.Timestamp).TotalHours;
                            lastCheckIn = null;
                        }
                        break;
                }
            }

            return totalHours;
        }
    }

    public class TimeClockRequest
    {
        public string? Notes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Location { get; set; }
    }
}