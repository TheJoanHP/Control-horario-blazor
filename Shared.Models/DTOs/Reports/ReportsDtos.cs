using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.Reports
{
    public class AttendanceReportRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public List<int>? EmployeeIds { get; set; }
        public List<int>? DepartmentIds { get; set; }
        public bool IncludeInactive { get; set; } = false;
    }

    public class HoursReportRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public List<int>? EmployeeIds { get; set; }
        public List<int>? DepartmentIds { get; set; }
        public bool IncludeOvertimeDetails { get; set; } = true;
        public bool IncludeInactive { get; set; } = false;
    }

    public class DepartmentReportDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public double TotalHours { get; set; }
        public double AverageHoursPerEmployee { get; set; }
        public double AttendanceRate { get; set; }
        public double PunctualityRate { get; set; }
    }

    public class EmployeeDetailReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public List<DailyAttendanceDto> DailyAttendances { get; set; } = new();
        public double TotalHours { get; set; }
        public double AverageHoursPerDay { get; set; }
        public int DaysWorked { get; set; }
        public int DaysAbsent { get; set; }
        public double AttendanceRate { get; set; }
    }

    public class DailyAttendanceDto
    {
        public DateTime Date { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public double HoursWorked { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsLate { get; set; }
        public bool IsEarlyLeave { get; set; }
        public List<BreakDto> Breaks { get; set; } = new();
    }

    public class BreakDto
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public double DurationMinutes { get; set; }
    }
}