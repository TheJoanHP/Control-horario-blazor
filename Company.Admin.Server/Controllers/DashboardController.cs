using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Company.Admin.Server.Services;
using Company.Admin.Server.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Services.Utils;

namespace Company.Admin.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly CompanyDbContext _context;
        private readonly IEmployeeService _employeeService;
        private readonly IDepartmentService _departmentService;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly IReportService _reportService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            CompanyDbContext context,
            IEmployeeService employeeService,
            IDepartmentService departmentService,
            ITimeTrackingService timeTrackingService,
            IReportService reportService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _employeeService = employeeService;
            _departmentService = departmentService;
            _timeTrackingService = timeTrackingService;
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener estadísticas generales del dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetDashboardStats()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeek = DateTimeService.GetStartOfWeek(today);
                var thisMonth = DateTimeService.GetStartOfMonth(today);

                // Estadísticas básicas
                var totalEmployees = await _employeeService.GetTotalEmployeesAsync();
                var activeEmployees = await _employeeService.GetActiveEmployeesAsync();
                var totalDepartments = (await _departmentService.GetActiveDepartmentsAsync()).Count();

                // Empleados presentes hoy
                var employeesCheckedInToday = await GetEmployeesCheckedInTodayAsync();
                var employeesOnBreak = await GetEmployeesOnBreakAsync();

                // Estadísticas de asistencia
                var attendanceStats = await GetAttendanceStatsAsync(today);
                var weeklyStats = await GetWeeklyStatsAsync(thisWeek, today);
                var monthlyStats = await GetMonthlyStatsAsync(thisMonth, today);

                return Ok(new
                {
                    overview = new
                    {
                        totalEmployees,
                        activeEmployees,
                        totalDepartments,
                        employeesPresent = employeesCheckedInToday,
                        employeesOnBreak
                    },
                    today = attendanceStats,
                    thisWeek = weeklyStats,
                    thisMonth = monthlyStats,
                    lastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas del dashboard");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener empleados actualmente en la oficina
        /// </summary>
        [HttpGet("employees-present")]
        public async Task<ActionResult> GetEmployeesPresent()
        {
            try
            {
                var employees = await _context.Employees
                    .Include(e => e.Department)
                    .Where(e => e.Active)
                    .ToListAsync();

                var employeesPresent = new List<object>();

                foreach (var employee in employees)
                {
                    var isCheckedIn = await _timeTrackingService.IsEmployeeCheckedInAsync(employee.Id);
                    if (isCheckedIn)
                    {
                        var status = await _timeTrackingService.GetEmployeeStatusAsync(employee.Id);
                        var lastRecord = await _timeTrackingService.GetLastRecordAsync(employee.Id);

                        employeesPresent.Add(new
                        {
                            id = employee.Id,
                            name = employee.FullName,
                            department = employee.Department?.Name,
                            status,
                            checkedInAt = lastRecord?.Timestamp,
                            email = employee.Email
                        });
                    }
                }

                return Ok(employeesPresent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados presentes");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener actividad reciente
        /// </summary>
        [HttpGet("recent-activity")]
        public async Task<ActionResult> GetRecentActivity([FromQuery] int limit = 20)
        {
            try
            {
                var today = DateTime.Today;
                var recentRecords = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Timestamp >= today)
                    .OrderByDescending(tr => tr.Timestamp)
                    .Take(limit)
                    .Select(tr => new
                    {
                        id = tr.Id,
                        employeeName = tr.Employee.FullName,
                        employeeDepartment = tr.Employee.Department != null ? tr.Employee.Department.Name : "Sin departamento",
                        type = tr.Type.ToString(),
                        timestamp = tr.Timestamp,
                        notes = tr.Notes
                    })
                    .ToListAsync();

                return Ok(recentRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener actividad reciente");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener estadísticas por departamento
        /// </summary>
        [HttpGet("departments-stats")]
        public async Task<ActionResult> GetDepartmentsStats()
        {
            try
            {
                var departments = await _context.Departments
                    .Include(d => d.Employees.Where(e => e.Active))
                    .Where(d => d.Active)
                    .ToListAsync();

                var departmentStats = new List<object>();

                foreach (var department in departments)
                {
                    var totalEmployees = department.Employees.Count;
                    var presentEmployees = 0;

                    foreach (var employee in department.Employees)
                    {
                        if (await _timeTrackingService.IsEmployeeCheckedInAsync(employee.Id))
                        {
                            presentEmployees++;
                        }
                    }

                    var attendanceRate = totalEmployees > 0 ? (double)presentEmployees / totalEmployees * 100 : 0;

                    departmentStats.Add(new
                    {
                        id = department.Id,
                        name = department.Name,
                        totalEmployees,
                        presentEmployees,
                        attendanceRate = Math.Round(attendanceRate, 2)
                    });
                }

                return Ok(departmentStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas por departamento");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Obtener gráfico de asistencia semanal
        /// </summary>
        [HttpGet("weekly-attendance-chart")]
        public async Task<ActionResult> GetWeeklyAttendanceChart()
        {
            try
            {
                var startOfWeek = DateTimeService.GetStartOfWeek(DateTime.Today);
                var chartData = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var date = startOfWeek.AddDays(i);
                    var dayName = date.ToString("dddd");
                    
                    if (date <= DateTime.Today)
                    {
                        var attendanceCount = await GetDayAttendanceCountAsync(date);
                        chartData.Add(new
                        {
                            day = dayName,
                            date = date.ToString("yyyy-MM-dd"),
                            attendance = attendanceCount
                        });
                    }
                    else
                    {
                        chartData.Add(new
                        {
                            day = dayName,
                            date = date.ToString("yyyy-MM-dd"),
                            attendance = 0
                        });
                    }
                }

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener gráfico de asistencia semanal");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        // Métodos auxiliares privados
        private async Task<int> GetEmployeesCheckedInTodayAsync()
        {
            var employees = await _context.Employees.Where(e => e.Active).ToListAsync();
            int count = 0;

            foreach (var employee in employees)
            {
                if (await _timeTrackingService.IsEmployeeCheckedInAsync(employee.Id))
                {
                    count++;
                }
            }

            return count;
        }

        private async Task<int> GetEmployeesOnBreakAsync()
        {
            var employees = await _context.Employees.Where(e => e.Active).ToListAsync();
            int count = 0;

            foreach (var employee in employees)
            {
                if (await _timeTrackingService.IsEmployeeOnBreakAsync(employee.Id))
                {
                    count++;
                }
            }

            return count;
        }

        private async Task<object> GetAttendanceStatsAsync(DateTime date)
        {
            var totalEmployees = await _employeeService.GetActiveEmployeesAsync();
            var presentEmployees = await GetEmployeesCheckedInTodayAsync();
            var attendanceRate = totalEmployees > 0 ? (double)presentEmployees / totalEmployees * 100 : 0;

            return new
            {
                date = date.ToString("yyyy-MM-dd"),
                totalEmployees,
                presentEmployees,
                attendanceRate = Math.Round(attendanceRate, 2)
            };
        }

        private async Task<object> GetWeeklyStatsAsync(DateTime startOfWeek, DateTime endDate)
        {
            var totalDays = (endDate - startOfWeek).Days + 1;
            var totalEmployees = await _employeeService.GetActiveEmployeesAsync();
            
            // Aquí podrías calcular estadísticas más detalladas de la semana
            // Por simplicidad, usamos estadísticas básicas
            
            return new
            {
                startDate = startOfWeek.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                totalWorkingDays = DateTimeService.GetWorkingDays(startOfWeek, endDate),
                averageAttendance = 0 // Implementar cálculo real
            };
        }

        private async Task<object> GetMonthlyStatsAsync(DateTime startOfMonth, DateTime endDate)
        {
            var totalEmployees = await _employeeService.GetActiveEmployeesAsync();
            
            return new
            {
                startDate = startOfMonth.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                totalWorkingDays = DateTimeService.GetWorkingDays(startOfMonth, endDate),
                averageAttendance = 0 // Implementar cálculo real
            };
        }

        private async Task<int> GetDayAttendanceCountAsync(DateTime date)
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var attendanceCount = await _context.TimeRecords
                .Where(tr => tr.Type == Shared.Models.Enums.RecordType.CheckIn &&
                           tr.Timestamp >= startOfDay &&
                           tr.Timestamp <= endOfDay)
                .Select(tr => tr.EmployeeId)
                .Distinct()
                .CountAsync();

            return attendanceCount;
        }
    }
}