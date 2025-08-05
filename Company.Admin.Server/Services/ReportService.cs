}
}using Microsoft.EntityFrameworkCore;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;
using Company.Admin.Server.Data;
using Shared.Services.Utils;

namespace Company.Admin.Server.Services
{
    public class ReportService : IReportService
    {
        private readonly CompanyDbContext _context;
        private readonly IExportService _exportService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            CompanyDbContext context,
            IExportService exportService,
            ILogger<ReportService> logger)
        {
            _context = context;
            _exportService = exportService;
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
                        EmployeeNumber = g.First().Employee?.EmployeeNumber ?? "",
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
                        EmployeeNumber = employeeGroup.First().Employee?.EmployeeNumber ?? "",
                        DepartmentName = employeeGroup.First().Employee?.Department?.Name ?? "",
                        TotalWorkHours = CalculateTotalWorkHours(employeeGroup.ToList()),
                        RegularHours = CalculateRegularHours(employeeGroup.ToList()),
                        OvertimeHours = CalculateOvertimeHours(employeeGroup.ToList()),
                        DaysWorked = employeeGroup.GroupBy(r => r.Date).Count(),
                        DaysPresent = CountDaysPresent(employeeGroup.ToList()),
                        DaysAbsent = CountDaysAbsent(employeeGroup.ToList(), request.StartDate, request.EndDate)
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
                var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
                var totalRecords = await _context.TimeRecords
                    .CountAsync(tr => tr.Date >= fromDate && tr.Date <= toDate);

                var records = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .Where(tr => tr.Date >= fromDate && tr.Date <= toDate)
                    .ToListAsync();

                var totalWorkHours = CalculateTotalWorkHours(records);
                var avgHoursPerEmployee = totalEmployees > 0 ? totalWorkHours / totalEmployees : 0;

                var attendanceRate = await CalculateAttendanceRateAsync(fromDate, toDate);
                var punctualityRate = await CalculatePunctualityRateAsync(fromDate, toDate);

                return new SummaryReportDto
                {
                    Period = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}",
                    TotalEmployees = totalEmployees,
                    TotalRecords = totalRecords,
                    TotalWorkHours = totalWorkHours,
                    AverageHoursPerEmployee = avgHoursPerEmployee,
                    AttendanceRate = attendanceRate,
                    PunctualityRate = punctualityRate,
                    GeneratedAt = DateTime.UtcNow
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
            var data = await GenerateAttendanceReportAsync(request);
            return _exportService.ExportToExcel(data.Cast<object>().ToList(), "Reporte de Asistencia");
        }

        public async Task<byte[]> ExportHoursReportToExcelAsync(HoursReportRequest request)
        {
            var data = await GenerateHoursReportAsync(request);
            return _exportService.ExportToExcel(data.Cast<object>().ToList(), "Reporte de Horas");
        }

        public async Task<byte[]> ExportSummaryReportToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            var data = await GenerateSummaryReportAsync(fromDate, toDate);
            var dataList = new List<object> { data };
            return _exportService.ExportToExcel(dataList, "Reporte Resumen");
        }

        public async Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportRequest request)
        {
            var data = await GenerateAttendanceReportAsync(request);
            return _exportService.ExportToCsv(data.Cast<object>().ToList());
        }

        public async Task<string> ExportHoursReportToCsvAsync(HoursReportRequest request)
        {
            var data = await GenerateHoursReportAsync(request);
            return _exportService.ExportToCsv(data.Cast<object>().ToList());
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Today.AddDays(-30);
            var endDate = toDate ?? DateTime.Today;

            var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
            var totalRecords = await _context.TimeRecords
                .CountAsync(tr => tr.Date >= startDate && tr.Date <= endDate);

            var records = await _context.TimeRecords
                .Include(tr => tr.Employee)
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate)
                .ToListAsync();

            var totalHours = CalculateTotalWorkHours(records);
            var attendanceRate = await CalculateAttendanceRateAsync(startDate, endDate);

            return new Dictionary<string, object>
            {
                ["TotalEmployees"] = totalEmployees,
                ["TotalRecords"] = totalRecords,
                ["TotalHours"] = totalHours,
                ["AttendanceRate"] = attendanceRate,
                ["Period"] = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}"
            };
        }

        public async Task<Dictionary<string, object>> GetEmployeeStatsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Today.AddDays(-30);
            var endDate = toDate ?? DateTime.Today;

            var records = await _context.TimeRecords
                .Where(tr => tr.EmployeeId == employeeId && tr.Date >= startDate && tr.Date <= endDate)
                .ToListAsync();

            var totalHours = CalculateTotalWorkHours(records);
            var daysWorked = records.GroupBy(r => r.Date).Count();

            return new Dictionary<string, object>
            {
                ["EmployeeId"] = employeeId,
                ["TotalRecords"] = records.Count,
                ["TotalHours"] = totalHours,
                ["DaysWorked"] = daysWorked,
                ["Period"] = $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}"
            };
        }

        // Métodos auxiliares privados
        private static double CalculateWorkHours(List<Shared.Models.TimeTracking.TimeRecord> dayRecords)
        {
            var workTime = TimeSpan.Zero;
            TimeSpan? checkInTime = null;

            foreach (var record in dayRecords.OrderBy(r => r.Time))
            {
                switch (record.Type)
                {
                    case RecordType.CheckIn:
                        checkInTime = record.Time;
                        break;
                    case RecordType.CheckOut:
                        if (checkInTime.HasValue)
                        {
                            workTime = workTime.Add(record.Time - checkInTime.Value);
                            checkInTime = null;
                        }
                        break;
                }
            }

            return workTime.TotalHours;
        }

        private static double CalculateTotalWorkHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            return records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .Sum(g => CalculateWorkHours(g.ToList()));
        }

        private static double CalculateRegularHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            // Asumimos 8 horas regulares por día
            const double regularHoursPerDay = 8.0;
            var totalHours = CalculateTotalWorkHours(records);
            var daysWorked = records.GroupBy(r => r.Date).Count();
            var maxRegularHours = daysWorked * regularHoursPerDay;

            return Math.Min(totalHours, maxRegularHours);
        }

        private static double CalculateOvertimeHours(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            const double regularHoursPerDay = 8.0;
            var totalHours = CalculateTotalWorkHours(records);
            var daysWorked = records.GroupBy(r => r.Date).Count();
            var maxRegularHours = daysWorked * regularHoursPerDay;

            return Math.Max(0, totalHours - maxRegularHours);
        }

        private static int CountDaysPresent(List<Shared.Models.TimeTracking.TimeRecord> records)
        {
            return records.GroupBy(r => r.Date).Count();
        }

        private static int CountDaysAbsent(List<Shared.Models.TimeTracking.TimeRecord> records, DateTime startDate, DateTime endDate)
        {
            var workDays = GetWorkDays(startDate, endDate);
            var presentDays = records.GroupBy(r => r.Date).Select(g => g.Key).ToHashSet();
            return workDays.Count(day => !presentDays.Contains(day));
        }

        private static List<DateTime> GetWorkDays(DateTime startDate, DateTime endDate)
        {
            var workDays = new List<DateTime>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    workDays.Add(date);
                }
            }
            return workDays;
        }

        private static bool IsLateArrival(List<Shared.Models.TimeTracking.TimeRecord> dayRecords)
        {
            var firstCheckIn = dayRecords
                .Where(r => r.Type == RecordType.CheckIn)
                .OrderBy(r => r.Time)
                .FirstOrDefault();

            // Asumimos que después de las 9:15 AM se considera tarde
            return firstCheckIn?.Time > new TimeSpan(9, 15, 0);
        }

        private static bool IsEarlyLeave(List<Shared.Models.TimeTracking.TimeRecord> dayRecords)
        {
            var lastCheckOut = dayRecords
                .Where(r => r.Type == RecordType.CheckOut)
                .OrderByDescending(r => r.Time)
                .FirstOrDefault();

            // Asumimos que antes de las 16:45 PM se considera salida temprana
            return lastCheckOut?.Time < new TimeSpan(16, 45, 0);
        }

        private static string DetermineAttendanceStatus(List<Shared.Models.TimeTracking.TimeRecord> dayRecords)
        {
            var hasCheckIn = dayRecords.Any(r => r.Type == RecordType.CheckIn);
            var hasCheckOut = dayRecords.Any(r => r.Type == RecordType.CheckOut);

            if (!hasCheckIn && !hasCheckOut)
                return "Ausente";

            if (hasCheckIn && !hasCheckOut)
                return "Sin Salida";

            if (IsLateArrival(dayRecords) || IsEarlyLeave(dayRecords))
                return "Irregular";

            return "Presente";
        }

        private async Task<double> CalculateAttendanceRateAsync(DateTime startDate, DateTime endDate)
        {
            var workDays = GetWorkDays(startDate, endDate);
            var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
            var totalExpectedAttendances = workDays.Count * totalEmployees;

            if (totalExpectedAttendances == 0)
                return 0;

            var actualAttendances = await _context.TimeRecords
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && tr.Type == RecordType.CheckIn)
                .GroupBy(tr => new { tr.EmployeeId, tr.Date })
                .CountAsync();

            return (double)actualAttendances / totalExpectedAttendances * 100;
        }

        private async Task<double> CalculatePunctualityRateAsync(DateTime startDate, DateTime endDate)
        {
            var checkIns = await _context.TimeRecords
                .Where(tr => tr.Date >= startDate && tr.Date <= endDate && tr.Type == RecordType.CheckIn)
                .ToListAsync();

            if (!checkIns.Any())
                return 0;

            var punctualCheckIns = checkIns.Count(ci => ci.Time <= new TimeSpan(9, 15, 0));
            return (double)punctualCheckIns / checkIns.Count * 100;
        }
    }
}