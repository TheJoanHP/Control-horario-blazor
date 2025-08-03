using Shared.Models.Core;
using Shared.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models.TimeTracking
{
    public class TimeRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        public DateTime? CheckIn { get; set; }

        public DateTime? CheckOut { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public double? TotalHours { get; set; }

        [Required]
        public RecordType RecordType { get; set; } = RecordType.CheckIn;

        [StringLength(200)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        // Computed properties
        [NotMapped]
        public bool IsComplete => CheckIn.HasValue && CheckOut.HasValue;

        [NotMapped]
        public TimeSpan? Duration => IsComplete ? CheckOut!.Value - CheckIn!.Value : null;

        [NotMapped]
        public bool IsActive => CheckIn.HasValue && !CheckOut.HasValue;
    }

    public class WorkSchedule
    {
        [Key]
        public int Id { get; set; }

        public int? EmployeeId { get; set; }

        public int? DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public TimeSpan? BreakStart { get; set; }

        public TimeSpan? BreakEnd { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        // Computed properties
        [NotMapped]
        public TimeSpan WorkingHours => EndTime - StartTime - (BreakEnd.HasValue && BreakStart.HasValue ? BreakEnd.Value - BreakStart.Value : TimeSpan.Zero);
    }

    public class Break
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TimeRecordId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [StringLength(200)]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TimeRecordId")]
        public virtual TimeRecord? TimeRecord { get; set; }

        // Computed properties
        [NotMapped]
        public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;

        [NotMapped]
        public bool IsActive => !EndTime.HasValue;
    }

    public class Overtime
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public double Hours { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public bool Approved { get; set; } = false;

        public int? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ApprovedBy")]
        public virtual Employee? ApprovedByEmployee { get; set; }
    }
}