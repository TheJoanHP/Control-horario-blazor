using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.TimeTracking
{
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
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

    public class WeeklyScheduleDto
    {
        public int? EmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<DailyScheduleDto> DailySchedules { get; set; } = new();
    }

    public class DailyScheduleDto
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStart { get; set; }
        public TimeSpan? BreakEnd { get; set; }
        public bool IsWorkingDay { get; set; } = true;
    }
}