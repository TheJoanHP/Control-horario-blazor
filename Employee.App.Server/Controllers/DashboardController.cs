using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Employee.App.Server.Data; // Corregido namespace
using Shared.Models.Enums;

namespace Employee.App.Server.Controllers // Corregido namespace
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
                    .GroupBy(tr => tr.Date)
                    .Count();

                // Solicitudes de vacaciones próximas
                var upcomingVacations = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id 
                                && vr.Status == VacationStatus.Approved 
                                && vr.StartDate >= today)
                    .OrderBy(vr => vr.StartDate)
                    .Take(3)
                    .Select(vr => new
                    {
                        vr.Id,
                        vr.StartDate,
                        vr.EndDate,
                        vr.TotalDays,
                        vr.Reason
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Employee = new
                    {
                        employee.Id,
                        FullName = $"{employee.FirstName} {employee.LastName}",
                        employee.EmployeeCode,
                        Position = employee.Position,
                        Department = employee.Department?.Name,
                        Company = employee.Company?.Name
                    },
                    Today = new
                    {
                        Date = today.ToString("yyyy-MM-dd"),
                        HoursWorked = hoursToday.ToString(@"hh\:mm"),
                        Status = statusToday
                    },
                    Week = new
                    {
                        StartDate = thisWeekStart.ToString("yyyy-MM-dd"),
                        TotalHours = hoursThisWeek.ToString(@"hh\:mm"),
                        DaysWorked = daysWorkedThisWeek
                    },
                    UpcomingVacations = upcomingVacations
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        private TimeSpan CalculateHoursWorked(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var totalHours = TimeSpan.Zero;
            Shared.Models.TimeTracking.TimeRecord? checkIn = null;

            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                if (record.Type == RecordType.CheckIn)
                {
                    checkIn = record;
                }
                else if (record.Type == RecordType.CheckOut && checkIn != null)
                {
                    totalHours = totalHours.Add(record.Timestamp - checkIn.Timestamp);
                    checkIn = null;
                }
            }

            return totalHours;
        }

        private TimeSpan CalculateWeeklyHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var dailyHours = records
                .GroupBy(r => r.Date)
                .Select(g => CalculateHoursWorked(g.ToList()))
                .Sum(h => h.TotalHours);

            return TimeSpan.FromHours(dailyHours);
        }

        private string GetCurrentStatus(Shared.Models.TimeTracking.TimeRecord? lastRecord)
        {
            if (lastRecord == null) return "Sin registros";

            return lastRecord.Type switch
            {
                RecordType.CheckIn => "Trabajando",
                RecordType.CheckOut => "Fuera del trabajo",
                RecordType.BreakStart => "En descanso",
                RecordType.BreakEnd => "Trabajando",
                _ => "Estado desconocido"
            };
        }
    }
}