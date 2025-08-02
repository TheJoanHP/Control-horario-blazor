using Microsoft.EntityFrameworkCore;
using Company.Admin.Server.Data;
using Shared.Models.DTOs.Reports;
using Shared.Services.Utils;
using System.Text;
using ClosedXML.Excel;

namespace Company.Admin.Server.Services
{
    public class ReportService : IReportService
    {
        private readonly CompanyDbContext _context;
        private readonly ITimeTrackingService _timeTrackingService;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            CompanyDbContext context,
            ITimeTrackingService timeTrackingService,
            ILogger<ReportService> logger)
        {
            _context = context;
            _timeTrackingService = timeTrackingService;
            _logger = logger;
        }

        public async Task<IEnumerable<AttendanceReportDto>> GenerateAttendanceReportAsync(AttendanceReportRequest request)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Company)
                .Where(e => e.Active)
                .Where(e => request.EmployeeId == null || e.Id == request.EmployeeId)
                .Where(e => request.DepartmentId == null || e.DepartmentId == request.DepartmentId)
                .ToListAsync();

            var attendanceReport = new List<AttendanceReportDto>();

            foreach (var employee in employees)
            {
                var current = request.FromDate.Date;
                while (current <= request.ToDate.Date)
                {
                    if (!request.IncludeWeekends && (current.DayOfWeek == DayOfWeek.Saturday || current.DayOfWeek == DayOfWeek.Sunday))
                    {
                        current = current.AddDays(1);
                        continue;
                    }

                    var dailyRecords = await _timeTrackingService.GetDailyRecordsAsync(employee.Id, current);
                    var checkIn = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckIn);
                    var checkOut = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckOut);

                    var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employee.Id, current);
                    var breakTime = await _timeTrackingService.CalculateBreakTimeAsync(employee.Id, current);

                    var isAbsent = checkIn == null;
                    var isLate = checkIn != null && checkIn.Timestamp.TimeOfDay > employee.WorkStartTime.Add(TimeSpan.FromMinutes(employee.Company?.ToleranceMinutes ?? 15));

                    var status = isAbsent ? "Ausente" : isLate ? "Tardanza" : "Presente";

                    if (!request.IncludeAbsent && isAbsent)
                    {
                        current = current.AddDays(1);
                        continue;
                    }

                    attendanceReport.Add(new AttendanceReportDto
                    {
                        EmployeeId = employee.Id,
                        EmployeeName = employee.FullName,
                        EmployeeCode = employee.EmployeeCode,
                        DepartmentName = employee.Department?.Name,
                        Date = current,
                        CheckInTime = checkIn?.Timestamp,
                        CheckOutTime = checkOut?.Timestamp,
                        WorkedHours = workedHours,
                        BreakTime = breakTime,
                        OvertimeHours = workedHours > TimeSpan.FromHours(8) ? workedHours - TimeSpan.FromHours(8) : TimeSpan.Zero,
                        IsLate = isLate,
                        IsAbsent = isAbsent,
                        Status = status,
                        Notes = checkIn?.Notes
                    });

                    current = current.AddDays(1);
                }
            }

            return attendanceReport.OrderBy(r => r.Date).ThenBy(r => r.EmployeeName);
        }

        public async Task<IEnumerable<HoursReportDto>> GenerateHoursReportAsync(HoursReportRequest request)
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Where(e => e.Active)
                .Where(e => request.EmployeeId == null || e.Id == request.EmployeeId)
                .Where(e => request.DepartmentId == null || e.DepartmentId == request.DepartmentId)
                .ToListAsync();

            var hoursReport = new List<HoursReportDto>();

            foreach (var employee in employees)
            {
                var totalWorkedHours = TimeSpan.Zero;
                var totalBreakTime = TimeSpan.Zero;
                var workingDays = 0;
                var absentDays = 0;
                var lateDays = 0;

                var current = request.FromDate.Date;
                while (current <= request.ToDate.Date)
                {
                    if (DateTimeService.IsWorkingDay(current))
                    {
                        workingDays++;
                        
                        var dailyRecords = await _timeTrackingService.GetDailyRecordsAsync(employee.Id, current);
                        var checkIn = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckIn);

                        if (checkIn == null)
                        {
                            absentDays++;
                        }
                        else
                        {
                            var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employee.Id, current);
                            var breakTime = await _timeTrackingService.CalculateBreakTimeAsync(employee.Id, current);
                            
                            totalWorkedHours = totalWorkedHours.Add(workedHours);
                            totalBreakTime = totalBreakTime.Add(breakTime);

                            var isLate = checkIn.Timestamp.TimeOfDay > employee.WorkStartTime.Add(TimeSpan.FromMinutes(employee.Company?.ToleranceMinutes ?? 15));
                            if (isLate) lateDays++;
                        }
                    }

                    current = current.AddDays(1);
                }

                var expectedHours = TimeSpan.FromHours(workingDays * 8); // 8 horas por día
                var overtimeHours = totalWorkedHours > expectedHours ? totalWorkedHours - expectedHours : TimeSpan.Zero;
                var attendancePercentage = workingDays > 0 ? (double)(workingDays - absentDays) / workingDays * 100 : 0;

                hoursReport.Add(new HoursReportDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    EmployeeCode = employee.EmployeeCode,
                    DepartmentName = employee.Department?.Name,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    TotalWorkedHours = totalWorkedHours,
                    ExpectedHours = expectedHours,
                    OvertimeHours = overtimeHours,
                    BreakTime = totalBreakTime,
                    WorkingDays = workingDays,
                    AbsentDays = absentDays,
                    LateDays = lateDays,
                    AttendancePercentage = Math.Round(attendancePercentage, 2)
                });
            }

            return hoursReport.OrderBy(r => r.EmployeeName);
        }

        public async Task<SummaryReportDto> GenerateSummaryReportAsync(DateTime fromDate, DateTime toDate)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Active);
            var totalEmployees = await _context.Employees.CountAsync();
            var activeEmployees = await _context.Employees.CountAsync(e => e.Active);
            var totalDepartments = await _context.Departments.CountAsync(d => d.Active);

            // Calcular estadísticas básicas
            var attendanceRequest = new AttendanceReportRequest
            {
                FromDate = fromDate,
                ToDate = toDate,
                IncludeAbsent = true,
                IncludeWeekends = false
            };

            var attendanceData = await GenerateAttendanceReportAsync(attendanceRequest);
            
            var totalRecords = attendanceData.Count();
            var presentRecords = attendanceData.Count(r => !r.IsAbsent);
            var averageAttendanceRate = totalRecords > 0 ? (double)presentRecords / totalRecords * 100 : 0;

            return new SummaryReportDto
            {
                ReportDate = DateTime.UtcNow,
                FromDate = fromDate,
                ToDate = toDate,
                CompanyName = company?.Name ?? "Empresa",
                TotalEmployees = totalEmployees,
                ActiveEmployees = activeEmployees,
                TotalDepartments = totalDepartments,
                AverageAttendanceRate = Math.Round(averageAttendanceRate, 2),
                TotalWorkingDays = DateTimeService.GetWorkingDays(fromDate, toDate),
                TotalAbsences = attendanceData.Count(r => r.IsAbsent),
                TotalLateArrivals = attendanceData.Count(r => r.IsLate),
                TotalWorkedHours = TimeSpan.FromTicks(attendanceData.Where(r => r.WorkedHours.HasValue).Sum(r => r.WorkedHours!.Value.Ticks)),
                TotalOvertimeHours = TimeSpan.FromTicks(attendanceData.Where(r => r.OvertimeHours.HasValue).Sum(r => r.OvertimeHours!.Value.Ticks)),
                AverageWorkHoursPerEmployee = TimeSpan.Zero, // Calcular si es necesario
                TopAttendance = new List<EmployeeAttendanceSummary>(), // Implementar si es necesario
                TopHours = new List<EmployeeHoursSummary>(), // Implementar si es necesario
                DepartmentStats = new List<DepartmentSummary>() // Implementar si es necesario
            };
        }

        public async Task<byte[]> ExportAttendanceReportToExcelAsync(AttendanceReportRequest request)
        {
            var data = await GenerateAttendanceReportAsync(request);
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Reporte de Asistencia");

            // Headers
            worksheet.Cell(1, 1).Value = "Empleado";
            worksheet.Cell(1, 2).Value = "Código";
            worksheet.Cell(1, 3).Value = "Departamento";
            worksheet.Cell(1, 4).Value = "Fecha";
            worksheet.Cell(1, 5).Value = "Entrada";
            worksheet.Cell(1, 6).Value = "Salida";
            worksheet.Cell(1, 7).Value = "Horas Trabajadas";
            worksheet.Cell(1, 8).Value = "Estado";
            worksheet.Cell(1, 9).Value = "Notas";

            // Data
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.EmployeeName;
                worksheet.Cell(row, 2).Value = item.EmployeeCode;
                worksheet.Cell(row, 3).Value = item.DepartmentName ?? "";
                worksheet.Cell(row, 4).Value = item.Date.ToString("dd/MM/yyyy");
                worksheet.Cell(row, 5).Value = item.CheckInTime?.ToString("HH:mm") ?? "";
                worksheet.Cell(row, 6).Value = item.CheckOutTime?.ToString("HH:mm") ?? "";
                worksheet.Cell(row, 7).Value = item.WorkedHours?.ToString(@"hh\:mm") ?? "";
                worksheet.Cell(row, 8).Value = item.Status;
                worksheet.Cell(row, 9).Value = item.Notes ?? "";
                row++;
            }

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportHoursReportToExcelAsync(HoursReportRequest request)
        {
            var data = await GenerateHoursReportAsync(request);
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Reporte de Horas");

            // Headers
            worksheet.Cell(1, 1).Value = "Empleado";
            worksheet.Cell(1, 2).Value = "Código";
            worksheet.Cell(1, 3).Value = "Departamento";
            worksheet.Cell(1, 4).Value = "Horas Trabajadas";
            worksheet.Cell(1, 5).Value = "Horas Esperadas";
            worksheet.Cell(1, 6).Value = "Horas Extra";
            worksheet.Cell(1, 7).Value = "Días Trabajados";
            worksheet.Cell(1, 8).Value = "Días Ausente";
            worksheet.Cell(1, 9).Value = "% Asistencia";

            // Data
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.EmployeeName;
                worksheet.Cell(row, 2).Value = item.EmployeeCode;
                worksheet.Cell(row, 3).Value = item.DepartmentName ?? "";
                worksheet.Cell(row, 4).Value = item.TotalWorkedHours.ToString(@"hh\:mm");
                worksheet.Cell(row, 5).Value = item.ExpectedHours.ToString(@"hh\:mm");
                worksheet.Cell(row, 6).Value = item.OvertimeHours.ToString(@"hh\:mm");
                worksheet.Cell(row, 7).Value = item.WorkingDays;
                worksheet.Cell(row, 8).Value = item.AbsentDays;
                worksheet.Cell(row, 9).Value = $"{item.AttendancePercentage:F2}%";
                row++;
            }

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportSummaryReportToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            var summary = await GenerateSummaryReportAsync(fromDate, toDate);
            
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Resumen Ejecutivo");

            // Título
            worksheet.Cell(1, 1).Value = $"Resumen Ejecutivo - {summary.CompanyName}";
            worksheet.Cell(2, 1).Value = $"Período: {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy}";

            // Estadísticas generales
            worksheet.Cell(4, 1).Value = "Estadísticas Generales";
            worksheet.Cell(5, 1).Value = "Total Empleados:";
            worksheet.Cell(5, 2).Value = summary.TotalEmployees;
            worksheet.Cell(6, 1).Value = "Empleados Activos:";
            worksheet.Cell(6, 2).Value = summary.ActiveEmployees;
            worksheet.Cell(7, 1).Value = "Total Departamentos:";
            worksheet.Cell(7, 2).Value = summary.TotalDepartments;
            worksheet.Cell(8, 1).Value = "Promedio Asistencia:";
            worksheet.Cell(8, 2).Value = $"{summary.AverageAttendanceRate:F2}%";

            worksheet.ColumnsUsed().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<string> ExportAttendanceReportToCsvAsync(AttendanceReportRequest request)
        {
            var data = await GenerateAttendanceReportAsync(request);
            var csv = new StringBuilder();

            // Headers
            csv.AppendLine("Empleado,Código,Departamento,Fecha,Entrada,Salida,Horas Trabajadas,Estado,Notas");

            // Data
            foreach (var item in data)
            {
                csv.AppendLine($"{item.EmployeeName},{item.EmployeeCode},{item.DepartmentName ?? ""},{item.Date:dd/MM/yyyy},{item.CheckInTime?.ToString("HH:mm") ?? ""},{item.CheckOutTime?.ToString("HH:mm") ?? ""},{item.WorkedHours?.ToString(@"hh\:mm") ?? ""},{item.Status},{item.Notes ?? ""}");
            }

            return csv.ToString();
        }

        public async Task<string> ExportHoursReportToCsvAsync(HoursReportRequest request)
        {
            var data = await GenerateHoursReportAsync(request);
            var csv = new StringBuilder();

            // Headers
            csv.AppendLine("Empleado,Código,Departamento,Horas Trabajadas,Horas Esperadas,Horas Extra,Días Trabajados,Días Ausente,% Asistencia");

            // Data
            foreach (var item in data)
            {
                csv.AppendLine($"{item.EmployeeName},{item.EmployeeCode},{item.DepartmentName ?? ""},{item.TotalWorkedHours:hh\\:mm},{item.ExpectedHours:hh\\:mm},{item.OvertimeHours:hh\\:mm},{item.WorkingDays},{item.AbsentDays},{item.AttendancePercentage:F2}%");
            }

            return csv.ToString();
        }

        public async Task<Dictionary<string, object>> GetDashboardStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var totalEmployees = await _context.Employees.CountAsync(e => e.Active);
            var totalDepartments = await _context.Departments.CountAsync(d => d.Active);

            // Empleados presentes hoy
            var employees = await _context.Employees.Where(e => e.Active).ToListAsync();
            var presentToday = 0;
            var onBreak = 0;

            foreach (var employee in employees)
            {
                if (await _timeTrackingService.IsEmployeeCheckedInAsync(employee.Id))
                {
                    presentToday++;
                    if (await _timeTrackingService.IsEmployeeOnBreakAsync(employee.Id))
                    {
                        onBreak++;
                    }
                }
            }

            var attendanceRate = totalEmployees > 0 ? (double)presentToday / totalEmployees * 100 : 0;

            // Registros recientes
            var recentRecords = await _context.TimeRecords
                .Include(tr => tr.Employee)
                .Where(tr => tr.Timestamp >= DateTime.Today)
                .OrderByDescending(tr => tr.Timestamp)
                .Take(10)
                .Select(tr => new
                {
                    EmployeeName = tr.Employee.FullName,
                    Type = tr.Type.ToString(),
                    Timestamp = tr.Timestamp,
                    Notes = tr.Notes
                })
                .ToListAsync();

            return new Dictionary<string, object>
            {
                ["totalEmployees"] = totalEmployees,
                ["totalDepartments"] = totalDepartments,
                ["presentToday"] = presentToday,
                ["onBreak"] = onBreak,
                ["attendanceRate"] = Math.Round(attendanceRate, 2),
                ["recentActivity"] = recentRecords,
                ["lastUpdated"] = DateTime.UtcNow
            };
        }

        public async Task<Dictionary<string, object>> GetEmployeeStatsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                throw new ArgumentException("Empleado no encontrado");
            }

            var totalWorkingDays = DateTimeService.GetWorkingDays(from, to);
            var totalWorkedHours = TimeSpan.Zero;
            var presentDays = 0;
            var lateDays = 0;

            var current = from.Date;
            while (current <= to.Date)
            {
                if (DateTimeService.IsWorkingDay(current))
                {
                    var dailyRecords = await _timeTrackingService.GetDailyRecordsAsync(employeeId, current);
                    var checkIn = dailyRecords.FirstOrDefault(r => r.Type == Shared.Models.Enums.RecordType.CheckIn);

                    if (checkIn != null)
                    {
                        presentDays++;
                        var workedHours = await _timeTrackingService.CalculateWorkedHoursAsync(employeeId, current);
                        totalWorkedHours = totalWorkedHours.Add(workedHours);

                        var tolerance = employee.Company?.ToleranceMinutes ?? 15;
                        var isLate = checkIn.Timestamp.TimeOfDay > employee.WorkStartTime.Add(TimeSpan.FromMinutes(tolerance));
                        if (isLate) lateDays++;
                    }
                }

                current = current.AddDays(1);
            }

            var attendanceRate = totalWorkingDays > 0 ? (double)presentDays / totalWorkingDays * 100 : 0;
            var averageHoursPerDay = presentDays > 0 ? totalWorkedHours.TotalHours / presentDays : 0;

            return new Dictionary<string, object>
            {
                ["employeeId"] = employeeId,
                ["employeeName"] = employee.FullName,
                ["departmentName"] = employee.Department?.Name ?? "",
                ["totalWorkingDays"] = totalWorkingDays,
                ["presentDays"] = presentDays,
                ["absentDays"] = totalWorkingDays - presentDays,
                ["lateDays"] = lateDays,
                ["attendanceRate"] = Math.Round(attendanceRate, 2),
                ["totalWorkedHours"] = totalWorkedHours.ToString(@"hh\:mm"),
                ["averageHoursPerDay"] = Math.Round(averageHoursPerDay, 2),
                ["period"] = new { from, to }
            };
        }
    }
}