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

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Lunes
        public bool MondayEnabled { get; set; } = true;
        public TimeSpan? MondayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? MondayEnd { get; set; } = new TimeSpan(17, 0, 0);

        // Martes
        public bool TuesdayEnabled { get; set; } = true;
        public TimeSpan? TuesdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? TuesdayEnd { get; set; } = new TimeSpan(17, 0, 0);

        // Miércoles
        public bool WednesdayEnabled { get; set; } = true;
        public TimeSpan? WednesdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? WednesdayEnd { get; set; } = new TimeSpan(17, 0, 0);

        // Jueves
        public bool ThursdayEnabled { get; set; } = true;
        public TimeSpan? ThursdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? ThursdayEnd { get; set; } = new TimeSpan(17, 0, 0);

        // Viernes
        public bool FridayEnabled { get; set; } = true;
        public TimeSpan? FridayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? FridayEnd { get; set; } = new TimeSpan(17, 0, 0);

        // Sábado
        public bool SaturdayEnabled { get; set; } = false;
        public TimeSpan? SaturdayStart { get; set; }
        public TimeSpan? SaturdayEnd { get; set; }

        // Domingo
        public bool SundayEnabled { get; set; } = false;
        public TimeSpan? SundayStart { get; set; }
        public TimeSpan? SundayEnd { get; set; }

        // Configuración general
        public TimeSpan? BreakDuration { get; set; } = new TimeSpan(1, 0, 0);
        public bool FlexibleHours { get; set; } = false;
        public int? MaxFlexMinutes { get; set; } = 30;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Relaciones
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

        // Propiedades calculadas
        [NotMapped]
        public double WeeklyHours
        {
            get
            {
                double total = 0;
                if (MondayEnabled && MondayStart.HasValue && MondayEnd.HasValue)
                    total += (MondayEnd.Value - MondayStart.Value).TotalHours;
                if (TuesdayEnabled && TuesdayStart.HasValue && TuesdayEnd.HasValue)
                    total += (TuesdayEnd.Value - TuesdayStart.Value).TotalHours;
                if (WednesdayEnabled && WednesdayStart.HasValue && WednesdayEnd.HasValue)
                    total += (WednesdayEnd.Value - WednesdayStart.Value).TotalHours;
                if (ThursdayEnabled && ThursdayStart.HasValue && ThursdayEnd.HasValue)
                    total += (ThursdayEnd.Value - ThursdayStart.Value).TotalHours;
                if (FridayEnabled && FridayStart.HasValue && FridayEnd.HasValue)
                    total += (FridayEnd.Value - FridayStart.Value).TotalHours;
                if (SaturdayEnabled && SaturdayStart.HasValue && SaturdayEnd.HasValue)
                    total += (SaturdayEnd.Value - SaturdayStart.Value).TotalHours;
                if (SundayEnabled && SundayStart.HasValue && SundayEnd.HasValue)
                    total += (SundayEnd.Value - SundayStart.Value).TotalHours;
                    
                return total;
            }
        }
    }
}