// ===== Shared/Models/DTOs/TimeTracking/TimeRecordDto.cs =====
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
        public double? TotalHours { get; set; }
        public RecordType RecordType { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsComplete { get; set; }
        public bool IsActive { get; set; }
    }

    public class CheckInDto
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckOutDto
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
    }

    public class EmployeeStatusDto
    {
        public int EmployeeId { get; set; }
        public bool IsCheckedIn { get; set; }
        public bool IsOnBreak { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CurrentBreakStart { get; set; }
        public double WorkedHoursToday { get; set; }
        public DateTime LastUpdate { get; set; }
        public EmployeeStatus Status { get; set; }
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

    public class WorkScheduleDto
    {
        public int Id { get; set; }
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStart { get; set; }
        public TimeSpan? BreakEnd { get; set; }
        public bool IsActive { get; set; }
        public TimeSpan WorkingHours { get; set; }
    }

    public class CreateWorkScheduleDto
    {
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "El día de la semana es requerido")]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required(ErrorMessage = "La hora de inicio es requerida")]
        public TimeSpan StartTime { get; set; }
        
        [Required(ErrorMessage = "La hora de fin es requerida")]
        public TimeSpan EndTime { get; set; }
        
        public TimeSpan? BreakStart { get; set; }
        public TimeSpan? BreakEnd { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWorkScheduleDto
    {
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? Name { get; set; }
        
        public DayOfWeek? DayOfWeek { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? BreakStart { get; set; }
        public TimeSpan? BreakEnd { get; set; }
        public bool? IsActive { get; set; }
    }
}
