using Shared.Models.Core;
using Shared.Models.Enums;

namespace Shared.Models.TimeTracking
{
    public class TimeRecord
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public RecordType Type { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string? Location { get; set; }
        
        public string? Notes { get; set; }
        
        public string? DeviceInfo { get; set; }
        
        public string? IpAddress { get; set; }
        
        public int? PairedRecordId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
        public TimeRecord? PairedRecord { get; set; }
    }
}