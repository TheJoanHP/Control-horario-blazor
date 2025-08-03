using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.TimeTracking
{
    public class UpdateTimeRecordDto
    {
        public DateTime? Timestamp { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(255)]
        public string? Location { get; set; }
    }
}