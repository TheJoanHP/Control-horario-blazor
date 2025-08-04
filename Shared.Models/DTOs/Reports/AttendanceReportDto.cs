namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para reporte de asistencia
    /// </summary>
    public class AttendanceReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public TimeSpan WorkedHours { get; set; }
        public TimeSpan BreakTime { get; set; }
        public string Status { get; set; } = string.Empty; // "Present", "Late", "Absent", "Partial"
    }
}