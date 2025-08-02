namespace Shared.Models.DTOs.Reports
{
    public class HoursReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan ExpectedHours { get; set; }
        public TimeSpan OvertimeHours { get; set; }
        public TimeSpan BreakTime { get; set; }
        public int WorkingDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public double AttendancePercentage { get; set; }
    }
    
    public class HoursReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public bool GroupByDepartment { get; set; } = false;
        public bool IncludeDetails { get; set; } = true;
    }
}