namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para el reporte de horas por empleado
    /// </summary>
    public class HoursReportDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double TotalWorkHours { get; set; }
        public double RegularHours { get; set; }
        public double OvertimeHours { get; set; }
        public int DaysWorked { get; set; }
        public int DaysPresent { get; set; }
        public int DaysAbsent { get; set; }
        public double AverageHoursPerDay { get; set; }
        public double AttendanceRate { get; set; }
    }

    /// <summary>
    /// DTO para registro individual de horas por día
    /// </summary>
    public class HoursRecordDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double HoursWorked { get; set; }
        public double RegularHours { get; set; }
        public double OvertimeHours { get; set; }
        public bool IsHoliday { get; set; }
        public bool IsWeekend { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO para resumen general del reporte de horas
    /// </summary>
    public class HoursSummaryDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TotalHours { get; set; }
        public double TotalRegularHours { get; set; }
        public double TotalOvertimeHours { get; set; }
        public int TotalEmployees { get; set; }
        public double AverageHoursPerEmployee { get; set; }
        public List<HoursReportDto> EmployeeHours { get; set; } = new();
    }
}