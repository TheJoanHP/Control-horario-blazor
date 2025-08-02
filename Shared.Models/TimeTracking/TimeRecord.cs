using System.ComponentModel.DataAnnotations;
using Shared.Models.Enums;
using Shared.Models.Core;

namespace Shared.Models.TimeTracking
{
    public class TimeRecord
    {
        public int Id { get; set; }
        
        public int EmployeeId { get; set; }
        
        public RecordType Type { get; set; } = RecordType.CheckIn;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(255)]
        public string? Notes { get; set; }
        
        // Geolocalización (opcional)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [MaxLength(255)]
        public string? Location { get; set; }
        
        // Metadatos
        [MaxLength(100)]
        public string? IpAddress { get; set; }
        
        [MaxLength(255)]
        public string? UserAgent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navegación
        public Employee Employee { get; set; } = null!;
    }
}