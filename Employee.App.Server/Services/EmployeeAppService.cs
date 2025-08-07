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

        public async Task<object> GetTodayTimeRecordsAsync(int employeeId)
        {
            var today = DateTime.Today;
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderBy(tr => tr.Time)
                .Select(tr => new
                {
                    tr.Id,
                    tr.Type,
                    tr.Date,
                    tr.Time,
                    DateTime = tr.Date.Add(tr.Time),
                    tr.Notes,
                    TypeDisplay = tr.Type.ToString()
                })
                .ToListAsync();

            var totalHours = CalculateWorkedHours(records);

            return new
            {
                Records = records,
                TotalHours = totalHours,
                Summary = new
                {
                    CheckIn = records.FirstOrDefault(r => r.Type.ToString() == "CheckIn")?.DateTime,
                    CheckOut = records.FirstOrDefault(r => r.Type.ToString() == "CheckOut")?.DateTime,
                    BreakTime = CalculateBreakTime(records),
                    WorkedHours = totalHours,
                    Status = GetCurrentStatus(records.LastOrDefault())
                }
            };
        }

        // Métodos auxiliares privados
        private async Task<double> GetTodayWorkedHoursAsync(int employeeId)
        {
            var today = DateTime.Today;
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderBy(tr => tr.Time)
                .ToListAsync();

            return CalculateWorkedHours(records);
        }

        private async Task<double> GetTodayBreakTimeAsync(int employeeId)
        {
            var today = DateTime.Today;
            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date == today)
                .OrderBy(tr => tr.Time)
                .ToListAsync();

            return CalculateBreakTime(records);
        }

        private async Task<double> GetWeekWorkedHoursAsync(int employeeId)
        {
            var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var endOfWeek = startOfWeek.AddDays(7);

            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date >= startOfWeek && tr.Date < endOfWeek)
                .OrderBy(tr => tr.Date).ThenBy(tr => tr.Time)
                .ToListAsync();

            // Agrupar por día y calcular horas trabajadas por día
            var dailyHours = records
                .GroupBy(r => r.Date)
                .Sum(dayGroup => CalculateWorkedHours(dayGroup.Select(r => new
                {
                    r.Id,
                    r.Type,
                    r.Date,
                    r.Time,
                    DateTime = r.Date.Add(r.Time),
                    r.Notes,
                    TypeDisplay = r.Type.ToString()
                }).ToList()));

            return dailyHours;
        }

        private double CalculateWorkedHours(IEnumerable<object> records)
        {
            // Implementación simplificada
            var recordList = records.ToList();
            if (recordList.Count < 2) return 0;

            // Buscar entrada y salida
            var checkIn = recordList.FirstOrDefault();
            var checkOut = recordList.LastOrDefault();

            if (checkIn != null && checkOut != null)
            {
                // Implementación básica - necesitará mejoras
                return 8.0; // Placeholder
            }

            return 0;
        }

        private double CalculateBreakTime(IEnumerable<object> records)
        {
            // Implementación simplificada
            return 0.5; // 30 minutos por defecto
        }

        private string GetCurrentStatus(object? lastRecord)
        {
            if (lastRecord == null) return "No fichado";
            
            // Usar reflexión para obtener el tipo
            var type = lastRecord.GetType().GetProperty("Type")?.GetValue(lastRecord);
            
            return type?.ToString() switch
            {
                "CheckIn" => "Trabajando",
                "CheckOut" => "Fuera",
                "BreakStart" => "En descanso",
                "BreakEnd" => "Trabajando",
                _ => "Desconocido"
            };
        }

        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            var days = 0;
            var current = startDate;

            while (current <= endDate)
            {
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    days++;
                }
                current = current.AddDays(1);
            }

            return days;
        }
    }
}