using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeApp.Server.Data;
using Shared.Models.Vacations;
using Shared.Models.Enums;

namespace EmployeeApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VacationsController : ControllerBase
    {
        private readonly EmployeeDbContext _context;

        public VacationsController(EmployeeDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-requests")]
        public async Task<ActionResult> GetMyVacationRequests()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                var requests = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id)
                    .OrderByDescending(vr => vr.CreatedAt)
                    .Select(vr => new
                    {
                        vr.Id,
                        vr.StartDate,
                        vr.EndDate,
                        vr.DaysRequested,
                        vr.Reason,
                        vr.Status,
                        vr.AdminNotes,
                        vr.ApprovedAt,
                        vr.CreatedAt,
                        StatusName = vr.Status.ToString(),
                        FormattedStartDate = vr.StartDate.ToString("dd/MM/yyyy"),
                        FormattedEndDate = vr.EndDate.ToString("dd/MM/yyyy")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    requests = requests,
                    total = requests.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo solicitudes de vacaciones" });
            }
        }

        [HttpPost("request")]
        public async Task<ActionResult> RequestVacation([FromBody] VacationRequestDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Validaciones
                if (request.StartDate < DateTime.UtcNow.Date)
                {
                    return BadRequest(new { message = "La fecha de inicio no puede ser anterior a hoy" });
                }

                if (request.EndDate <= request.StartDate)
                {
                    return BadRequest(new { message = "La fecha de fin debe ser posterior a la fecha de inicio" });
                }

                // Calcular días solicitados (excluyendo fines de semana)
                var daysRequested = CalculateWorkDays(request.StartDate, request.EndDate);

                // Verificar que no haya solapamiento con otras solicitudes aprobadas
                var overlapping = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id &&
                                vr.Status == VacationStatus.Approved &&
                                ((vr.StartDate <= request.EndDate && vr.EndDate >= request.StartDate)))
                    .AnyAsync();

                if (overlapping)
                {
                    return BadRequest(new { message = "Ya tienes vacaciones aprobadas que se solapan con estas fechas" });
                }

                var vacationRequest = new VacationRequest
                {
                    EmployeeId = employee.Id,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    DaysRequested = daysRequested,
                    Reason = request.Reason,
                    Status = VacationStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VacationRequests.Add(vacationRequest);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Solicitud de vacaciones enviada correctamente",
                    request = new
                    {
                        vacationRequest.Id,
                        vacationRequest.StartDate,
                        vacationRequest.EndDate,
                        vacationRequest.DaysRequested,
                        vacationRequest.Reason,
                        vacationRequest.Status,
                        StatusName = vacationRequest.Status.ToString(),
                        FormattedStartDate = vacationRequest.StartDate.ToString("dd/MM/yyyy"),
                        FormattedEndDate = vacationRequest.EndDate.ToString("dd/MM/yyyy")
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error enviando solicitud de vacaciones" });
            }
        }

        [HttpGet("balance")]
        public async Task<ActionResult> GetVacationBalance()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                
                var employee = await _context.Employees
                    .Include(e => e.User)
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return BadRequest(new { message = "Empleado no encontrado" });

                // Calcular días de vacaciones según antigüedad (simplificado)
                var yearsWorked = (DateTime.UtcNow - employee.HireDate).TotalDays / 365.25;
                var totalVacationDays = Math.Min(30, Math.Max(21, (int)(21 + yearsWorked))); // Min 21, Max 30

                // Calcular días usados este año
                var currentYear = DateTime.UtcNow.Year;
                var usedDays = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id &&
                                vr.Status == VacationStatus.Approved &&
                                vr.StartDate.Year == currentYear)
                    .SumAsync(vr => vr.DaysRequested);

                // Días pendientes de aprobación
                var pendingDays = await _context.VacationRequests
                    .Where(vr => vr.EmployeeId == employee.Id &&
                                vr.Status == VacationStatus.Pending &&
                                vr.StartDate.Year == currentYear)
                    .SumAsync(vr => vr.DaysRequested);

                var availableDays = totalVacationDays - usedDays;

                return Ok(new
                {
                    success = true,
                    balance = new
                    {
                        totalDays = totalVacationDays,
                        usedDays = usedDays,
                        pendingDays = pendingDays,
                        availableDays = availableDays,
                        year = currentYear,
                        yearsWorked = Math.Round(yearsWorked, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error obteniendo balance de vacaciones" });
            }
        }

        private int CalculateWorkDays(DateTime startDate, DateTime endDate)
        {
            int workDays = 0;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && 
                    currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    workDays++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return workDays;
        }
    }

    public class VacationRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Reason { get; set; }
    }
}