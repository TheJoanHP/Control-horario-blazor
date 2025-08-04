using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    /// <summary>
    /// DTO para crear registros de tiempo
    /// </summary>
    public class CreateTimeRecordDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public RecordType Type { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(500)]
        public string? DeviceInfo { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}