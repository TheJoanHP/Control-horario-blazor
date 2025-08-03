// Shared.Models/DTOs/TimeTracking/WorkScheduleDto.cs
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    public class WorkScheduleDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DayOfWeek[] WorkDays { get; set; } = Array.Empty<DayOfWeek>();
        public bool Active { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}