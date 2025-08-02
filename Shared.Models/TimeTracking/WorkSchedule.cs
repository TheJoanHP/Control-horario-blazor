using System.ComponentModel.DataAnnotations;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    public class WorkSchedule
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        // Horarios por día de la semana (0 = Domingo, 6 = Sábado)
        public bool MondayEnabled { get; set; } = true;
        public TimeSpan? MondayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? MondayEnd { get; set; } = new TimeSpan(17, 0, 0);
        
        public bool TuesdayEnabled { get; set; } = true;
        public TimeSpan? TuesdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? TuesdayEnd { get; set; } = new TimeSpan(17, 0, 0);
        
        public bool WednesdayEnabled { get; set; } = true;
        public TimeSpan? WednesdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? WednesdayEnd { get; set; } = new TimeSpan(17, 0, 0);
        
        public bool ThursdayEnabled { get; set; } = true;
        public TimeSpan? ThursdayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? ThursdayEnd { get; set; } = new TimeSpan(17, 0, 0);
        
        public bool FridayEnabled { get; set; } = true;
        public TimeSpan? FridayStart { get; set; } = new TimeSpan(9, 0, 0);
        public TimeSpan? FridayEnd { get; set; } = new TimeSpan(17, 0, 0);
        
        public bool SaturdayEnabled { get; set; } = false;
        public TimeSpan? SaturdayStart { get; set; }
        public TimeSpan? SaturdayEnd { get; set; }
        
        public bool SundayEnabled { get; set; } = false;
        public TimeSpan? SundayStart { get; set; }
        public TimeSpan? SundayEnd { get; set; }
        
        public bool Active { get; set; } = true;
        
        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveTo { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
    }
}