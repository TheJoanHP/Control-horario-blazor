using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    [Table("breaks")]
    public class Break
    {
        [Key]
        public int Id { get; set; }

        public int? EmployeeId { get; set; }

        public int? WorkScheduleId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public bool IsPaid { get; set; } = false;

        public bool Active { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("WorkScheduleId")]
        public virtual WorkSchedule? WorkSchedule { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public TimeSpan Duration => EndTime - StartTime;

        [NotMapped]
        public double DurationMinutes => Duration.TotalMinutes;

        [NotMapped]
        public bool IsActive => Active;

        [NotMapped]
        public string DisplayName => $"{Name} ({StartTime:hh\\:mm} - {EndTime:hh\\:mm})";
    }
}