namespace Shared.Models.DTOs.Reports
{
    public class SummaryReportDto
    {
        public DateTime ReportDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        
        // Estadísticas generales
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int TotalDepartments { get; set; }
        
        // Estadísticas de asistencia
        public double AverageAttendanceRate { get; set; }
        public int TotalWorkingDays { get; set; }
        public int TotalAbsences { get; set; }
        public int TotalLateArrivals { get; set; }
        
        // Estadísticas de horas
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan TotalOvertimeHours { get; set; }
        public TimeSpan AverageWorkHoursPerEmployee { get; set; }
        
        // Top performers
        public List<EmployeeAttendanceSummary> TopAttendance { get; set; } = new();
        public List<EmployeeHoursSummary> TopHours { get; set; } = new();
        
        // Estadísticas por departamento
        public List<DepartmentSummary> DepartmentStats { get; set; } = new();
    }
    
    public class EmployeeAttendanceSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double AttendanceRate { get; set; }
        public int WorkingDays { get; set; }
        public int AbsentDays { get; set; }
    }
    
    public class EmployeeHoursSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public TimeSpan TotalHours { get; set; }
        public TimeSpan OvertimeHours { get; set; }
    }
    
    public class DepartmentSummary
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public double AverageAttendanceRate { get; set; }
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan TotalOvertimeHours { get; set; }
    }
}