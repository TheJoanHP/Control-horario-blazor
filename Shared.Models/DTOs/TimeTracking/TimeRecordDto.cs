using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    public class TimeRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public DateTime Date { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public RecordType RecordType { get; set; }
        public double? TotalHours { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Información adicional
        public bool IsComplete { get; set; }
        public bool IsOnBreak { get; set; }
        public DateTime? CurrentBreakStart { get; set; }
        public double BreakHours { get; set; }
    }

    public class CheckInDto
    {
        [Required(ErrorMessage = "La hora de entrada es requerida")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(200, ErrorMessage = "La ubicación no puede exceder 200 caracteres")]
        public string? Location { get; set; }
        
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notes { get; set; }
        
        // Coordenadas GPS (opcionales)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class CheckOutDto
    {
        [Required(ErrorMessage = "La hora de salida es requerida")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(500, ErrorMessage = "Las notas no pueden exceder 500 caracteres")]
        public string? Notes { get; set; }
        
        // Coordenadas GPS (opcionales)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class EmployeeStatusDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public RecordType Status { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CurrentBreakStart { get; set; }
        public double WorkedHoursToday { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class DailyHoursSummaryDto
    {
        public DateTime Date { get; set; }
        public int EmployeeId { get; set; }
        public double TotalWorkedHours { get; set; }
        public double TotalBreakHours { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public bool IsComplete { get; set; }
        public double ExpectedHours { get; set; } = 8.0;
        public double OvertimeHours { get; set; }
    }

    public class WeeklyHoursSummaryDto
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int EmployeeId { get; set; }
        public double TotalHours { get; set; }
        public int DaysWorked { get; set; }
        public double AverageHoursPerDay { get; set; }
        public double ExpectedHours { get; set; } = 40.0;
        public double OvertimeHours { get; set; }
    }

    public class MonthlyHoursSummaryDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int EmployeeId { get; set; }
        public double TotalHours { get; set; }
        public int DaysWorked { get; set; }
        public double AverageHoursPerDay { get; set; }
        public double ExpectedHours { get; set; }
        public double OvertimeHours { get; set; }
    }
}