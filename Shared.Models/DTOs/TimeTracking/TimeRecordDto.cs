using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    public class TimeRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public RecordType Type { get; set; }
        public string TypeDisplay { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? Notes { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Location { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}