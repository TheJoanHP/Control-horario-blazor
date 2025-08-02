using System.ComponentModel.DataAnnotations;
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
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        // Geolocalización (opcional)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [MaxLength(255)]
        public string? Location { get; set; }
        
        // Información del dispositivo
        [MaxLength(100)]
        public string? DeviceInfo { get; set; }
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
    }
}