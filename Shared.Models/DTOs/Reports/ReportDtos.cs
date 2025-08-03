// ===== Shared/Models/DTOs/Reports/ReportDtos.cs =====
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Reports
{

    public class AttendanceRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public double AttendancePercentage { get; set; }
        public TimeSpan AverageArrivalTime { get; set; }
        public double TotalHours { get; set; }
    }

    public class HoursRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public double RegularHours { get; set; }
        public double OvertimeHours { get; set; }
        public double AverageHoursPerDay { get; set; }
        public int DaysWorked { get; set; }
    }

    public class OvertimeReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int TotalEmployeesWithOvertime { get; set; }
        public double TotalOvertimeHours { get; set; }
        public double AverageOvertimePerEmployee { get; set; }
        public List<OvertimeRecordDto> OvertimeRecords { get; set; } = new();
    }

    public class OvertimeRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public double RegularHours { get; set; }
        public double OvertimeHours { get; set; }
        public int DaysWithOvertime { get; set; }
        public double MaxDailyOvertime { get; set; }
    }

    public class DepartmentSummaryDto
    {
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public double TotalHours { get; set; }
        public double AverageHoursPerEmployee { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalEmployees { get; set; }
        public int EmployeesPresent { get; set; }
        public double AttendanceRate { get; set; }
        public double TotalHours { get; set; }
        public double AverageHours { get; set; }
        public string Period { get; set; } = string.Empty;
        public int EmployeesOnBreak { get; set; }
        public int EmployeesWorking { get; set; }
        public double OvertimeHours { get; set; }
    }

    public class ExportRequestDto
    {
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string Format { get; set; } = "CSV"; // CSV, Excel, PDF
        
        public bool IncludeHeaders { get; set; } = true;
        public string? ReportType { get; set; } // Attendance, Hours, Overtime
    }
}
