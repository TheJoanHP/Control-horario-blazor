using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;
using Shared.Services.Utils;

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
                    .ThenInclude(e => e.User)
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= request.StartDate && tr.Date <= request.EndDate);

                if (request.EmployeeIds != null && request.EmployeeIds.Any())
                    query = query.Where(tr => request.EmployeeIds.Contains(tr.EmployeeId));

                if (request.DepartmentIds != null && request.DepartmentIds.Any())
                    query = query.Where(tr => tr.Employee.DepartmentId.HasValue && 
                                            request.DepartmentIds.Contains(tr.Employee.DepartmentId.Value));

                var records = await query
                    .OrderBy(tr => tr.Employee.User.LastName)
                    .ThenBy(tr => tr.Employee.User.FirstName)
                    .ThenBy(tr => tr.Date)
                    .ToListAsync();

                var attendanceData = records
                    .GroupBy(tr => new { tr.EmployeeId, tr.Date })
                    .Select(g => new AttendanceReportDto
                    {
                        EmployeeId = g.Key.EmployeeId,
                        EmployeeName = g.First().Employee?.User?.FullName ?? "",
                        EmployeeNumber = g.First().Employee?.EmployeeCode ?? "",
                        DepartmentName = g.First().Employee?.Department?.Name ?? "",
                        Date = g.Key.Date,
                        CheckInTime = g.Where(r => r.Type == RecordType.CheckIn).OrderBy(r => r.Time).FirstOrDefault()?.Time,
                        CheckOutTime = g.Where(r => r.Type == RecordType.CheckOut).OrderByDescending(r => r.Time).FirstOrDefault()?.Time,
                        TotalHours = CalculateWorkHours(g.ToList()),
                        IsLate = IsLateArrival(g.ToList()),
                        IsEarlyLeave = IsEarlyLeave(g.ToList()),
                        Status = DetermineAttendanceStatus(g.ToList())
                    })
                    .ToList();

                return attendanceData;
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
                    .ThenInclude(e => e.User)
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= request.StartDate && tr.Date <= request.EndDate);

                if (request.EmployeeIds != null && request.EmployeeIds.Any())
                    query = query.Where(tr => request.EmployeeIds.Contains(tr.EmployeeId));

                if (request.DepartmentIds != null && request.DepartmentIds.Any())
                    query = query.Where(tr => tr.Employee.DepartmentId.HasValue && 
                                            request.DepartmentIds.Contains(tr.Employee.DepartmentId.Value));

                var records = await query
                    .OrderBy(tr => tr.Employee.User.LastName)
                    .ThenBy(tr => tr.Employee.User.FirstName)
                    .ThenBy(tr => tr.Date)
                    .ToListAsync();

                var hoursData = records
                    .GroupBy(tr => tr.EmployeeId)
                    .Select(employeeGroup => new HoursReportDto
                    {
                        EmployeeId = employeeGroup.Key,
                        EmployeeName = employeeGroup.First().Employee?.User?.FullName ?? "",
                        EmployeeNumber = employeeGroup.First().Employee?.EmployeeCode ?? "",
                        DepartmentName = employeeGroup.First().Employee?.Department?.Name ?? "",
                        TotalWorkHours = CalculateTotalHours(employeeGroup.ToList()),
                        RegularHours = CalculateRegularHours(employeeGroup.ToList()),
                        OvertimeHours = CalculateOvertimeHours(employeeGroup.ToList()),
                        DaysWorked = employeeGroup.GroupBy(r => r.Date).Count(),
                        DaysPresent = employeeGroup.GroupBy(r => r.Date).Count(g => g.Any(r => r.Type == RecordType.CheckIn)),
                        DaysAbsent = 0 // Calcular según lógica de negocio
                    })
                    .ToList();

                return hoursData;
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
                var employees = await _context.Employees
                    .Where(e => e.Active)
                    .CountAsync();

                var timeRecords = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .Where(tr => tr.Date >= fromDate && tr.Date <= toDate)
                    .ToListAsync();

                var totalHours = CalculateTotalHours(timeRecords);
                var avgHoursPerEmployee = employees > 0 ? totalHours / employees : 0;

                return new SummaryReportDto
                {
                    Period = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}",
                    TotalEmployees = employees,
                    TotalRecords = timeRecords.Count,
                    TotalWorkHours = totalHours,
                    AverageHoursPerEmployee = avgHoursPerEmployee,
                    AttendanceRate = CalculateAttendanceRate(timeRecords, employees),
                    PunctualityRate = CalculatePunctualityRate(timeRecords),
                    GeneratedAt = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte resumen");
                throw;
            }
        }

        public async Task<byte[]> ExportAttendanceReportToExcelAsync(AttendanceReportRequest request)
        {
            // TODO: Implementar exportación a Excel usando EPPlus
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportHoursReportToExcelAsync(HoursReportRequest request)
        {
            // TODO: Implementar exportación a Excel usando EPPlus
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportSummaryReportToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            // TODO: Implementar exportación a Excel usando EPPlus
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        public async Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportRequest request)
        {
            // TODO: Implementar exportación a CSV
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<string> ExportHoursReportToCsvAsync(HoursReportRequest request)
        {
            // TODO: Implementar exportación a CSV
            await Task.CompletedTask;
            return string.Empty;
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var startDate = fromDate ?? DateTime.Today.AddDays(-30);
                var endDate = toDate ?? DateTime.Today;

                var stats = new Dictionary<string, object>
                {
                    ["totalEmployees"] = await _context.Employees.CountAsync(e => e.Active),
                    ["totalRecords"] = await _context.TimeRecords.CountAsync(tr => tr.Date >= startDate && tr.Date <= endDate),
                    ["averageHoursPerDay"] = 8.0, // TODO: Calcular real
                    ["attendanceRate"] = 95.5 // TODO: Calcular real
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del dashboard");
                return new Dictionary<string, object>();
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
                    ["totalRecords"] = records.Count,
                    ["totalHours"] = CalculateTotalHours(records),
                    ["averageHoursPerDay"] = records.Count > 0 ? CalculateTotalHours(records) / records.GroupBy(r => r.Date).Count() : 0,
                    ["daysWorked"] = records.GroupBy(r => r.Date).Count()
                };

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del empleado {EmployeeId}", employeeId);
                return new Dictionary<string, object>();
            }
        }

        // Métodos privados para cálculos
        private double CalculateWorkHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var checkIn = records.FirstOrDefault(r => r.Type == RecordType.CheckIn);
            var checkOut = records.FirstOrDefault(r => r.Type == RecordType.CheckOut);

            if (checkIn != null && checkOut != null)
            {
                var timeSpan = checkOut.Time - checkIn.Time;
                return timeSpan.TotalHours;
            }

            return 0;
        }

        private double CalculateTotalHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            return records
                .GroupBy(r => r.Date)
                .Sum(g => CalculateWorkHours(g.ToList()));
        }

        private double CalculateRegularHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var totalHours = CalculateTotalHours(records);
            const double maxRegularHoursPerDay = 8;
            var daysWorked = records.GroupBy(r => r.Date).Count();
            
            return Math.Min(totalHours, daysWorked * maxRegularHoursPerDay);
        }

        private double CalculateOvertimeHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var totalHours = CalculateTotalHours(records);
            var regularHours = CalculateRegularHours(records);
            
            return Math.Max(0, totalHours - regularHours);
        }

        private bool IsLateArrival(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var checkIn = records.FirstOrDefault(r => r.Type == RecordType.CheckIn);
            if (checkIn == null) return false;

            // Considerar tarde si llega después de las 9:00 AM
            var standardStartTime = new TimeSpan(9, 0, 0);
            return checkIn.Time > standardStartTime;
        }

        private bool IsEarlyLeave(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var checkOut = records.FirstOrDefault(r => r.Type == RecordType.CheckOut);
            if (checkOut == null) return false;

            // Considerar salida temprana si sale antes de las 5:00 PM
            var standardEndTime = new TimeSpan(17, 0, 0);
            return checkOut.Time < standardEndTime;
        }

        private string DetermineAttendanceStatus(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var hasCheckIn = records.Any(r => r.Type == RecordType.CheckIn);
            var hasCheckOut = records.Any(r => r.Type == RecordType.CheckOut);

            if (!hasCheckIn) return "Ausente";
            if (!hasCheckOut) return "Sin salida";
            if (IsLateArrival(records)) return "Tarde";
            if (IsEarlyLeave(records)) return "Salida temprana";
            
            return "Presente";
        }

        private double CalculateAttendanceRate(List<Shared.Models.TimeTracking.TimeRecord> records, int totalEmployees)
        {
            if (totalEmployees == 0) return 0;

            var employeesWithRecords = records
                .Where(r => r.Type == RecordType.CheckIn)
                .Select(r => r.EmployeeId)
                .Distinct()
                .Count();

            return totalEmployees > 0 ? (double)employeesWithRecords / totalEmployees * 100 : 0;
        }

        private double CalculatePunctualityRate(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            var dailyRecords = records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .ToList();

            if (!dailyRecords.Any()) return 0;

            var punctualDays = dailyRecords.Count(g => !IsLateArrival(g.ToList()));
            
            return (double)punctualDays / dailyRecords.Count * 100;
        }
    }
}