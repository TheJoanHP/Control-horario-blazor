using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    /// <summary>
    /// DTO para actualizar registros de tiempo
    /// </summary>
    public class UpdateTimeRecordDto
    {
        [Required]
        public RecordType Type { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}