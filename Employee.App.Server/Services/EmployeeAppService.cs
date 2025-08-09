using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Company.Admin.Server.Services;
using Shared.Models.Vacations;
using Shared.Models.Enums;
using System.Globalization;

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

            // Horas trabajadas hoy
            var todayHours = await GetTodayWorkedHoursAsync(employeeId);
            var todayBreak = await GetTodayBreakTimeAsync(employeeId);
            
            // Horas de la semana
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
            var weekHours = await GetWeekWorkedHoursAsync(employeeId);

            // Próximas vacaciones
            var upcomingVacations = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId && 
                           vr.Status == VacationStatus.Approved && 
                           vr.StartDate > today)
                .OrderBy(vr => vr.StartDate)
                .Take(3)
                .Select(vr => new
                {
                    vr.Id,
                    vr.StartDate,
                    vr.EndDate,
                    vr.TotalDays
                })
                .ToListAsync();

            return new
            {
                Employee = new
                {
                    employee.Id,
                    Name = employee.FullName,
                    employee.EmployeeCode,
                    Department = employee.Department?.Name,
                    Company = employee.Company?.Name
                },
                Status = new
                {
                    IsCheckedIn = isCheckedIn,
                    IsOnBreak = isOnBreak,
                    CurrentStatus = status.ToString()
                },
                Today = new
                {
                    WorkedHours = todayHours,
                    BreakTime = todayBreak,
                    Date = today.ToString("yyyy-MM-dd")
                },
                Week = new
                {
                    TotalHours = weekHours,
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
                .Where(vr => vr.EmployeeId == employeeId)
                .OrderByDescending(vr => vr.CreatedAt)
                .ToListAsync();
        }

        public async Task<VacationRequest> CreateVacationRequestAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments)
        {
            return await RequestVacationAsync(employeeId, startDate, endDate, comments);
        }

        public async Task<VacationRequest> RequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate, string? comments)
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
                TotalDays = daysRequested,
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
                // CORRECCIÓN 1: Obtener la empresa desde el contexto directo
                var employee = await _context.Employees
                    .Include(e => e.Company)
                    .FirstOrDefaultAsync(e => e.Id == employeeId);
                
                // CORRECCIÓN 2: Usar la propiedad correcta del modelo Company
                var defaultDays = employee?.Company?.VacationDaysPerYear ?? 22;

                balance = new VacationBalance
                {
                    EmployeeId = employeeId,
                    Year = currentYear,
                    TotalDays = defaultDays,
                    UsedDays = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VacationBalances.Add(balance);
                await _context.SaveChangesAsync();
            }

            return new
            {
                balance.Year,
                balance.TotalDays,
                balance.UsedDays,
                balance.RemainingDays,
                balance.PendingDays
            };
        }

        public async Task<bool> CanRequestVacationAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            // Verificar si ya tiene solicitudes en esas fechas
            var overlappingRequests = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId
                           && vr.Status != VacationStatus.Rejected
                           && ((vr.StartDate <= startDate && vr.EndDate >= startDate)
                               || (vr.StartDate <= endDate && vr.EndDate >= endDate)
                               || (vr.StartDate >= startDate && vr.EndDate <= endDate)))
                .AnyAsync();

            return !overlappingRequests;
        }

        public async Task<object> GetEmployeeProfileAsync(int employeeId)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
            if (employee == null) throw new ArgumentException("Empleado no encontrado");

            return new
            {
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.Phone,
                employee.Position,
                employee.EmployeeCode,
                Department = employee.Department?.Name,
                Company = employee.Company?.Name,
                employee.HireDate,
                employee.Active
            };
        }

        public async Task<bool> UpdateEmployeeProfileAsync(int employeeId, string firstName, string lastName, string? phone)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(employeeId);
                if (employee == null) return false;

                employee.FirstName = firstName;
                employee.LastName = lastName;
                employee.Phone = phone;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando perfil del empleado {EmployeeId}", employeeId);
                return false;
            }
        }

        // MÉTODO FALTANTE 1: GetRecentVacationRequestsAsync
        public async Task<List<object>> GetRecentVacationRequestsAsync(int employeeId)
        {
            var requests = await _context.VacationRequests
                .Where(vr => vr.EmployeeId == employeeId)
                .OrderByDescending(vr => vr.CreatedAt)
                .Take(10)
                .Select(vr => new
                {
                    vr.Id,
                    vr.StartDate,
                    vr.EndDate,
                    vr.TotalDays,
                    vr.Status,
                    vr.Comments,
                    vr.CreatedAt,
                    StatusText = vr.Status.ToString()
                })
                .ToListAsync();

            return requests.Cast<object>().ToList();
        }

        // MÉTODO FALTANTE 2: GetTodayTimeRecordsAsync
        public async Task<object> GetTodayTimeRecordsAsync(int employeeId)
        {
            var today = DateTime.Today;
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderBy(tr => tr.Timestamp)
                .Select(tr => new
                {
                    tr.Id,
                    tr.Type,
                    tr.Timestamp,
                    tr.Date,
                    tr.Time,
                    // CORRECCIÓN 3: Usar Notes en lugar de Comments
                    Notes = tr.Notes,
                    TypeText = tr.Type.ToString()
                })
                .ToListAsync();

            var summary = new
            {
                Date = today.ToString("yyyy-MM-dd"),
                Records = records,
                TotalRecords = records.Count,
                WorkedHours = CalculateWorkedHours(await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                    .ToListAsync()).ToString(@"hh\:mm"),
                LastRecord = records.LastOrDefault()
            };

            return summary;
        }

        // Métodos auxiliares privados
        private async Task<TimeSpan> GetTodayWorkedHoursAsync(int employeeId)
        {
            var today = DateTime.Today;
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderBy(tr => tr.Timestamp)
                .ToListAsync();

            return CalculateWorkedHours(records);
        }

        private async Task<TimeSpan> GetTodayBreakTimeAsync(int employeeId)
        {
            var today = DateTime.Today;
            // CORRECCIÓN 4: Usar StartTime en lugar de StartTime.Date
            var breaks = await _context.Breaks
                .Where(b => b.EmployeeId == employeeId && b.StartTime >= today && b.StartTime < today.AddDays(1))
                .ToListAsync();

            return TimeSpan.FromMinutes(breaks.Sum(b => b.DurationMinutes));
        }

        private async Task<TimeSpan> GetWeekWorkedHoursAsync(int employeeId)
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var endOfWeek = startOfWeek.AddDays(6);

            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && 
                           tr.Date >= startOfWeek && 
                           tr.Date <= endOfWeek)
                .OrderBy(tr => tr.Timestamp)
                .ToListAsync();

            var dailyHours = records
                .GroupBy(r => r.Date)
                .Select(g => CalculateWorkedHours(g.ToList()))
                .Sum(h => h.TotalHours);

            return TimeSpan.FromHours(dailyHours);
        }

        private static TimeSpan CalculateWorkedHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var totalHours = TimeSpan.Zero;
            Shared.Models.TimeTracking.TimeRecord? checkIn = null;

            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                if (record.Type == Shared.Models.Enums.RecordType.CheckIn)
                {
                    checkIn = record;
                }
                else if (record.Type == Shared.Models.Enums.RecordType.CheckOut && checkIn != null)
                {
                    totalHours = totalHours.Add(record.Timestamp - checkIn.Timestamp);
                    checkIn = null;
                }
            }

            return totalHours;
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