namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para reporte resumen
    /// </summary>
    public class SummaryReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalEmployees { get; set; }
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan TotalOvertimeHours { get; set; }
        public TimeSpan AverageWorkedHours { get; set; }
        public TimeSpan TotalBreakTime { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalLateArrivals { get; set; }
        public double AttendanceRate { get; set; }
    }
}