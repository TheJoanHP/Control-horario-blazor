using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeApp.Server.Data;
using Shared.Models.Enums;

namespace EmployeeApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly EmployeeDbContext _context;

        public DashboardController(EmployeeDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetDashboardSummary()
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

                var today = DateTime.UtcNow.Date;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Lunes
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);

                // Estadísticas de hoy
                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Timestamp >= today)
                    .OrderBy(tr => tr.Timestamp)
                    .ToListAsync();

                var hoursToday = CalculateHoursWorked(todayRecords);
                var statusToday = GetCurrentStatus(todayRecords.LastOrDefault());

                // Estadísticas de esta semana
                var weekRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Timestamp >= thisWeekStart)
                    .ToListAsync();

                var hoursThisWeek = CalculateWeeklyHours(weekRecords);
                var daysWorkedThisWeek = weekRecords
                    .Where(tr => tr.Type == RecordType.CheckIn)
                    .Select(tr => tr.Timestamp.Date)
                    .Distinct()
                    .Count();

                // Estadísticas del mes
                var monthRecords = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employee.Id && tr.Timestamp >= thisMonthStart)
                    .ToListAsync();

                var hoursThisMonth = CalculateWeeklyHours(monthRecords);
                var daysWorkedThisMonth = monthRecords
                    .Where(tr => tr.Type == RecordType.CheckIn)
                    .Select(tr => tr.Timestamp.Date)
                    .Distinct()
                    .Count();

                // Vacaciones pendientes
                var pendingVacations = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id && vr.Status == VacationStatus.Pending)
                    .CountAsync();

                // Próximas vacaciones aprobadas
                var upcomingVacations = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id && 
                                vr.Status == VacationStatus.Approved &&
                                vr.StartDate >= today)
                    .OrderBy(vr => vr.StartDate)
                    .Take(3)
                    .Select(vr => new
                    {
                        vr.Id,
                        vr.StartDate,
                        vr.EndDate,
                        vr.DaysRequested,
                        FormattedDates = $"{vr.StartDate:dd/MM} - {vr.EndDate:dd/MM}"
                    })
                    .ToListAsync();

                // Horario de trabajo esperado
                var expectedDailyHours = (employee.Company.WorkEndTime - employee.Company.WorkStartTime).TotalHours;
                var expectedWeeklyHours = expectedDailyHours * 5; // 5 días laborables
                var expectedMonthlyHours = expectedWeeklyHours * 4.33; // Aproximado

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
                    today = new
                    {
                        date = today.ToString("dd/MM/yyyy"),
                        status = statusToday,
                        hoursWorked = Math.Round(hoursToday, 2),
                        expectedHours = expectedDailyHours,
                        recordsCount = todayRecords.Count,
                        firstCheckIn = todayRecords.FirstOrDefault(tr => tr.Type == RecordType.CheckIn)?.Timestamp.ToString("HH:mm"),
                        lastCheckOut = todayRecords.LastOrDefault(tr => tr.Type == RecordType.CheckOut)?.Timestamp.ToString("HH:mm")
                    },
                    thisWeek = new
                    {
                        hoursWorked = Math.Round(hoursThisWeek, 2),
                        expectedHours = expectedWeeklyHours,
                        daysWorked = daysWorkedThisWeek,
                        expectedDays = 5,
                        efficiency = Math.Round((hoursThisWeek / expectedWeeklyHours) * 100, 1)
                    },
                    thisMonth = new
                    {
                        hoursWorked = Math.Round(hoursThisMonth, 2),
                        expectedHours = Math.Round(expectedMonthlyHours, 2),
                        daysWorked = daysWorkedThisMonth,
                        efficiency = Math.Round((hoursThisMonth / expectedMonthlyHours) * 100, 1)
                    },
                    vacations = new
                    {
                        pendingRequests = pendingVacations,
                        upcomingVacations = upcomingVacations
                    },
                    quickActions = new[]
                    {
                        new { action = "check-in", label = "Fichar Entrada", available = statusToday != "Trabajando" },
                        new { action = "check-out", label = "Fichar Salida", available = statusToday == "Trabajando" },
                        new { action = "break", label = "Iniciar Descanso", available = statusToday == "Trabajando" },
                        new { action = "vacation", label = "Solicitar Vacaciones", available = true }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo resumen del dashboard" });
            }
        }

        [HttpGet("weekly-chart")]
        public async Task<ActionResult> GetWeeklyChart()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var today = DateTime.UtcNow.Date;
                var weekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Lunes

                var weekData = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var date = weekStart.AddDays(i);
                    var dayRecords = await _context.TimeRecords
                        .Where(tr => tr.EmployeeId == employee.Id && 
                                    tr.Timestamp.Date == date)
                        .OrderBy(tr => tr.Timestamp)
                        .ToListAsync();

                    var hoursWorked = CalculateHoursWorked(dayRecords);
                    var dayName = date.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));

                    weekData.Add(new
                    {
                        date = date.ToString("dd/MM"),
                        dayName = dayName,
                        hoursWorked = Math.Round(hoursWorked, 2),
                        expectedHours = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday ? 0 : 8,
                        hasRecords = dayRecords.Any(),
                        checkIn = dayRecords.FirstOrDefault(tr => tr.Type == RecordType.CheckIn)?.Timestamp.ToString("HH:mm"),
                        checkOut = dayRecords.LastOrDefault(tr => tr.Type == RecordType.CheckOut)?.Timestamp.ToString("HH:mm")
                    });
                }

                return Ok(new
                {
                    success = true,
                    weekData = weekData,
                    weekStart = weekStart.ToString("dd/MM/yyyy"),
                    weekEnd = weekStart.AddDays(6).ToString("dd/MM/yyyy")
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo datos semanales" });
            }
        }

        private double CalculateHoursWorked(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            double totalHours = 0;
            Shared.Models.TimeTracking.TimeRecord? lastCheckIn = null;

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

        private double CalculateWeeklyHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var groupedByDay = records.GroupBy(r => r.Timestamp.Date);
            double totalHours = 0;

            foreach (var dayGroup in groupedByDay)
            {
                totalHours += CalculateHoursWorked(dayGroup.OrderBy(r => r.Timestamp).ToList());
            }

            return totalHours;
        }

        private string GetCurrentStatus(Shared.Models.TimeTracking.TimeRecord? lastRecord)
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
    }
}