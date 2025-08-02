using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Shared.Models.DTOs.TimeTracking;
using System.Security.Claims;

namespace Employee.App.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimeClockController : ControllerBase
    {
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<TimeClockController> _logger;

        public TimeClockController(
            ITimeTrackingService timeTrackingService,
            IEmployeeService employeeService,
            ILogger<TimeClockController> logger)
        {
            _timeTrackingService = timeTrackingService;
            _employeeService = employeeService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener estado actual del empleado
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult> GetStatus()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var isCheckedIn = await _timeTrackingService.IsEmployeeCheckedInAsync(employeeId.Value);
                var isOnBreak = await _timeTrackingService.IsEmployeeOnBreakAsync(employeeId.Value);
                var status = await _timeTrackingService.GetEmployeeStatusAsync(employeeId.Value);
                var lastRecord = await _timeTrackingService.GetLastRecordAsync(employeeId.Value);

                return Ok(new
                {
                    employeeId = employeeId.Value,
                    isCheckedIn,
                    isOnBreak,
                    status,
                    lastActivity = lastRecord?.Timestamp,
                    lastActivityType = lastRecord?.Type.ToString(),
                    canCheckIn = !isCheckedIn,
                    canCheckOut = isCheckedIn,
                    canStartBreak = isCheckedIn && !isOnBreak,
                    canEndBreak = isOnBreak
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado del empleado");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Fichar entrada
        /// </summary>
        [HttpPost("check-in")]
        public async Task<ActionResult> CheckIn([FromBody] CheckInDto checkInDto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                // Validar que el empleado pueda fichar entrada
                if (!await _timeTrackingService.ValidateCheckInAsync(employeeId.Value))
                {
                    return BadRequest("No puedes fichar entrada. Ya tienes un registro activo.");
                }

                var timeRecord = await _timeTrackingService.CheckInAsync(employeeId.Value, checkInDto);

                _logger.LogInformation("Empleado {EmployeeId} fichó entrada a las {Timestamp}", 
                    employeeId.Value, timeRecord.Timestamp);

                return Ok(new
                {
                    success = true,
                    message = "Entrada registrada correctamente",
                    timestamp = timeRecord.Timestamp,
                    recordId = timeRecord.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al fichar entrada");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Fichar salida
        /// </summary>
        [HttpPost("check-out")]
        public async Task<ActionResult> CheckOut([FromBody] CheckOutDto checkOutDto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                // Validar que el empleado pueda fichar salida
                if (!await _timeTrackingService.ValidateCheckOutAsync(employeeId.Value))
                {
                    return BadRequest("No puedes fichar salida. No tienes un registro de entrada activo.");
                }

                var timeRecord = await _timeTrackingService.CheckOutAsync(employeeId.Value, checkOutDto);

                // Calcular horas trabajadas del día
                var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, DateTime.Today);

                _logger.LogInformation("Empleado {EmployeeId} fichó salida a las {Timestamp}. Horas trabajadas: {Hours}", 
                    employeeId.Value, timeRecord.Timestamp, workedHours);

                return Ok(new
                {
                    success = true,
                    message = "Salida registrada correctamente",
                    timestamp = timeRecord.Timestamp,
                    recordId = timeRecord.Id,
                    workedHours = workedHours.ToString(@"hh\:mm")
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al fichar salida");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Iniciar descanso
        /// </summary>
        [HttpPost("start-break")]
        public async Task<ActionResult> StartBreak([FromBody] CheckInDto breakDto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var timeRecord = await _timeTrackingService.StartBreakAsync(employeeId.Value, breakDto);

                _logger.LogInformation("Empleado {EmployeeId} inició descanso a las {Timestamp}", 
                    employeeId.Value, timeRecord.Timestamp);

                return Ok(new
                {
                    success = true,
                    message = "Descanso iniciado",
                    timestamp = timeRecord.Timestamp,
                    recordId = timeRecord.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar descanso");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Finalizar descanso
        /// </summary>
        [HttpPost("end-break")]
        public async Task<ActionResult> EndBreak([FromBody] CheckOutDto breakDto)
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var timeRecord = await _timeTrackingService.EndBreakAsync(employeeId.Value, breakDto);

                _logger.LogInformation("Empleado {EmployeeId} finalizó descanso a las {Timestamp}", 
                    employeeId.Value, timeRecord.Timestamp);

                return Ok(new
                {
                    success = true,
                    message = "Descanso finalizado",
                    timestamp = timeRecord.Timestamp,
                    recordId = timeRecord.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al finalizar descanso");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener registros del día actual
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult> GetTodayRecords()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var records = await _timeTrackingService.GetDailyRecordsAsync(employeeId.Value, DateTime.Today);
                var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, DateTime.Today);
                var breakTime = await _timeTrackingService.CalculateBreakTimeAsync(employeeId.Value, DateTime.Today);

                var recordsData = records.Select(r => new
                {
                    id = r.Id,
                    type = r.Type.ToString(),
                    timestamp = r.Timestamp,
                    notes = r.Notes,
                    location = r.Location
                }).ToList();

                return Ok(new
                {
                    date = DateTime.Today.ToString("yyyy-MM-dd"),
                    records = recordsData,
                    summary = new
                    {
                        workedHours = workedHours.ToString(@"hh\:mm"),
                        breakTime = breakTime.ToString(@"hh\:mm"),
                        totalRecords = records.Count(),
                        lastActivity = records.LastOrDefault()?.Timestamp
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros del día");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener registros de la semana actual
        /// </summary>
        [HttpGet("week")]
        public async Task<ActionResult> GetWeekRecords()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Lunes
                var weekData = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var date = startOfWeek.AddDays(i);
                    if (date <= DateTime.Today)
                    {
                        var dailyRecords = await _timeTrackingService.GetDailyRecordsAsync(employeeId.Value, date);
                        var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, date);

                        var checkIn = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckIn);
                        var checkOut = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckOut);

                        weekData.Add(new
                        {
                            date = date.ToString("yyyy-MM-dd"),
                            dayName = date.ToString("dddd"),
                            checkIn = checkIn?.Timestamp.ToString("HH:mm"),
                            checkOut = checkOut?.Timestamp.ToString("HH:mm"),
                            workedHours = workedHours.ToString(@"hh\:mm"),
                            isToday = date.Date == DateTime.Today
                        });
                    }
                }

                var totalWeekHours = TimeSpan.Zero;
                for (int i = 0; i < 7; i++)
                {
                    var date = startOfWeek.AddDays(i);
                    if (date <= DateTime.Today)
                    {
                        var dailyHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, date);
                        totalWeekHours = totalWeekHours.Add(dailyHours);
                    }
                }

                return Ok(new
                {
                    weekStart = startOfWeek.ToString("yyyy-MM-dd"),
                    weekEnd = startOfWeek.AddDays(6).ToString("yyyy-MM-dd"),
                    days = weekData,
                    summary = new
                    {
                        totalHours = totalWeekHours.ToString(@"hh\:mm"),
                        averageHoursPerDay = TimeSpan.FromTicks(totalWeekHours.Ticks / Math.Max(1, weekData.Count)).ToString(@"hh\:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener registros de la semana");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener resumen del empleado
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult> GetEmployeeSummary()
        {
            try
            {
                var employeeId = GetCurrentEmployeeId();
                if (employeeId == null) return Unauthorized();

                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId.Value);
                if (employee == null) return NotFound();

                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                
                // Horas del día
                var todayHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, today);
                
                // Horas del mes
                var monthHours = TimeSpan.Zero;
                var current = thisMonth;
                while (current <= today)
                {
                    var dailyHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId.Value, current);
                    monthHours = monthHours.Add(dailyHours);
                    current = current.AddDays(1);
                }

                var isCurrentlyWorking = await _timeTrackingService.IsEmployeeCheckedInAsync(employeeId.Value);
                var isOnBreak = await _timeTrackingService.IsEmployeeOnBreakAsync(employeeId.Value);

                return Ok(new
                {
                    employee = new
                    {
                        id = employee.Id,
                        name = employee.FullName,
                        employeeCode = employee.EmployeeCode,
                        department = employee.Department?.Name,
                        workSchedule = new
                        {
                            startTime = employee.WorkStartTime.ToString(@"hh\:mm"),
                            endTime = employee.WorkEndTime.ToString(@"hh\:mm")
                        }
                    },
                    status = new
                    {
                        isWorking = isCurrentlyWorking,
                        isOnBreak = isOnBreak,
                        currentStatus = await _timeTrackingService.GetEmployeeStatusAsync(employeeId.Value)
                    },
                    hours = new
                    {
                        today = todayHours.ToString(@"hh\:mm"),
                        thisMonth = monthHours.ToString(@"hh\:mm")
                    },
                    lastUpdate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen del empleado");
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
    }
}