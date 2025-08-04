using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    /// <summary>
    /// Horario de trabajo de un empleado
    /// </summary>
    [Table("WorkSchedules")]
    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsWorkingDay { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public TimeSpan WorkingHours => IsWorkingDay ? EndTime - StartTime : TimeSpan.Zero;
    }
}