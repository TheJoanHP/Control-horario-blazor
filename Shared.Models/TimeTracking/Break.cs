using System.ComponentModel.DataAnnotations;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    public class Break
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Propiedades calculadas
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
        public bool IsActive => !EndTime.HasValue;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
    }
}