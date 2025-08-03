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

}