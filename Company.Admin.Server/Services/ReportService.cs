using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;

namespace Company.Admin.Server.Services
{
    public class ReportService : IReportService
    {
        private readonly CompanyDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(CompanyDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<AttendanceReportDto>> GenerateAttendanceReportAsync(AttendanceReportRequest request)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= request.StartDate && tr.Date <= request.EndDate);

                if (request.EmployeeId.HasValue)
                {
                    query = query.Where(tr => tr.EmployeeId == request.EmployeeId);
                }

                if (request.DepartmentId.HasValue)
                {
                    query = query.Where(tr => tr.Employee.DepartmentId == request.DepartmentId);
                }

                var records = await query.ToListAsync();

                var report = records
                    .GroupBy(tr => new { tr.EmployeeId, tr.Date })
                    .Select(g => new AttendanceReportDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = g.First().Employee.FirstName + " " + g.First().Employee.LastName,
                        DepartmentName = g.First().Employee.Department?.Name ?? "Sin departamento",
                        Date = g.Key.Date,
                        CheckIn = g.Where(tr => tr.Type == RecordType.CheckIn).Min(tr => tr.Time),
                        CheckOut = g.Where(tr => tr.Type == RecordType.CheckOut).Max(tr => tr.Time),
                        WorkedHours = CalculateWorkedHours(g.ToList()),
                        BreakTime = CalculateBreakTime(g.ToList()),
                        Status = DetermineAttendanceStatus(g.ToList())
                    })
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.EmployeeName);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de asistencia");
                throw;
            }
        }

        public async Task<IEnumerable<HoursReportDto>> GenerateHoursReportAsync(HoursReportRequest request)
        {
            try
            {
                var query = _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= request.StartDate && tr.Date <= request.EndDate);

                if (request.EmployeeId.HasValue)
                {
                    query = query.Where(tr => tr.EmployeeId == request.EmployeeId);
                }

                if (request.DepartmentId.HasValue)
                {
                    query = query.Where(tr => tr.Employee.DepartmentId == request.DepartmentId);
                }

                var records = await query.ToListAsync();

                var report = records
                    .GroupBy(tr => tr.EmployeeId)
                    .Select(g => new HoursReportDto
                    {
                        EmployeeId = g.Key,
                        EmployeeName = g.First().Employee.FirstName + " " + g.First().Employee.LastName,
                        DepartmentName = g.First().Employee.Department?.Name ?? "Sin departamento",
                        TotalWorkedHours = CalculateTotalWorkedHours(g.ToList()),
                        RegularHours = CalculateRegularHours(g.ToList()),
                        OvertimeHours = CalculateOvertimeHours(g.ToList()),
                        TotalBreakTime = CalculateTotalBreakTime(g.ToList())
                    })
                    .OrderBy(r => r.EmployeeName);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte de horas");
                throw;
            }
        }

        public async Task<SummaryReportDto> GenerateSummaryReportAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var records = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .Where(tr => tr.Date >= fromDate && tr.Date <= toDate)
                    .ToListAsync();

                var summary = new SummaryReportDto
                {
                    StartDate = fromDate,
                    EndDate = toDate,
                    TotalEmployees = await _context.Employees.CountAsync(e => e.Active),
                    TotalWorkedHours = CalculateTotalWorkedHours(records),
                    TotalOvertimeHours = CalculateOvertimeHours(records),
                    AverageWorkedHours = records.Any() ? CalculateTotalWorkedHours(records) / records.GroupBy(r => r.EmployeeId).Count() : 0,
                    TotalBreakTime = CalculateTotalBreakTime(records)
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte resumen");
                throw;
            }
        }

        public async Task<byte[]> ExportAttendanceReportToExcelAsync(AttendanceReportRequest request)
        {
            // TODO: Implementar exportación a Excel
            await Task.CompletedTask;
            throw new NotImplementedException("Exportación a Excel no implementada aún");
        }

        public async Task<byte[]> ExportHoursReportToExcelAsync(HoursReportRequest request)
        {
            // TODO: Implementar exportación a Excel
            await Task.CompletedTask;
            throw new NotImplementedException("Exportación a Excel no implementada aún");
        }

        public async Task<byte[]> ExportSummaryReportToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            // TODO: Implementar exportación a Excel
            await Task.CompletedTask;
            throw new NotImplementedException("Exportación a Excel no implementada aún");
        }

        public async Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportRequest request)
        {
            // TODO: Implementar exportación a CSV
            await Task.CompletedTask;
            throw new NotImplementedException("Exportación a CSV no implementada aún");
        }

        public async Task<string> ExportHoursReportToCsvAsync(HoursReportRequest request)
        {
            // TODO: Implementar exportación a CSV
            await Task.CompletedTask;
            throw new NotImplementedException("Exportación a CSV no implementada aún");
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.Today.AddDays(-30);
                var endDate = toDate ?? DateTime.Today;

                var stats = new Dictionary<string, object>
                {
                    ["TotalEmployees"] = await _context.Employees.CountAsync(e => e.Active),
                    ["PresentToday"] = await GetPresentTodayCountAsync(),
                    ["AbsentToday"] = await GetAbsentTodayCountAsync(),
                    ["OnBreak"] = await GetOnBreakCountAsync(),
                    ["TotalHoursThisMonth"] = await GetTotalHoursThisMonth(),
                    ["AverageHoursPerDay"] = await GetAverageHoursPerDay(startDate, endDate)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del dashboard");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetEmployeeStatsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.Today.AddDays(-30);
                var endDate = toDate ?? DateTime.Today;

                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && tr.Date >= startDate && tr.Date <= endDate)
                    .ToListAsync();

                var stats = new Dictionary<string, object>
                {
                    ["TotalWorkedHours"] = CalculateTotalWorkedHours(records),
                    ["TotalDaysWorked"] = records.Select(r => r.Date).Distinct().Count(),
                    ["AverageHoursPerDay"] = records.Any() ? CalculateTotalWorkedHours(records) / records.Select(r => r.Date).Distinct().Count() : 0,
                    ["TotalBreakTime"] = CalculateTotalBreakTime(records),
                    ["OvertimeHours"] = CalculateOvertimeHours(records)
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        // Métodos auxiliares
        private decimal CalculateWorkedHours(List<dynamic> records)
        {
            // TODO: Implementar cálculo de horas trabajadas
            return 8.0m; // Placeholder
        }

        private decimal CalculateBreakTime(List<dynamic> records)
        {
            // TODO: Implementar cálculo de tiempo de descanso
            return 1.0m; // Placeholder
        }

        private string DetermineAttendanceStatus(List<dynamic> records)
        {
            // TODO: Implementar determinación de estado de asistencia
            return "Presente"; // Placeholder
        }

        private decimal CalculateTotalWorkedHours(List<dynamic> records)
        {
            // TODO: Implementar cálculo total de horas trabajadas
            return records.Count * 8.0m; // Placeholder
        }

        private decimal CalculateRegularHours(List<dynamic> records)
        {
            // TODO: Implementar cálculo de horas regulares
            return Math.Min(CalculateTotalWorkedHours(records), 8.0m * records.Select(r => ((dynamic)r).Date).Distinct().Count());
        }

        private decimal CalculateOvertimeHours(List<dynamic> records)
        {
            // TODO: Implementar cálculo de horas extra
            return Math.Max(0, CalculateTotalWorkedHours(records) - CalculateRegularHours(records));
        }

        private decimal CalculateTotalBreakTime(List<dynamic> records)
        {
            // TODO: Implementar cálculo total de tiempo de descanso
            return records.Count * 1.0m; // Placeholder
        }

        private async Task<int> GetPresentTodayCountAsync()
        {
            var today = DateTime.Today;
            return await _context.TimeRecords
                .Where(tr => tr.Date == today && tr.Type == RecordType.CheckIn)
                .Select(tr => tr.EmployeeId)
                .Distinct()
                .CountAsync();
        }

        private async Task<int> GetAbsentTodayCountAsync()
        {
            var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
            var presentToday = await GetPresentTodayCountAsync();
            return totalEmployees - presentToday;
        }

        private async Task<int> GetOnBreakCountAsync()
        {
            var today = DateTime.Today;
            return await _context.TimeRecords
                .Where(tr => tr.Date == today && tr.Type == RecordType.BreakStart)
                .Select(tr => tr.EmployeeId)
                .Distinct()
                .CountAsync();
        }

        private async Task<decimal> GetTotalHoursThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var records = await _context.TimeRecords
                .Where(tr => tr.Date >= startOfMonth && tr.Date <= endOfMonth)
                .ToListAsync();

            return CalculateTotalWorkedHours(records);
        }

        private async Task<decimal> GetAverageHoursPerDay(DateTime startDate, DateTime endDate)
        {
            var records = await _context.TimeRecords
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate)
                .ToListAsync();

            var totalDays = (endDate - startDate).Days + 1;
            return totalDays > 0 ? CalculateTotalWorkedHours(records) / totalDays : 0;
        }
    }
}