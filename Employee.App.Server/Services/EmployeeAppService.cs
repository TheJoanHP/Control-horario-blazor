using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Shared.Models.Vacations;
using Shared.Models.Enums;

namespace Employee.App.Server.Services
{
    public class EmployeeAppService : IEmployeeAppService
    {
        private readonly CompanyDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ILogger<EmployeeAppService> _logger;

        public EmployeeAppService(
            CompanyDbContext context,
            IEmployeeService employeeService,
            ITimeTrackingService timeTrackingService,
            ILogger<EmployeeAppService> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _timeTrackingService = timeTrackingService;
            _logger = logger;
        }

        public async Task<object> GetEmployeeDashboardAsync(int employeeId)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
            if (employee == null) throw new ArgumentException("Empleado no encontrado");

            var today = DateTime.Today;
            var isCheckedIn = await _timeTrackingService.IsEmployeeCheckedInAsync(employeeId);
            var isOnBreak = await _timeTrackingService.IsEmployeeOnBreakAsync(employeeId);
            var status = await _timeTrackingService.GetEmployeeStatusAsync(employeeId);
            
            var todayHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId, today);
            var todayBreak = await _timeTrackingService.CalculateBreakTimeAsync(employeeId, today);
            
            // Horas de la semana
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekHours = TimeSpan.Zero;
            for (int i = 0; i < 7; i++)
            {
                var date = startOfWeek.AddDays(i);
                if (date <= today)
                {
                    var dailyHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId, date);
                    weekHours = weekHours.Add(dailyHours);
                }
            }

            // Próximas vacaciones
            var upcomingVacations = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId && 
                           vr.Status == VacationStatus.Approved && 
                           vr.StartDate > today)
                .OrderBy(vr => vr.StartDate)
                .Take(3)
                .Select(vr => new
                {
                    Id = vr.Id,
                    StartDate = vr.StartDate,
                    EndDate = vr.EndDate,
                    DaysRequested = vr.DaysRequested
                })
                .ToListAsync();

            return new
            {
                Employee = new
                {
                    Id = employee.Id,
                    Name = employee.FullName,
                    EmployeeCode = employee.EmployeeCode,
                    Department = employee.Department?.Name,
                    Company = employee.Company?.Name
                },
                Status = new
                {
                    IsCheckedIn = isCheckedIn,
                    IsOnBreak = isOnBreak,
                    CurrentStatus = status
                },
                Today = new
                {
                    WorkedHours = todayHours.ToString(@"hh\:mm"),
                    BreakTime = todayBreak.ToString(@"hh\:mm"),
                    Date = today.ToString("yyyy-MM-dd")
                },
                Week = new
                {
                    TotalHours = weekHours.ToString(@"hh\:mm"),
                    StartDate = startOfWeek.ToString("yyyy-MM-dd"),
                    EndDate = startOfWeek.AddDays(6).ToString("yyyy-MM-dd")
                },
                UpcomingVacations = upcomingVacations,
                LastUpdated = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<VacationRequest>> GetEmployeeVacationRequestsAsync(int employeeId)
        {
            return await _context.VacationRequests
                .Include(vr => vr.ReviewedBy)
                .Where(vr => vr.EmployeeId == employeeId)
                .OrderByDescending(vr => vr.CreatedAt)
                .ToListAsync();
        }

        public async Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments)
        {
            // Validar fechas
            if (startDate < DateTime.Today)
            {
                throw new InvalidOperationException("La fecha de inicio no puede ser en el pasado");
            }

            if (endDate <= startDate)
            {
                throw new InvalidOperationException("La fecha de fin debe ser posterior a la fecha de inicio");
            }

            // Calcular días laborables
            var daysRequested = CalculateWorkingDays(startDate, endDate);

            // Verificar si puede solicitar vacaciones
            if (!await CanRequestVacationAsync(employeeId, startDate, endDate))
            {
                throw new InvalidOperationException("No puedes solicitar vacaciones en estas fechas");
            }

            var vacationRequest = new VacationRequest
            {
                EmployeeId = employeeId,
                StartDate = startDate,
                EndDate = endDate,
                DaysRequested = daysRequested,
                Comments = comments,
                Status = VacationStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VacationRequests.Add(vacationRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Solicitud de vacaciones creada para empleado {EmployeeId}: {StartDate} - {EndDate}", 
                employeeId, startDate, endDate);

            return vacationRequest;
        }

        public async Task<object> GetVacationBalanceAsync(int employeeId)
        {
            var currentYear = DateTime.Now.Year;
            
            var balance = await _context.VacationBalances
                .FirstOrDefaultAsync(vb => vb.EmployeeId == employeeId && vb.Year == currentYear);

            if (balance == null)
            {
                // Crear balance por defecto si no existe
                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
                var company = employee?.Company;
                var defaultDays = company?.VacationDaysPerYear ?? 22;

                balance = new VacationBalance
                {
                    EmployeeId = employeeId,
                    Year = currentYear,
                    TotalDays = defaultDays,
                    UsedDays = 0,
                    PendingDays = 0,
                    CarriedOverDays = 0
                };

                _context.VacationBalances.Add(balance);
                await _context.SaveChangesAsync();
            }

            // Calcular días pendientes de solicitudes aprobadas
            var approvedRequests = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId && 
                           vr.Status == VacationStatus.Approved &&
                           vr.StartDate.Year == currentYear)
                .ToListAsync();

            var pendingRequests = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId && 
                           vr.Status == VacationStatus.Pending &&
                           vr.StartDate.Year == currentYear)
                .ToListAsync();

            var usedDays = approvedRequests.Sum(vr => vr.DaysRequested);
            var pendingDays = pendingRequests.Sum(vr => vr.DaysRequested);

            return new
            {
                Year = currentYear,
                TotalDays = balance.TotalDays,
                UsedDays = usedDays,
                PendingDays = pendingDays,
                AvailableDays = balance.TotalDays - usedDays - pendingDays,
                CarriedOverDays = balance.CarriedOverDays,
                Requests = new
                {
                    Approved = approvedRequests.Count,
                    Pending = pendingRequests.Count,
                    Rejected = await _context.VacationRequests
                        .CountAsync(vr => vr.EmployeeId == employeeId && 
                                    vr.Status == VacationStatus.Rejected &&
                                    vr.StartDate.Year == currentYear)
                }
            };
        }

        public async Task<bool> CanRequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            // Verificar si hay solicitudes conflictivas
            var conflictingRequests = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId &&
                           (vr.Status == VacationStatus.Approved || vr.Status == VacationStatus.Pending) &&
                           ((vr.StartDate <= startDate && vr.EndDate >= startDate) ||
                            (vr.StartDate <= endDate && vr.EndDate >= endDate) ||
                            (vr.StartDate >= startDate && vr.EndDate <= endDate)))
                .AnyAsync();

            if (conflictingRequests) return false;

            // Verificar balance de vacaciones
            var balance = await GetVacationBalanceAsync(employeeId);
            var balanceData = (dynamic)balance;
            var daysToRequest = CalculateWorkingDays(startDate, endDate);

            return balanceData.AvailableDays >= daysToRequest;
        }

        public async Task<object> GetEmployeeProfileAsync(int employeeId)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
            if (employee == null) throw new ArgumentException("Empleado no encontrado");

            return new
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = employee.Phone,
                EmployeeCode = employee.EmployeeCode,
                Department = employee.Department?.Name,
                Company = employee.Company?.Name,
                HiredAt = employee.HiredAt,
                WorkSchedule = new
                {
                    StartTime = employee.WorkStartTime.ToString(@"hh\:mm"),
                    EndTime = employee.WorkEndTime.ToString(@"hh\:mm")
                }
            };
        }

        public async Task<bool> UpdateEmployeeProfileAsync(int employeeId, string firstName, string lastName, string? phone)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) return false;

            employee.FirstName = firstName.Trim();
            employee.LastName = lastName.Trim();
            employee.Phone = phone?.Trim();
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Perfil actualizado para empleado {EmployeeId}", employeeId);
            return true;
        }

        private static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            var current = startDate.Date;

            while (current <= endDate.Date)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }
    }
}