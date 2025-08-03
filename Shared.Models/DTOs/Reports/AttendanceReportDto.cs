// Shared.Models/DTOs/Reports/AttendanceReportDto.cs
namespace Shared.Models.DTOs.Reports
{
    public class AttendanceReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public decimal TotalHours { get; set; }
        public int WorkingDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
