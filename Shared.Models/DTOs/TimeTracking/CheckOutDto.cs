using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs.TimeTracking
{
    public class CheckOutDto
    {
        public DateTime? Timestamp { get; set; }
        
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
    }
}