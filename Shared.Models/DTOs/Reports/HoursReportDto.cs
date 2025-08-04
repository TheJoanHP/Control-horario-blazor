namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para reporte de horas
    /// </summary>
    public class HoursReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan RegularHours { get; set; }
        public TimeSpan OvertimeHours { get; set; }
        public TimeSpan TotalOvertimeHours { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int TotalWorkingDays { get; set; }
        public TimeSpan AverageHoursPerDay { get; set; }
    }
}