using Shared.Models.Enums;

namespace Shared.Models.DTOs.TimeTracking
{
    /// <summary>
    /// DTO para mostrar registros de tiempo
    /// </summary>
    public class TimeRecordDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public RecordType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}