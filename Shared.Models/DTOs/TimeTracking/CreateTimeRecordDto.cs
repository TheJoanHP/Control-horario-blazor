using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    public class CreateTimeRecordDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public RecordType Type { get; set; }

        public DateTime? Timestamp { get; set; } // Si es null, se usa DateTime.UtcNow

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(255)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? DeviceInfo { get; set; }
    }
}