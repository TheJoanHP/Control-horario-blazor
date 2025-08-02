namespace Shared.Models.DTOs.Reports
{
    public class AttendanceReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public TimeSpan? WorkedHours { get; set; }
        public TimeSpan? BreakTime { get; set; }
        public TimeSpan? OvertimeHours { get; set; }
        public bool IsLate { get; set; }
        public bool IsAbsent { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
    
    public class AttendanceReportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public bool IncludeAbsent { get; set; } = true;
        public bool IncludeWeekends { get; set; } = false;
    }
}