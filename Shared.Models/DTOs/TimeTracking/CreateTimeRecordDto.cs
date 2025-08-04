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

        public DateTime? Date { get; set; }

        public DateTime? Time { get; set; }

        [StringLength(255)]
        public string? Location { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}