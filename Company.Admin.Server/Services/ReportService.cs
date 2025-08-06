using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.DTOs.Reports;
using Shared.Models.Enums;
using Shared.Models.TimeTracking;

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
                        DaysAbsent = CalculateAbsentDays(employeeGroup.ToList(), request.StartDate, request.EndDate),
                        AverageHoursPerDay = CalculateAverageHoursPerDay(employeeGroup.ToList()),
                        AttendanceRate = CalculateEmployeeAttendanceRate(employeeGroup.ToList(), request.StartDate, request.EndDate)
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
                var totalEmployees = await _context.Employees
                    .Where(e => e.Active)
                    .CountAsync();

                var timeRecords = await _context.TimeRecords
                    .Include(tr => tr.Employee)
                    .ThenInclude(e => e.Department)
                    .Where(tr => tr.Date >= fromDate && tr.Date <= toDate)
                    .ToListAsync();

                var totalHours = CalculateTotalHours(timeRecords);
                var avgHoursPerEmployee = totalEmployees > 0 ? totalHours / totalEmployees : 0;
                var employeesWithRecords = timeRecords.Select(r => r.EmployeeId).Distinct().Count();

                // Generar resumen por departamentos
                var departmentSummaries = await GenerateDepartmentSummariesAsync(fromDate, toDate);

                // Top empleados por horas
                var topEmployees = await GenerateTopEmployeesByHoursAsync(fromDate, toDate);

                // Días con alto ausentismo
                var absenteeismDays = await GenerateAbsenteeismDataAsync(fromDate, toDate);

                return new SummaryReportDto
                {
                    Period = $"{fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}",
                    TotalEmployees = totalEmployees,
                    ActiveWorkingEmployees = employeesWithRecords,
                    TotalRecords = timeRecords.Count,
                    TotalWorkHours = totalHours,
                    AverageHoursPerEmployee = avgHoursPerEmployee,
                    AttendanceRate = CalculateAttendanceRate(timeRecords, totalEmployees, fromDate, toDate),
                    PunctualityRate = CalculatePunctualityRate(timeRecords),
                    TotalOvertimeHours = CalculateOvertimeHours(timeRecords),
                    WorkingDaysInPeriod = CalculateWorkingDays(fromDate, toDate),
                    TopDepartmentByHours = departmentSummaries.OrderByDescending(d => d.TotalHours).FirstOrDefault()?.DepartmentName ?? "",
                    TopDepartmentByAttendance = departmentSummaries.OrderByDescending(d => d.AttendanceRate).FirstOrDefault()?.DepartmentName ?? "",
                    GeneratedAt = DateTime.Now,
                    DepartmentSummaries = departmentSummaries.ToList(),
                    TopEmployeesByHours = topEmployees.Take(5).ToList(),
                    HighAbsenteeismDays = absenteeismDays.OrderByDescending(d => d.AbsenteeismRate).Take(10).ToList()
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

                var stats = new Dictionary<string, object>();

                // Estadísticas básicas
                var totalEmployees = await _context.Employees.Where(e => e.Active).CountAsync();
                var totalRecords = await _context.TimeRecords
                    .Where(tr => tr.Date >= startDate && tr.Date <= endDate)
                    .CountAsync();

                var todayRecords = await _context.TimeRecords
                    .Where(tr => tr.Date == DateTime.Today)
                    .CountAsync();

                stats.Add("totalEmployees", totalEmployees);
                stats.Add("totalRecords", totalRecords);
                stats.Add("todayRecords", todayRecords);
                stats.Add("period", $"{startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}");

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

                var stats = new Dictionary<string, object>();

                var records = await _context.TimeRecords
                    .Where(tr => tr.EmployeeId == employeeId && tr.Date >= startDate && tr.Date <= endDate)
                    .ToListAsync();

                stats.Add("totalRecords", records.Count);
                stats.Add("totalHours", CalculateTotalHours(records));
                stats.Add("daysWorked", records.GroupBy(r => r.Date).Count());

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas del empleado {EmployeeId}", employeeId);
                throw;
            }
        }

        #region Private Helper Methods

        private double CalculateWorkHours(List<TimeRecord> dayRecords)
        {
            var checkIn = dayRecords.Where(r => r.Type == RecordType.CheckIn).OrderBy(r => r.Time).FirstOrDefault();
            var checkOut = dayRecords.Where(r => r.Type == RecordType.CheckOut).OrderByDescending(r => r.Time).FirstOrDefault();

            if (checkIn == null || checkOut == null)
                return 0;

            var totalMinutes = (checkOut.Time - checkIn.Time).TotalMinutes;
            
            // Descontar tiempo de descansos si existe
            var breakMinutes = dayRecords
                .Where(r => r.Type == RecordType.BreakStart || r.Type == RecordType.BreakEnd)
                .GroupBy(r => r.Date)
                .Sum(g => CalculateBreakTime(g.ToList()));

            return Math.Max(0, (totalMinutes - breakMinutes) / 60.0);
        }

        private double CalculateBreakTime(List<TimeRecord> breakRecords)
        {
            var breakStarts = breakRecords.Where(r => r.Type == RecordType.BreakStart).OrderBy(r => r.Time).ToList();
            var breakEnds = breakRecords.Where(r => r.Type == RecordType.BreakEnd).OrderBy(r => r.Time).ToList();

            double totalBreakMinutes = 0;
            var minCount = Math.Min(breakStarts.Count, breakEnds.Count);

            for (int i = 0; i < minCount; i++)
            {
                totalBreakMinutes += (breakEnds[i].Time - breakStarts[i].Time).TotalMinutes;
            }

            return totalBreakMinutes;
        }

        private double CalculateTotalHours(List<TimeRecord> records)
        {
            return records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .Sum(g => CalculateWorkHours(g.ToList()));
        }

        private double CalculateRegularHours(List<TimeRecord> records)
        {
            // Lógica para calcular horas regulares (máximo 8 horas por día)
            return records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .Sum(g => Math.Min(8.0, CalculateWorkHours(g.ToList())));
        }

        private double CalculateOvertimeHours(List<TimeRecord> records)
        {
            // Lógica para calcular horas extra (más de 8 horas por día)
            return records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .Sum(g => Math.Max(0, CalculateWorkHours(g.ToList()) - 8.0));
        }

        private bool IsLateArrival(List<TimeRecord> dayRecords)
        {
            var checkIn = dayRecords.Where(r => r.Type == RecordType.CheckIn).OrderBy(r => r.Time).FirstOrDefault();
            if (checkIn == null) return false;

            // Considerar tardanza después de las 9:00 AM (esto debería ser configurable por empleado/departamento)
            var expectedTime = new TimeSpan(9, 0, 0);
            return checkIn.Time > expectedTime;
        }

        private bool IsEarlyLeave(List<TimeRecord> dayRecords)
        {
            var checkOut = dayRecords.Where(r => r.Type == RecordType.CheckOut).OrderByDescending(r => r.Time).FirstOrDefault();
            if (checkOut == null) return false;

            // Considerar salida temprana antes de las 5:00 PM (esto debería ser configurable)
            var expectedTime = new TimeSpan(17, 0, 0);
            return checkOut.Time < expectedTime;
        }

        private string DetermineAttendanceStatus(List<TimeRecord> dayRecords)
        {
            var hasCheckIn = dayRecords.Any(r => r.Type == RecordType.CheckIn);
            var hasCheckOut = dayRecords.Any(r => r.Type == RecordType.CheckOut);

            if (!hasCheckIn) return "Ausente";
            if (IsLateArrival(dayRecords)) return "Tardanza";
            if (IsEarlyLeave(dayRecords)) return "Salida Temprana";
            if (!hasCheckOut) return "Sin Salida";

            return "Presente";
        }

        private int CalculateAbsentDays(List<TimeRecord> records, DateTime startDate, DateTime endDate)
        {
            var workingDays = CalculateWorkingDays(startDate, endDate);
            var presentDays = records.GroupBy(r => r.Date).Count();
            return Math.Max(0, workingDays - presentDays);
        }

        private double CalculateAverageHoursPerDay(List<TimeRecord> records)
        {
            var daysWorked = records.GroupBy(r => r.Date).Count();
            if (daysWorked == 0) return 0;

            var totalHours = CalculateTotalHours(records);
            return totalHours / daysWorked;
        }

        private double CalculateEmployeeAttendanceRate(List<TimeRecord> records, DateTime startDate, DateTime endDate)
        {
            var workingDays = CalculateWorkingDays(startDate, endDate);
            if (workingDays == 0) return 100;

            var presentDays = records.GroupBy(r => r.Date).Count();
            return (double)presentDays / workingDays * 100;
        }

        private double CalculateAttendanceRate(List<TimeRecord> records, int totalEmployees, DateTime startDate, DateTime endDate)
        {
            var workingDays = CalculateWorkingDays(startDate, endDate);
            if (workingDays == 0 || totalEmployees == 0) return 100;

            var employeesWithRecords = records.Select(r => r.EmployeeId).Distinct().Count();
            var expectedAttendances = totalEmployees * workingDays;
            var actualAttendances = records.GroupBy(r => new { r.EmployeeId, r.Date }).Count();

            return expectedAttendances > 0 ? (double)actualAttendances / expectedAttendances * 100 : 0;
        }

        private double CalculatePunctualityRate(List<TimeRecord> records)
        {
            var dailyRecords = records
                .GroupBy(r => new { r.EmployeeId, r.Date })
                .ToList();

            if (!dailyRecords.Any()) return 100;

            var punctualDays = dailyRecords.Count(g => !IsLateArrival(g.ToList()));
            
            return (double)punctualDays / dailyRecords.Count * 100;
        }

        private int CalculateWorkingDays(DateTime startDate, DateTime endDate)
        {
            int workingDays = 0;
            var current = startDate.Date;

            while (current <= endDate.Date)
            {
                // Contar solo días laborables (Lunes a Viernes)
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    workingDays++;
                }
                current = current.AddDays(1);
            }

            return workingDays;
        }

        private async Task<IEnumerable<DepartmentSummaryDto>> GenerateDepartmentSummariesAsync(DateTime fromDate, DateTime toDate)
        {
            var departmentData = await _context.TimeRecords
                .Include(tr => tr.Employee)
                .ThenInclude(e => e.Department)
                .Where(tr => tr.Date >= fromDate && tr.Date <= toDate && tr.Employee.Department != null)
                .GroupBy(tr => new { tr.Employee.Department.Id, tr.Employee.Department.Name })
                .Select(g => new
                {
                    DepartmentId = g.Key.Id,
                    DepartmentName = g.Key.Name,
                    Records = g.ToList(),
                    EmployeeCount = g.Select(r => r.EmployeeId).Distinct().Count()
                })
                .ToListAsync();

            return departmentData.Select(d => new DepartmentSummaryDto
            {
                DepartmentId = d.DepartmentId,
                DepartmentName = d.DepartmentName,
                EmployeeCount = d.EmployeeCount,
                TotalHours = CalculateTotalHours(d.Records),
                AverageHours = d.EmployeeCount > 0 ? CalculateTotalHours(d.Records) / d.EmployeeCount : 0,
                AttendanceRate = CalculateEmployeeAttendanceRate(d.Records, fromDate, toDate),
                PunctualityRate = CalculatePunctualityRate(d.Records),
                OvertimeHours = CalculateOvertimeHours(d.Records)
            });
        }

        private async Task<IEnumerable<EmployeeSummaryDto>> GenerateTopEmployeesByHoursAsync(DateTime fromDate, DateTime toDate)
        {
            var employeeData = await _context.TimeRecords
                .Include(tr => tr.Employee)
                .ThenInclude(e => e.User)
                .Include(tr => tr.Employee)
                .ThenInclude(e => e.Department)
                .Where(tr => tr.Date >= fromDate && tr.Date <= toDate)
                .GroupBy(tr => new 
                { 
                    tr.Employee.Id, 
                    tr.Employee.User.FullName, 
                    tr.Employee.EmployeeCode,
                    DepartmentName = tr.Employee.Department != null ? tr.Employee.Department.Name : ""
                })
                .Select(g => new
                {
                    EmployeeId = g.Key.Id,
                    EmployeeName = g.Key.FullName,
                    EmployeeNumber = g.Key.EmployeeCode,
                    DepartmentName = g.Key.DepartmentName,
                    Records = g.ToList(),
                    DaysWorked = g.GroupBy(r => r.Date).Count()
                })
                .ToListAsync();

            return employeeData
                .Select(e => new EmployeeSummaryDto
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName ?? "",
                    EmployeeNumber = e.EmployeeNumber ?? "",
                    DepartmentName = e.DepartmentName,
                    TotalHours = CalculateTotalHours(e.Records),
                    AverageHoursPerDay = e.DaysWorked > 0 ? CalculateTotalHours(e.Records) / e.DaysWorked : 0,
                    DaysWorked = e.DaysWorked,
                    AttendanceRate = CalculateEmployeeAttendanceRate(e.Records, fromDate, toDate),
                    OvertimeHours = CalculateOvertimeHours(e.Records)
                })
                .OrderByDescending(e => e.TotalHours);
        }

        private async Task<IEnumerable<DayAbsenteeismDto>> GenerateAbsenteeismDataAsync(DateTime fromDate, DateTime toDate)
        {
            var totalEmployees = await _context.Employees.Where(e => e.Active).CountAsync();
            var absenteeismData = new List<DayAbsenteeismDto>();

            var current = fromDate.Date;
            while (current <= toDate.Date)
            {
                // Solo días laborables
                if (current.DayOfWeek != DayOfWeek.Saturday && current.DayOfWeek != DayOfWeek.Sunday)
                {
                    var presentEmployees = await _context.TimeRecords
                        .Where(tr => tr.Date == current && tr.Type == RecordType.CheckIn)
                        .Select(tr => tr.EmployeeId)
                        .Distinct()
                        .CountAsync();

                    var absentEmployees = totalEmployees - presentEmployees;
                    var absenteeismRate = totalEmployees > 0 ? (double)absentEmployees / totalEmployees * 100 : 0;

                    absenteeismData.Add(new DayAbsenteeismDto
                    {
                        Date = current,
                        ExpectedEmployees = totalEmployees,
                        PresentEmployees = presentEmployees,
                        AbsentEmployees = absentEmployees,
                        AbsenteeismRate = absenteeismRate,
                        DayOfWeek = current.ToString("dddd", new System.Globalization.CultureInfo("es-ES"))
                    });
                }

                current = current.AddDays(1);
            }

            return absenteeismData;
        }

        #endregion
    }
}