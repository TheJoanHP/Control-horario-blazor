namespace Shared.Models.DTOs.Reports
{
    /// <summary>
    /// DTO para el reporte resumen del sistema de control horario
    /// </summary>
    public class SummaryReportDto
    {
        /// <summary>
        /// Período del reporte (ej: "01/01/2024 - 31/01/2024")
        /// </summary>
        public string Period { get; set; } = string.Empty;

        /// <summary>
        /// Número total de empleados activos
        /// </summary>
        public int TotalEmployees { get; set; }

        /// <summary>
        /// Número total de registros de tiempo en el período
        /// </summary>
        public int TotalRecords { get; set; }

        /// <summary>
        /// Total de horas trabajadas en el período
        /// </summary>
        public double TotalWorkHours { get; set; }

        /// <summary>
        /// Promedio de horas trabajadas por empleado
        /// </summary>
        public double AverageHoursPerEmployee { get; set; }

        /// <summary>
        /// Tasa de asistencia general (%)
        /// </summary>
        public double AttendanceRate { get; set; }

        /// <summary>
        /// Tasa de puntualidad general (%)
        /// </summary>
        public double PunctualityRate { get; set; }

        /// <summary>
        /// Total de horas extras en el período
        /// </summary>
        public double TotalOvertimeHours { get; set; }

        /// <summary>
        /// Número de días hábiles en el período
        /// </summary>
        public int WorkingDaysInPeriod { get; set; }

        /// <summary>
        /// Número de empleados que trabajaron al menos un día
        /// </summary>
        public int ActiveWorkingEmployees { get; set; }

        /// <summary>
        /// Departamento con más horas trabajadas
        /// </summary>
        public string TopDepartmentByHours { get; set; } = string.Empty;

        /// <summary>
        /// Departamento con mejor tasa de asistencia
        /// </summary>
        public string TopDepartmentByAttendance { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de generación del reporte
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Resumen por departamentos
        /// </summary>
        public List<DepartmentSummaryDto> DepartmentSummaries { get; set; } = new();

        /// <summary>
        /// Top 5 empleados por horas trabajadas
        /// </summary>
        public List<EmployeeSummaryDto> TopEmployeesByHours { get; set; } = new();

        /// <summary>
        /// Días con mayor ausentismo
        /// </summary>
        public List<DayAbsenteeismDto> HighAbsenteeismDays { get; set; } = new();
    }

    /// <summary>
    /// Resumen por departamento
    /// </summary>
    public class DepartmentSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
        public double TotalHours { get; set; }
        public double AverageHours { get; set; }
        public double AttendanceRate { get; set; }
        public double PunctualityRate { get; set; }
        public double OvertimeHours { get; set; }
    }

    /// <summary>
    /// Resumen de empleado para rankings
    /// </summary>
    public class EmployeeSummaryDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public double TotalHours { get; set; }
        public double AverageHoursPerDay { get; set; }
        public int DaysWorked { get; set; }
        public double AttendanceRate { get; set; }
        public double OvertimeHours { get; set; }
    }

    /// <summary>
    /// Información de ausentismo por día
    /// </summary>
    public class DayAbsenteeismDto
    {
        public DateTime Date { get; set; }
        public int ExpectedEmployees { get; set; }
        public int PresentEmployees { get; set; }
        public int AbsentEmployees { get; set; }
        public double AbsenteeismRate { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
    }
}