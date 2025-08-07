using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Employee.App.Server.Data;
using Shared.Models.TimeTracking;
using Shared.Models.Enums;
using System.Globalization;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeClockController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        private readonly ILogger<TimeClockController> _logger;

        public TimeClockController(EmployeeDbContext context, ILogger<TimeClockController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("check-in")]
        public async Task<ActionResult<object>> CheckIn([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var now = DateTime.Now;
                var today = now.Date;

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

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckIn,
                    Date = today,
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

                _logger.LogInformation("Check-in registrado para empleado {EmployeeId} a las {Time}", 
                    employee.Id, now.ToString("HH:mm:ss"));

                return Ok(new
                {
                    success = true,
                    message = "Entrada registrada correctamente",
                    record = new
                    {
                        timeRecord.Id,
                        timeRecord.Type,
                        DateTime = timeRecord.Date.Add(timeRecord.Time),
                        timeRecord.Notes,
                        timeRecord.Location
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando check-in");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("check-out")]
        public async Task<ActionResult<object>> CheckOut([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var now = DateTime.Now;
                var today = now.Date;

                // Verificar que tenga un check-in activo
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id)
                    .OrderByDescending(tr => tr.Date)
                    .ThenByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                if (lastRecord?.Type != RecordType.CheckIn)
                {
                    return BadRequest(new { message = "No tienes una entrada registrada para poder marcar la salida." });
                }

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.CheckOut,
                    Date = today,
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

                // Calcular horas trabajadas del día
                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Date == today)
                    .OrderBy(tr => tr.Time)
                    .ToListAsync();

                var workedHours = CalculateWorkedHours(todayRecords);

                _logger.LogInformation("Check-out registrado para empleado {EmployeeId} a las {Time}", 
                    employee.Id, now.ToString("HH:mm:ss"));

                return Ok(new
                {
                    success = true,
                    message = "Salida registrada correctamente",
                    record = new
                    {
                        timeRecord.Id,
                        timeRecord.Type,
                        DateTime = timeRecord.Date.Add(timeRecord.Time),
                        timeRecord.Notes,
                        timeRecord.Location
                    },
                    summary = new
                    {
                        WorkedHours = workedHours,
                        TotalRecords = todayRecords.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando check-out");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-start")]
        public async Task<ActionResult<object>> StartBreak([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var now = DateTime.Now;

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakStart,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Inicio de descanso registrado",
                    record = new
                    {
                        timeRecord.Id,
                        timeRecord.Type,
                        DateTime = timeRecord.Date.Add(timeRecord.Time)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando inicio de descanso");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpPost("break-end")]
        public async Task<ActionResult<object>> EndBreak([FromBody] TimeClockRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var now = DateTime.Now;

                var timeRecord = new TimeRecord
                {
                    EmployeeId = employee.Id,
                    Type = RecordType.BreakEnd,
                    Date = now.Date,
                    Time = now.TimeOfDay,
                    Notes = request.Notes,
                    Location = request.Location,
                    IsManualEntry = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.TimeRecords.Add(timeRecord);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Fin de descanso registrado",
                    record = new
                    {
                        timeRecord.Id,
                        timeRecord.Type,
                        DateTime = timeRecord.Date.Add(timeRecord.Time)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando fin de descanso");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("current-status")]
        public async Task<ActionResult<object>> GetCurrentStatus()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var today = DateTime.Today;
                var lastRecord = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Date == today)
                    .OrderByDescending(tr => tr.Time)
                    .FirstOrDefaultAsync();

                var status = GetCurrentStatus(lastRecord);
                var isCheckedIn = lastRecord?.Type == RecordType.CheckIn || lastRecord?.Type == RecordType.BreakEnd;
                var isOnBreak = lastRecord?.Type == RecordType.BreakStart;

                return Ok(new
                {
                    status = status,
                    isCheckedIn = isCheckedIn,
                    isOnBreak = isOnBreak,
                    lastRecord = lastRecord != null ? new
                    {
                        lastRecord.Type,
                        DateTime = lastRecord.Date.Add(lastRecord.Time),
                        lastRecord.Notes
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estado actual");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("today-summary")]
        public async Task<ActionResult<object>> GetTodaySummary()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var today = DateTime.Today;
                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Date == today)
                    .OrderBy(tr => tr.Time)
                    .ToListAsync();

                var workedHours = CalculateWorkedHours(todayRecords);
                var breakTime = CalculateBreakTime(todayRecords);

                var summary = new
                {
                    Date = today,
                    Records = todayRecords.Select(r => new
                    {
                        r.Id,
                        r.Type,
                        DateTime = r.Date.Add(r.Time),
                        TimeDisplay = r.Time.ToString(@"hh\:mm"), // CORREGIDO: usar formato correcto
                        r.Notes,
                        TypeDisplay = r.Type switch
                        {
                            RecordType.CheckIn => "Entrada",
                            RecordType.CheckOut => "Salida",
                            RecordType.BreakStart => "Inicio Descanso",
                            RecordType.BreakEnd => "Fin Descanso",
                            _ => r.Type.ToString()
                        }
                    }).ToList(),
                    Summary = new
                    {
                        WorkedHours = workedHours,
                        BreakTime = breakTime,
                        CheckIn = todayRecords.FirstOrDefault(r => r.Type == RecordType.CheckIn)?.Time.ToString(@"hh\:mm"), // CORREGIDO
                        CheckOut = todayRecords.FirstOrDefault(r => r.Type == RecordType.CheckOut)?.Time.ToString(@"hh\:mm"), // CORREGIDO
                        Status = GetCurrentStatus(todayRecords.LastOrDefault())
                    }
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo resumen del día");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        // Métodos privados auxiliares
        private double CalculateWorkedHours(List<TimeRecord> records)
        {
            if (records.Count < 2) return 0;

            var checkIn = records.FirstOrDefault(r => r.Type == RecordType.CheckIn);
            var checkOut = records.FirstOrDefault(r => r.Type == RecordType.CheckOut);

            if (checkIn == null) return 0;

            var endTime = checkOut?.Time ?? DateTime.Now.TimeOfDay;
            var workedTime = endTime - checkIn.Time;

            // Restar tiempo de descansos
            var breakTime = CalculateBreakTime(records);
            var totalMinutes = workedTime.TotalMinutes - (breakTime * 60);

            return Math.Max(0, totalMinutes / 60.0);
        }

        private double CalculateBreakTime(List<TimeRecord> records)
        {
            double totalBreakHours = 0;
            TimeRecord? breakStart = null;

            foreach (var record in records.OrderBy(r => r.Time))
            {
                if (record.Type == RecordType.BreakStart)
                {
                    breakStart = record;
                }
                else if (record.Type == RecordType.BreakEnd && breakStart != null)
                {
                    var breakDuration = record.Time - breakStart.Time;
                    totalBreakHours += breakDuration.TotalHours;
                    breakStart = null;
                }
            }

            return totalBreakHours;
        }

        private string GetCurrentStatus(TimeRecord? lastRecord)
        {
            if (lastRecord == null) return "No fichado";

            return lastRecord.Type switch
            {
                RecordType.CheckIn => "Trabajando",
                RecordType.CheckOut => "Fuera",
                RecordType.BreakStart => "En descanso",
                RecordType.BreakEnd => "Trabajando",
                _ => "Desconocido"
            };
        }
    }

    // Clase para las peticiones de fichaje
    public class TimeClockRequest
    {
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}