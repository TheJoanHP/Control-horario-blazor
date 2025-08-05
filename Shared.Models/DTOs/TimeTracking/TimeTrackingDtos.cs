using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    // DTOs para Check In/Out
    public class CheckInDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class CheckOutDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class BreakStartDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    public class BreakEndDto
    {
        [Required]
        public int EmployeeId { get; set; }
        
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; } = false;
        public int? CreatedByUserId { get; set; }
    }

    // DTOs para TimeRecord
    public class TimeRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public RecordType Type { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public DateTime DateTime { get; set; }
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsManualEntry { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateTimeRecordDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public RecordType Type { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public TimeSpan Time { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class UpdateTimeRecordDto
    {
        public DateTime? Date { get; set; }
        public TimeSpan? Time { get; set; }
        public RecordType? Type { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}