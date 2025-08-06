using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    [Table("work_schedules")]
    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }

        public int? CompanyId { get; set; }

        public int? EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

        public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);

        // Configuración por día de la semana
        public bool MondayEnabled { get; set; } = true;
        public TimeSpan? MondayStart { get; set; }
        public TimeSpan? MondayEnd { get; set; }

        public bool TuesdayEnabled { get; set; } = true;
        public TimeSpan? TuesdayStart { get; set; }
        public TimeSpan? TuesdayEnd { get; set; }

        public bool WednesdayEnabled { get; set; } = true;
        public TimeSpan? WednesdayStart { get; set; }
        public TimeSpan? WednesdayEnd { get; set; }

        public bool ThursdayEnabled { get; set; } = true;
        public TimeSpan? ThursdayStart { get; set; }
        public TimeSpan? ThursdayEnd { get; set; }

        public bool FridayEnabled { get; set; } = true;
        public TimeSpan? FridayStart { get; set; }
        public TimeSpan? FridayEnd { get; set; }

        public bool SaturdayEnabled { get; set; } = false;
        public TimeSpan? SaturdayStart { get; set; }
        public TimeSpan? SaturdayEnd { get; set; }

        public bool SundayEnabled { get; set; } = false;
        public TimeSpan? SundayStart { get; set; }
        public TimeSpan? SundayEnd { get; set; }

        public int ToleranceMinutes { get; set; } = 15;

        // Propiedades adicionales requeridas por DbInitializer
        public TimeSpan? BreakDuration { get; set; }
        public bool FlexibleHours { get; set; } = false;
        public int MaxFlexMinutes { get; set; } = 0;

        public bool IsDefault { get; set; } = false;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        public virtual ICollection<Break> Breaks { get; set; } = new List<Break>();

        // Propiedades calculadas
        [NotMapped]
        public double WorkHoursPerDay => (EndTime - StartTime).TotalHours;

        [NotMapped]
        public int WorkDaysPerWeek
        {
            get
            {
                int count = 0;
                if (MondayEnabled) count++;
                if (TuesdayEnabled) count++;
                if (WednesdayEnabled) count++;
                if (ThursdayEnabled) count++;
                if (FridayEnabled) count++;
                if (SaturdayEnabled) count++;
                if (SundayEnabled) count++;
                return count;
            }
        }

        [NotMapped]
        public double WorkHoursPerWeek => WorkHoursPerDay * WorkDaysPerWeek;
    }
}