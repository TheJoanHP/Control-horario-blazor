namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para el reporte de asistencia individual por empleado y día
    /// </summary>
    public class AttendanceReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
        public double TotalHours { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarlyLeave { get; set; }
        public string Status { get; set; } = string.Empty; // Present, Absent, Late, etc.
        public string? Notes { get; set; }
        public List<BreakRecordDto> Breaks { get; set; } = new();
    }

    /// <summary>
    /// DTO para registros de descanso/pausa
    /// </summary>
    public class BreakRecordDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double DurationMinutes { get; set; }
        public string Type { get; set; } = string.Empty; // Lunch, Break, etc.
    }

    /// <summary>
    /// DTO para resumen de asistencia (diferente al reporte individual)
    /// </summary>
    public class AttendanceSummaryDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalRecords { get; set; }
        public int TotalEmployees { get; set; }
        public double OverallAttendanceRate { get; set; }
        public double OverallPunctualityRate { get; set; }
        public List<AttendanceReportDto> Records { get; set; } = new();
    }
}