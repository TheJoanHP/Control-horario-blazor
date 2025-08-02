using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.Enums;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly CompanyDbContext _context;

        public DashboardController(CompanyDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<ActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek + 1);
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);

                // Estadísticas generales
                var totalEmployees = await _context.Employees
                    .Where(e => e.Active)
                    .CountAsync();

                var activeEmployees = await _context.Users
                    .Where(u => u.Active && u.Role == "EMPLOYEE")
                    .CountAsync();

                // Asistencia de hoy
                var todayCheckIns = await _context.TimeRecords
                    .Where(tr => tr.Timestamp >= today && tr.Type == RecordType.CheckIn)
                    .Select(tr => tr.EmployeeId)
                    .Distinct()
                    .CountAsync();

                var todayCheckOuts = await _context.TimeRecords
                    .Where(tr => tr.Timestamp >= today && tr.Type == RecordType.CheckOut)
                    .Select(tr => tr.EmployeeId)
                    .Distinct()
                    .CountAsync();

                var currentlyWorking = todayCheckIns - todayCheckOuts;

                // Horas trabajadas este mes
                var monthlyRecords = await _context.TimeRecords
                    .Where(tr => tr.Timestamp >= thisMonthStart)
                    .ToListAsync();

                var monthlyHours = CalculateTotalHours(monthlyRecords);

                // Llegadas tarde (más de 15 minutos después de las 9:00)
                var lateArrivals = await _context.TimeRecords
                    .Where(tr => tr.Timestamp >= thisMonthStart && 
                                tr.Type == RecordType.CheckIn &&
                                tr.Timestamp.TimeOfDay > new TimeSpan(9, 15, 0))
                    .CountAsync();

                // Solicitudes de vacaciones pendientes
                var pendingVacations = await _context.VacationRequests
                    .Where(vr => vr.Status == VacationStatus.Pending)
                    .CountAsync();

                // Departamentos con más empleados
                var departmentStats = await _context.Employees
                    .Where(e => e.Active)
                    .GroupBy(e => e.Department)
                    .Select(g => new
                    {
                        department = g.Key,
                        employeeCount = g.Count(),
                        todayAttendance = g.Count(e => _context.TimeRecords
                            .Any(tr => tr.EmployeeId == e.Id && 
                                      tr.Timestamp >= today && 
                                      tr.Type == RecordType.CheckIn))
                    })
                    .OrderByDescending(d => d.employeeCount)
                    .ToListAsync();

                // Empleados que aún no han fichado hoy
                var employeesNotCheckedIn = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.Active && !_context.TimeRecords
                        .Any(tr => tr.EmployeeId == e.Id && 
                                  tr.Timestamp >= today && 
                                  tr.Type == RecordType.CheckIn))
                    .Select(e => new
                    {
                        e.Id,
                        e.User.Name,
                        e.EmployeeCode,
                        e.Department
                    })
                    .ToListAsync();

                // Horas promedio por empleado esta semana
                var weeklyRecords = await _context.TimeRecords
                    .Where(tr => tr.Timestamp >= thisWeekStart)
                    .ToListAsync();

                var weeklyHours = CalculateTotalHours(weeklyRecords);
                var avgHoursPerEmployee = activeEmployees > 0 ? weeklyHours / activeEmployees : 0;

                return Ok(new
                {
                    success = true,
                    overview = new
                    {
                        totalEmployees,
                        activeEmployees,
                        currentlyWorking,
                        attendanceRate = totalEmployees > 0 ? Math.Round((double)todayCheckIns / totalEmployees * 100, 1) : 0
                    },
                    today = new
                    {
                        date = today.ToString("dd/MM/yyyy"),
                        checkIns = todayCheckIns,
                        checkOuts = todayCheckOuts,
                        currentlyWorking,
                        notCheckedIn = employeesNotCheckedIn.Count
                    },
                    thisMonth = new
                    {
                        totalHours = Math.Round(monthlyHours, 2),
                        lateArrivals,
                        pendingVacations,
                        avgHoursPerEmployee = Math.Round(avgHoursPerEmployee, 2)
                    },
                    departments = departmentStats,
                    alerts = new
                    {
                        employeesNotCheckedIn = employeesNotCheckedIn.Take(5),
                        pendingVacations,
                        lateArrivals
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo estadísticas del dashboard" });
            }
        }

        [HttpGet("attendance-chart")]
        public async Task<ActionResult> GetAttendanceChart([FromQuery] int days = 7)
        {
            try
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-days);
                var chartData = new List<object>();

                for (int i = days; i >= 0; i--)
                {
                    var date = DateTime.UtcNow.Date.AddDays(-i);
                    
                    var checkIns = await _context.TimeRecords
                        .Where(tr => tr.Timestamp.Date == date && tr.Type == RecordType.CheckIn)
                        .CountAsync();

                    var checkOuts = await _context.TimeRecords
                        .Where(tr => tr.Timestamp.Date == date && tr.Type == RecordType.CheckOut)
                        .CountAsync();

                    var lateArrivals = await _context.TimeRecords
                        .Where(tr => tr.Timestamp.Date == date && 
                                    tr.Type == RecordType.CheckIn &&
                                    tr.Timestamp.TimeOfDay > new TimeSpan(9, 15, 0))
                        .CountAsync();

                    chartData.Add(new
                    {
                        date = date.ToString("dd/MM"),
                        dayName = date.ToString("ddd", new System.Globalization.CultureInfo("es-ES")),
                        checkIns,
                        checkOuts,
                        lateArrivals,
                        isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
                    });
                }

                return Ok(new
                {
                    success = true,
                    chartData,
                    period = $"Últimos {days} días"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo datos del gráfico" });
            }
        }

        [HttpGet("live-attendance")]
        public async Task<ActionResult> GetLiveAttendance()
        {
            try
            {
                var today = DateTime.UtcNow.Date;

                var liveData = await _context.Employees
                    .Include(e => e.User)
                    .Where(e => e.Active)
                    .Select(e => new
                    {
                        e.Id,
                        e.User.Name,
                        e.EmployeeCode,
                        e.Department,
                        e.Position,
                        lastRecord = _context.TimeRecords
                            .Where(tr => tr.EmployeeId == e.Id && tr.Timestamp >= today)
                            .OrderByDescending(tr => tr.Timestamp)
                            .Select(tr => new
                            {
                                tr.Type,
                                tr.Timestamp,
                                FormattedTime = tr.Timestamp.ToString("HH:mm")
                            })
                            .FirstOrDefault(),
                        todayHours = CalculateEmployeeDayHours(e.Id, today)
                    })
                    .ToListAsync();

                var processedData = liveData.Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.EmployeeCode,
                    e.Department,
                    e.Position,
                    status = GetEmployeeStatus(e.lastRecord?.Type),
                    lastActivity = e.lastRecord?.FormattedTime ?? "Sin actividad",
                    hoursToday = e.todayHours,
                    statusColor = GetStatusColor(e.lastRecord?.Type)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    employees = processedData,
                    summary = new
                    {
                        total = processedData.Count,
                        working = processedData.Count(e => e.status == "Trabajando"),
                        onBreak = processedData.Count(e => e.status == "En descanso" || e.status == "En comida"),
                        absent = processedData.Count(e => e.status == "Sin actividad" || e.status == "Fuera del trabajo")
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo asistencia en vivo" });
            }
        }

        private double CalculateTotalHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var groupedByEmployee = records.GroupBy(r => r.EmployeeId);
            double totalHours = 0;

            foreach (var employeeGroup in groupedByEmployee)
            {
                var employeeRecords = employeeGroup.GroupBy(r => r.Timestamp.Date);
                
                foreach (var dayGroup in employeeRecords)
                {
                    var dayRecords = dayGroup.OrderBy(r => r.Timestamp).ToList();
                    Shared.Models.TimeTracking.TimeRecord? lastCheckIn = null;

                    foreach (var record in dayRecords)
                    {
                        if (record.Type == RecordType.CheckIn)
                            lastCheckIn = record;
                        else if (record.Type == RecordType.CheckOut && lastCheckIn != null)
                        {
                            totalHours += (record.Timestamp - lastCheckIn.Timestamp).TotalHours;
                            lastCheckIn = null;
                        }
                    }
                }
            }

            return totalHours;
        }

        private double CalculateEmployeeDayHours(int employeeId, DateTime date)
        {
            var records = _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Timestamp.Date == date)
                .OrderBy(tr => tr.Timestamp)
                .ToList();

            double hours = 0;
            Shared.Models.TimeTracking.TimeRecord? lastCheckIn = null;

            foreach (var record in records)
            {
                if (record.Type == RecordType.CheckIn)
                    lastCheckIn = record;
                else if (record.Type == RecordType.CheckOut && lastCheckIn != null)
                {
                    hours += (record.Timestamp - lastCheckIn.Timestamp).TotalHours;
                    lastCheckIn = null;
                }
            }

            return Math.Round(hours, 2);
        }

        private string GetEmployeeStatus(RecordType? lastRecordType)
        {
            return lastRecordType switch
            {
                RecordType.CheckIn => "Trabajando",
                RecordType.CheckOut => "Fuera del trabajo",
                RecordType.BreakStart => "En descanso",
                RecordType.BreakEnd => "Trabajando",
                RecordType.LunchStart => "En comida",
                RecordType.LunchEnd => "Trabajando",
                null => "Sin actividad",
                _ => "Estado desconocido"
            };
        }

        private string GetStatusColor(RecordType? lastRecordType)
        {
            return lastRecordType switch
            {
                RecordType.CheckIn => "success",
                RecordType.CheckOut => "secondary",
                RecordType.BreakStart => "warning",
                RecordType.BreakEnd => "success",
                RecordType.LunchStart => "info",
                RecordType.LunchEnd => "success",
                null => "danger",
                _ => "secondary"
            };
        }
    }
}