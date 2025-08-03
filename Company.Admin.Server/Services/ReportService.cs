using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public interface IReportService
    {
        Task<AttendanceReportDto> GenerateAttendanceReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null);
        Task<HoursReportDto> GenerateHoursReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null);
        Task<OvertimeReportDto> GenerateOvertimeReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null);
        Task<SummaryReportDto> GenerateSummaryReportAsync(DateTime startDate, DateTime endDate, int? departmentId = null);
        Task<byte[]> ExportAttendanceReportToCsvAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null);
        Task<byte[]> ExportHoursReportToCsvAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null);
        Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    public class ReportService : IReportService
    {
        private readonly CompanyDbContext _context;
        private readonly ILogger<ReportService> _logger;
        private const double StandardWorkingHours = 8.0; // Horas estándar por día

        public ReportService(CompanyDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AttendanceReportDto> GenerateAttendanceReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= startDate.Date && 
                               tr.Date <= endDate.Date &&
                               tr.RecordType == RecordType.CheckIn)
                    .AsQueryable();

                if (employeeId.HasValue)
                    query = query.Where(tr => tr.EmployeeId == employeeId);

                if (departmentId.HasValue)
                    query = query.Where(tr => tr.Employee.DepartmentId == departmentId);

                var records = await query.ToListAsync();

                var attendanceData = records
                    .GroupBy(r => new { r.EmployeeId, r.Employee.FirstName, r.Employee.LastName, r.Employee.EmployeeCode, DepartmentName = r.Employee.Department?.Name })
                    .Select(g => new AttendanceRecordDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                        EmployeeCode = g.Key.EmployeeCode,
                        DepartmentName = g.Key.DepartmentName ?? "Sin Departamento",
                        TotalDays = g.Select(r => r.Date).Distinct().Count(),
                        DaysPresent = g.Count(r => r.CheckIn != null),
                        DaysAbsent = GetWorkingDaysInPeriod(startDate, endDate) - g.Select(r => r.Date).Distinct().Count(),
                        AttendancePercentage = CalculateAttendancePercentage(g.Select(r => r.Date).Distinct().Count(), startDate, endDate),
                        AverageArrivalTime = CalculateAverageArrivalTime(g.Where(r => r.CheckIn != null).Select(r => r.CheckIn!.Value)),
                        TotalHours = g.Sum(r => r.TotalHours ?? 0)
                    })
                    .OrderBy(a => a.EmployeeName)
                    .ToList();

                return new AttendanceReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    TotalEmployees = attendanceData.Count,
                    AverageAttendance = attendanceData.Any() ? attendanceData.Average(a => a.AttendancePercentage) : 0,
                    AttendanceRecords = attendanceData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de asistencia");
                throw;
            }
        }

        public async Task<HoursReportDto> GenerateHoursReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= startDate.Date && 
                               tr.Date <= endDate.Date &&
                               tr.RecordType == RecordType.CheckIn)
                    .AsQueryable();

                if (employeeId.HasValue)
                    query = query.Where(tr => tr.EmployeeId == employeeId);

                if (departmentId.HasValue)
                    query = query.Where(tr => tr.Employee.DepartmentId == departmentId);

                var records = await query.ToListAsync();

                var hoursData = records
                    .GroupBy(r => new { r.EmployeeId, r.Employee.FirstName, r.Employee.LastName, r.Employee.EmployeeCode, DepartmentName = r.Employee.Department?.Name })
                    .Select(g => new HoursRecordDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                        EmployeeCode = g.Key.EmployeeCode,
                        DepartmentName = g.Key.DepartmentName ?? "Sin Departamento",
                        TotalHours = g.Sum(r => r.TotalHours ?? 0),
                        RegularHours = CalculateRegularHours(g.Sum(r => r.TotalHours ?? 0), GetWorkingDaysInPeriod(startDate, endDate)),
                        OvertimeHours = CalculateOvertimeHours(g.Sum(r => r.TotalHours ?? 0), GetWorkingDaysInPeriod(startDate, endDate)),
                        AverageHoursPerDay = g.Any() ? g.Sum(r => r.TotalHours ?? 0) / g.Select(r => r.Date).Distinct().Count() : 0,
                        DaysWorked = g.Select(r => r.Date).Distinct().Count()
                    })
                    .OrderBy(h => h.EmployeeName)
                    .ToList();

                return new HoursReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    TotalEmployees = hoursData.Count,
                    TotalHours = hoursData.Sum(h => h.TotalHours),
                    TotalRegularHours = hoursData.Sum(h => h.RegularHours),
                    TotalOvertimeHours = hoursData.Sum(h => h.OvertimeHours),
                    AverageHoursPerEmployee = hoursData.Any() ? hoursData.Average(h => h.TotalHours) : 0,
                    HoursRecords = hoursData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de horas");
                throw;
            }
        }

        public async Task<OvertimeReportDto> GenerateOvertimeReportAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= startDate.Date && 
                               tr.Date <= endDate.Date &&
                               tr.RecordType == RecordType.CheckIn)
                    .AsQueryable();

                if (employeeId.HasValue)
                    query = query.Where(tr => tr.EmployeeId == employeeId);

                if (departmentId.HasValue)
                    query = query.Where(tr => tr.Employee.DepartmentId == departmentId);

                var records = await query.ToListAsync();

                var overtimeData = records
                    .GroupBy(r => new { r.EmployeeId, r.Employee.FirstName, r.Employee.LastName, r.Employee.EmployeeCode, DepartmentName = r.Employee.Department?.Name })
                    .Where(g => CalculateOvertimeHours(g.Sum(r => r.TotalHours ?? 0), GetWorkingDaysInPeriod(startDate, endDate)) > 0)
                    .Select(g => new OvertimeRecordDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = $"{g.Key.FirstName} {g.Key.LastName}",
                        EmployeeCode = g.Key.EmployeeCode,
                        DepartmentName = g.Key.DepartmentName ?? "Sin Departamento",
                        TotalHours = g.Sum(r => r.TotalHours ?? 0),
                        RegularHours = CalculateRegularHours(g.Sum(r => r.TotalHours ?? 0), GetWorkingDaysInPeriod(startDate, endDate)),
                        OvertimeHours = CalculateOvertimeHours(g.Sum(r => r.TotalHours ?? 0), GetWorkingDaysInPeriod(startDate, endDate)),
                        DaysWithOvertime = g.Count(r => (r.TotalHours ?? 0) > StandardWorkingHours),
                        MaxDailyOvertime = g.Max(r => Math.Max(0, (r.TotalHours ?? 0) - StandardWorkingHours))
                    })
                    .OrderByDescending(o => o.OvertimeHours)
                    .ToList();

                return new OvertimeReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    TotalEmployeesWithOvertime = overtimeData.Count,
                    TotalOvertimeHours = overtimeData.Sum(o => o.OvertimeHours),
                    AverageOvertimePerEmployee = overtimeData.Any() ? overtimeData.Average(o => o.OvertimeHours) : 0,
                    OvertimeRecords = overtimeData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de horas extra");
                throw;
            }
        }

        public async Task<SummaryReportDto> GenerateSummaryReportAsync(DateTime startDate, DateTime endDate, int? departmentId = null)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= startDate.Date && 
                               tr.Date <= endDate.Date &&
                               tr.RecordType == RecordType.CheckIn)
                    .AsQueryable();

                if (departmentId.HasValue)
                    query = query.Where(tr => tr.Employee.DepartmentId == departmentId);

                var records = await query.ToListAsync();
                var workingDays = GetWorkingDaysInPeriod(startDate, endDate);

                var departmentSummaries = records
                    .GroupBy(r => new { r.Employee.DepartmentId, DepartmentName = r.Employee.Department?.Name ?? "Sin Departamento" })
                    .Select(g => new DepartmentSummaryDto
                    {
                        DepartmentId = g.Key.DepartmentId,
                        DepartmentName = g.Key.DepartmentName,
                        TotalEmployees = g.Select(r => r.EmployeeId).Distinct().Count(),
                        TotalHours = g.Sum(r => r.TotalHours ?? 0),
                        AverageHoursPerEmployee = g.Select(r => r.EmployeeId).Distinct().Count() > 0 
                            ? g.Sum(r => r.TotalHours ?? 0) / g.Select(r => r.EmployeeId).Distinct().Count() 
                            : 0,
                        AttendanceRate = CalculateAttendancePercentage(g.Select(r => r.Date).Distinct().Count(), startDate, endDate)
                    })
                    .OrderBy(d => d.DepartmentName)
                    .ToList();

                return new SummaryReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    GeneratedAt = DateTime.UtcNow,
                    TotalEmployees = records.Select(r => r.EmployeeId).Distinct().Count(),
                    TotalWorkingDays = workingDays,
                    TotalHours = records.Sum(r => r.TotalHours ?? 0),
                    AverageHoursPerDay = records.Any() ? records.Sum(r => r.TotalHours ?? 0) / workingDays : 0,
                    OverallAttendanceRate = CalculateAttendancePercentage(records.Select(r => r.Date).Distinct().Count(), startDate, endDate),
                    DepartmentSummaries = departmentSummaries
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte resumen");
                throw;
            }
        }

        public async Task<byte[]> ExportAttendanceReportToCsvAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var report = await GenerateAttendanceReportAsync(startDate, endDate, employeeId, departmentId);
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Código Empleado,Nombre,Departamento,Días Totales,Días Presente,Días Ausente,% Asistencia,Promedio Llegada,Total Horas");

                foreach (var record in report.AttendanceRecords)
                {
                    csv.AppendLine($"{record.EmployeeCode},{record.EmployeeName},{record.DepartmentName},{record.TotalDays},{record.DaysPresent},{record.DaysAbsent},{record.AttendancePercentage:F1},{record.AverageArrivalTime:hh\\:mm},{record.TotalHours:F2}");
                }

                return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando reporte de asistencia a CSV");
                throw;
            }
        }

        public async Task<byte[]> ExportHoursReportToCsvAsync(DateTime startDate, DateTime endDate, int? employeeId = null, int? departmentId = null)
        {
            try
            {
                var report = await GenerateHoursReportAsync(startDate, endDate, employeeId, departmentId);
                
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Código Empleado,Nombre,Departamento,Total Horas,Horas Regulares,Horas Extra,Promedio Horas/Día,Días Trabajados");

                foreach (var record in report.HoursRecords)
                {
                    csv.AppendLine($"{record.EmployeeCode},{record.EmployeeName},{record.DepartmentName},{record.TotalHours:F2},{record.RegularHours:F2},{record.OvertimeHours:F2},{record.AverageHoursPerDay:F2},{record.DaysWorked}");
                }

                return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando reporte de horas a CSV");
                throw;
            }
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today;
                var end = endDate ?? DateTime.Today;

                var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
                var employeesPresent = await _context.TimeRecords
                    .Where(tr => tr.Date >= start && tr.Date <= end && tr.RecordType == RecordType.CheckIn)
                    .Select(tr => tr.EmployeeId)
                    .Distinct()
                    .CountAsync();

                var totalHours = await _context.TimeRecords
                    .Where(tr => tr.Date >= start && tr.Date <= end && tr.RecordType == RecordType.CheckIn)
                    .SumAsync(tr => tr.TotalHours ?? 0);

                var averageHours = employeesPresent > 0 ? totalHours / employeesPresent : 0;

                return new DashboardStatsDto
                {
                    TotalEmployees = totalEmployees,
                    EmployeesPresent = employeesPresent,
                    AttendanceRate = totalEmployees > 0 ? (double)employeesPresent / totalEmployees * 100 : 0,
                    TotalHours = totalHours,
                    AverageHours = averageHours,
                    Period = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del dashboard");
                throw;
            }
        }

        #region Private Helper Methods

        private int GetWorkingDaysInPeriod(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
            }
            return workingDays;
        }

        private double CalculateAttendancePercentage(int daysPresent, DateTime startDate, DateTime endDate)
        {
            var workingDays = GetWorkingDaysInPeriod(startDate, endDate);
            return workingDays > 0 ? (double)daysPresent / workingDays * 100 : 0;
        }

        private TimeSpan CalculateAverageArrivalTime(IEnumerable<DateTime> arrivalTimes)
        {
            if (!arrivalTimes.Any()) return TimeSpan.Zero;
            
            var totalTicks = arrivalTimes.Sum(t => t.TimeOfDay.Ticks);
            return new TimeSpan(totalTicks / arrivalTimes.Count());
        }

        private double CalculateRegularHours(double totalHours, int workingDays)
        {
            var maxRegularHours = workingDays * StandardWorkingHours;
            return Math.Min(totalHours, maxRegularHours);
        }

        private double CalculateOvertimeHours(double totalHours, int workingDays)
        {
            var maxRegularHours = workingDays * StandardWorkingHours;
            return Math.Max(0, totalHours - maxRegularHours);
        }

        #endregion
    }
}